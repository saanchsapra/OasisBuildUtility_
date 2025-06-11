using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;
using System;
using OasisBuildUtility.ViewModel;
using Microsoft.UI;
using System.Runtime.InteropServices;

namespace OasisBuildUtility
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();
        private AppWindow _appWindow;
        private IntPtr _hWnd;
        private const int MinWidth = 1500;
        private const int MinHeight = 1200;

        public MainWindow()
        {
            this.InitializeComponent();
            App.MainWindow = this;
            InitializeAppWindow();
            SetupWindowMessageHandling();
            SetInitialWindowSize();
        }

        private void InitializeAppWindow()
        {
            _hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
        }

        private void SetupWindowMessageHandling()
        {
            // Subclass the window to handle WM_GETMINMAXINFO
            _originalWndProc = SetWindowLongPtr(_hWnd, GWLP_WNDPROC, _wndProcDelegate);
        }

        private void SetInitialWindowSize()
        {
            _appWindow.Resize(new SizeInt32(MinWidth, MinHeight));
        }

        #region Window Message Handling
        private IntPtr _originalWndProc;
        private readonly WndProcDelegate _wndProcDelegate = WndProc;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const uint WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                minMaxInfo.ptMinTrackSize.X = MinWidth;
                minMaxInfo.ptMinTrackSize.Y = MinHeight;
                Marshal.StructureToPtr(minMaxInfo, lParam, false);
                return IntPtr.Zero;
            }

            // Get the original window procedure for this window
            var window = App.MainWindow;
            if (window != null)
            {
                return CallWindowProc(((MainWindow)window)._originalWndProc, hWnd, msg, wParam, lParam);
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

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        #endregion
    }
}