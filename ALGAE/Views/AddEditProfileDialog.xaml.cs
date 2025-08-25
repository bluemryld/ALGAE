using System.Windows;
using Algae.DAL.Models;
using ALGAE.ViewModels;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for AddEditProfileDialog.xaml
    /// </summary>
    public partial class AddEditProfileDialog : Window
    {
        public Profile? Result { get; private set; }

        public AddEditProfileDialog()
        {
            InitializeComponent();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as AddEditProfileViewModel;
            if (viewModel != null && !string.IsNullOrWhiteSpace(viewModel.ProfileName))
            {
                Result = viewModel.ToProfile();
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
