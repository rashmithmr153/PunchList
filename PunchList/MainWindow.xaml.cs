using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PunchList.ViewModels;

namespace PunchList
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        private bool _isExplicitExit = false;
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            SourceInitialized += (_, _) => ApplyImmersiveDarkMode();

            try
            {
                var iconUri = new Uri("pack://application:,,,/Resources/logo.ico");
                var streamInfo = Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    using var stream = streamInfo.Stream;
                    TrayIcon.Icon = new System.Drawing.Icon(stream);
                }
            }
            catch
            {
                TrayIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            Loaded += async (_, _) =>
            {
                await _viewModel.LoadDataAsync();
            };
        }

        private void ApplyImmersiveDarkMode()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;

                int useDarkMode = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));

                // Match caption color to #111113 (COLORREF 0x00BBGGRR)
                int captionColor = 0x00131111;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                // Match caption text to #F2F2F3
                int textColor = 0x00F3F2F2;
                DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
            }
            catch
            {
                // Silently fallback on older Windows releases
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                e.Cancel = true;
                this.Hide(); // Hides window, keeps background services & tray icon alive
                return;
            }

            base.OnClosing(e);
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            _isExplicitExit = true;
            TrayIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void OnTrayLeftClick(object sender, RoutedEventArgs e) => ShowWindow();
        private void OnOpenClick(object sender, RoutedEventArgs e) => ShowWindow();
    }
}