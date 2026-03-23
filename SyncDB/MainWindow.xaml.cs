using System.Windows;
using SyncDB.ViewModels;

namespace SyncDB
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Auto-scroll log to bottom
            var vm = (MainViewModel)DataContext;
            vm.LogEntries.CollectionChanged += (s, e) =>
            {
                if (LogListBox.Items.Count > 0)
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            };
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.OnClosing();
        }
    }
}
