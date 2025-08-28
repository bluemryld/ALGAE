using System.Windows;
using ALGAE.ViewModels;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for GameScanProgressDialog.xaml
    /// </summary>
    public partial class GameScanProgressDialog : Window
    {
        public GameScanProgressDialog()
        {
            InitializeComponent();
        }

        public GameScanProgressDialog(GameScanProgressViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is GameScanProgressViewModel viewModel && !viewModel.IsCompleted)
            {
                // If scan is still running, cancel it
                if (viewModel.CancelCommand.CanExecute(null))
                {
                    viewModel.CancelCommand.Execute(null);
                }
            }
        }
    }
}
