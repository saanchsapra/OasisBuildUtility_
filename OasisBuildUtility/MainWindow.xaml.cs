using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;
using System;
using OasisBuildUtility.ViewModel;
using Microsoft.UI;
using System.Runtime.InteropServices;
using Windows.Storage;

namespace OasisBuildUtility
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();
        private AppWindow _appWindow;
        private IntPtr _hWnd;
        private IntPtr _originalWndProc;
        private WndProcDelegate _wndProcDelegate;
        private bool _isDarkTheme = false;

        private const double MinWidth = 0.5;
        private const double MinHeight = 0.8;
        private const string ThemeSettingsKey = "AppTheme";

        public MainWindow()
        {
            this.InitializeComponent();
            App.MainWindow = this;
            InitializeAppWindow();
            SetupWindowMessageHandling();
            SetInitialWindowSize();
            LoadThemeSettings();
        }

        private void InitializeAppWindow()
        {
            _hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
        }

        private void SetupWindowMessageHandling()
        {
            _wndProcDelegate = WndProc;
            _originalWndProc = SetWindowLongPtr(_hWnd, GWLP_WNDPROC, _wndProcDelegate);
        }

        private void SetInitialWindowSize()
        {
            int minWidth = (int)(GetSystemMetrics(0) * MinWidth);
            int minHeight = (int)(GetSystemMetrics(1) * MinHeight);
            _appWindow.Resize(new SizeInt32(minWidth, minHeight));
        }

        private void LoadThemeSettings()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.ContainsKey(ThemeSettingsKey))
                {
                    _isDarkTheme = (bool)localSettings.Values[ThemeSettingsKey];
                }
                else
                {
                    _isDarkTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark;
                }

                ApplyTheme();
            }
            catch (Exception)
            {
                _isDarkTheme = false;
                ApplyTheme();
            }
        }

        private void SaveThemeSettings()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[ThemeSettingsKey] = _isDarkTheme;
            }
            catch (Exception)
            {
                // Handle save error silently
            }
        }

        private void ApplyTheme()
        {
            // Set the requested theme for the Grid (which contains the ThemeDictionaries)
            var rootGrid = this.Content as FrameworkElement;
            if (rootGrid != null)
            {
                rootGrid.RequestedTheme = _isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
            }

            // Update the theme toggle button state and icon
            if (_isDarkTheme)
            {
                ThemeIcon.Glyph = "\uE706"; // Moon icon for dark theme
                ThemeToggleButton.IsChecked = true;
            }
            else
            {
                ThemeIcon.Glyph = "\uE708"; // Sun icon for light theme
                ThemeToggleButton.IsChecked = false;
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();
            SaveThemeSettings();
        }

        #region Window Message Handling
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const uint WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                int screenWidth = GetSystemMetrics(0);
                int screenHeight = GetSystemMetrics(1);

                MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                minMaxInfo.ptMinTrackSize.X = (int)(screenWidth * MinWidth);
                minMaxInfo.ptMinTrackSize.Y = (int)(screenHeight * MinHeight);
                Marshal.StructureToPtr(minMaxInfo, lParam, false);

                return IntPtr.Zero;
            }

            var window = App.MainWindow as MainWindow;
            if (window != null)
            {
                return CallWindowProc(window._originalWndProc, hWnd, msg, wParam, lParam);
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
        #endregion

        #region Win32 Interop
        private const int GWLP_WNDPROC = -4;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        #endregion

        private void LogTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            LogTextBlock.LayoutUpdated += (_, _) =>
            {
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            };
        }
    }
}