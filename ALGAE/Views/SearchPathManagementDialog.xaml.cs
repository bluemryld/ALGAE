using ALGAE.DAL.Repositories;
using ALGAE.ViewModels;
using System.Windows;

namespace ALGAE.Views
{
    public partial class SearchPathManagementDialog : Window
    {
        public SearchPathManagementDialog(ISearchPathRepository searchPathRepository)
        {
            InitializeComponent();
            DataContext = new SearchPathManagementViewModel(searchPathRepository);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
