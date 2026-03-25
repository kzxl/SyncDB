using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using SyncDB.ViewModels;
using Application = System.Windows.Application;

namespace SyncDB
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _trayIcon;

        public MainWindow()
        {
            InitializeComponent();

            // Auto-scroll log
            var vm = (MainViewModel)DataContext;
            vm.LogEntries.CollectionChanged += (s, e) =>
            {
                if (LogListBox.Items.Count > 0)
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            };

            InitTray();
        }

        private void InitTray()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "SyncDB",
                Visible = false
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Mở SyncDB", null, (s, e) => ShowWindow());
            menu.Items.Add("-");
            menu.Items.Add("Thoát", null, (s, e) =>
            {
                _trayIcon.Visible = false;
                Application.Current.Shutdown();
            });
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _trayIcon.Visible = false;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            var vm = DataContext as MainViewModel;
            if (WindowState == WindowState.Minimized && vm != null && vm.MinimizeToTray)
            {
                Hide();
                _trayIcon.Visible = true;
                _trayIcon.ShowBalloonTip(2000, "SyncDB", "Ứng dụng đang chạy ẩn ở đây", ToolTipIcon.Info);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null && vm.MinimizeToTray)
            {
                // Ẩn xuống tray thay vì đóng
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                return;
            }
            vm?.OnClosing();
            _trayIcon?.Dispose();
        }
    }
}
