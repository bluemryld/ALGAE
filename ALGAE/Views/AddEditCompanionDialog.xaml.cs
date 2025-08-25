using System.Windows;
using Algae.DAL.Models;
using Microsoft.Win32;

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
            var companion = DataContext as Companion;
            if (companion != null && 
                !string.IsNullOrWhiteSpace(companion.Name) && 
                !string.IsNullOrWhiteSpace(companion.Type) &&
                !string.IsNullOrWhiteSpace(companion.PathOrURL))
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

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var companion = DataContext as Companion;
            if (companion == null) return;

            if (companion.Type == "Executable" || companion.Type == "Script")
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Application",
                    Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    companion.PathOrURL = openFileDialog.FileName;
                }
            }
            else if (companion.Type == "Document")
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Document",
                    Filter = "All files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    companion.PathOrURL = openFileDialog.FileName;
                }
            }
        }
    }
}
