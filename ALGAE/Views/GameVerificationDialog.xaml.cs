using System.Windows;
using ALGAE.ViewModels;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for GameVerificationDialog.xaml
    /// </summary>
    public partial class GameVerificationDialog : Window
    {
        public GameVerificationDialog()
        {
            InitializeComponent();
        }

        public GameVerificationDialog(GameVerificationViewModel viewModel) : this()
        {
            DataContext = viewModel;
            
            // Subscribe to close requests from the view model
            viewModel.CloseRequested += (s, e) => 
            {
                DialogResult = e;
                Close();
            };
        }
    }
}
