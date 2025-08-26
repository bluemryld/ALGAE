using System.Windows;
using ALGAE.ViewModels;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow : Window
    {
        public LauncherWindow()
        {
            InitializeComponent();
        }

        public LauncherWindow(LauncherViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Hide(); // Hide instead of close so it can be reopened
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Hide instead of closing to allow reopening
            e.Cancel = true;
            Hide();
        }

        /// <summary>
        /// Show the launcher window and bring it to front
        /// </summary>
        public void ShowAndActivate()
        {
            Show();
            Activate();
            Focus();
        }
    }
}
