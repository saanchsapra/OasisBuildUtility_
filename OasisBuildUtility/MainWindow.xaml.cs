using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OasisBuildUtility.ViewModel;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace OasisBuildUtility
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();
        private AppWindow _appWindow;
        private IntPtr _hWnd;
        private IntPtr _originalWndProc;
        private readonly WndProcDelegate _wndProcDelegate;

        private const double MinWidth = 0.8;
        private const double MinHeight = 0.7;

        public MainWindow()
        {
            this.InitializeComponent();
            App.MainWindow = this;
            _wndProcDelegate = WndProc;
            InitializeAppWindow();
            SetupWindowMessageHandling();
            SetInitialWindowSize();
        }

        private async void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AppendLogText("Build button clicked!");
            // You can add your build logic here or call a method in ViewModel
        }

        private async void BrowseJavaSource_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SelectJavaSourcePathAsync();
        }

        private async void BrowseNativeSource_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.SelectNativeSourcePathAsync();
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearLog();
        }

        private void InitializeAppWindow()
        {
            _hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
        }

        private void SetupWindowMessageHandling()
        {
            _originalWndProc = SetWindowLongPtr(_hWnd, GWLP_WNDPROC, _wndProcDelegate);
        }

        private void SetInitialWindowSize()
        {
            int minWidth = (int)(GetSystemMetrics(0) * MinWidth);
            int minHeight = (int)(GetSystemMetrics(1) * MinHeight);
            _appWindow.Resize(new SizeInt32(minWidth, minHeight));
        }

        #region Window Message Handling
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
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

            return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
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
    }
}