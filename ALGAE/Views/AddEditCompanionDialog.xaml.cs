using System.Windows;
using ALGAE.ViewModels;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for AddEditCompanionDialog.xaml
    /// </summary>
    public partial class AddEditCompanionDialog : Window
    {
        public AddEditCompanionDialog()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as AddEditCompanionViewModel;
            if (viewModel != null && viewModel.ValidateCompanion())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
