using System.Windows.Controls;
using ALGAE.ViewModels;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for CompanionsView.xaml
    /// </summary>
    public partial class CompanionsView : UserControl
    {
        public CompanionsView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is CompanionsViewModel viewModel)
            {
                await viewModel.LoadCompanionsAsync();
            }
        }
    }
}
