using System.Windows;
using System.Windows.Controls;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for GameDetailView.xaml
    /// </summary>
    public partial class GameDetailView : UserControl
    {
        public GameDetailView()
        {
            InitializeComponent();
        }
        
        private void ProfileDropdownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Get the view model
                if (DataContext is ALGAE.ViewModels.GameDetailViewModel viewModel)
                {
                    // Clear existing menu items
                    ProfileContextMenu.Items.Clear();
                    
                    // Add Default option
                    var defaultItem = new MenuItem
                    {
                        Header = "Default",
                        ToolTip = "Launch with default game settings",
                        Command = viewModel.LaunchGameCommand
                    };
                    ProfileContextMenu.Items.Add(defaultItem);
                    
                    // Add separator if there are profiles
                    if (viewModel.HasProfiles && viewModel.Profiles.Count > 0)
                    {
                        ProfileContextMenu.Items.Add(new Separator());
                        
                        // Add profile items
                        foreach (var profile in viewModel.Profiles)
                        {
                            var profileItem = new MenuItem
                            {
                                Header = profile.ProfileName,
                                ToolTip = string.IsNullOrEmpty(profile.CommandLineArgs) ? "No special arguments" : $"Arguments: {profile.CommandLineArgs}",
                                Command = viewModel.LaunchWithProfileCommand,
                                CommandParameter = profile
                            };
                            ProfileContextMenu.Items.Add(profileItem);
                        }
                    }
                }
                
                // Show the context menu
                ProfileContextMenu.PlacementTarget = button;
                ProfileContextMenu.IsOpen = true;
            }
        }
    }
}
