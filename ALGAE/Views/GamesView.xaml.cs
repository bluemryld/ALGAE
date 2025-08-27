using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;

namespace ALGAE.Views
{
    /// <summary>
    /// Interaction logic for GamesView.xaml
    /// </summary>
    public partial class GamesView : UserControl
    {
        public GamesView()
        {
            InitializeComponent();
        }
        
        private async void ProfileDropdownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DataContext is ALGAE.ViewModels.GamesViewModel gamesViewModel)
            {
                // Get the game from the button's Tag
                if (button.Tag is Game game)
                {
                    // Find the context menu
                    var contextMenu = button.ContextMenu;
                    if (contextMenu != null)
                    {
                        // Clear existing items
                        contextMenu.Items.Clear();
                        
                        // Add Default option
                        var defaultItem = new MenuItem
                        {
                            Header = "Default",
                            ToolTip = "Launch with default game settings",
                            Command = gamesViewModel.LaunchGameCommand,
                            CommandParameter = game
                        };
                        contextMenu.Items.Add(defaultItem);
                        
                        try
                        {
                            // Load profiles for this game
                            if (App.Current is ALGAE.App algaeApp)
                            {
                                var profilesRepository = algaeApp.Services.GetService(typeof(IProfilesRepository)) as IProfilesRepository;
                                if (profilesRepository != null)
                                {
                                    var profiles = await profilesRepository.GetAllByGameIdAsync(game.GameId);
                                    
                                    // Add separator and profiles if any exist
                                    if (profiles.Any())
                                    {
                                        contextMenu.Items.Add(new Separator());
                                        
                                        foreach (var profile in profiles)
                                        {
                                            var profileItem = new MenuItem
                                            {
                                                Header = profile.ProfileName,
                                                ToolTip = string.IsNullOrEmpty(profile.CommandLineArgs) ? "No special arguments" : $"Arguments: {profile.CommandLineArgs}",
                                                Tag = new { Game = game, Profile = profile }
                                            };
                                            profileItem.Click += async (s, args) => await gamesViewModel.LaunchGameWithProfileAsync(game, profile);
                                            contextMenu.Items.Add(profileItem);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error loading profiles for game {game.Name}: {ex.Message}");
                        }
                        
                        // Show the context menu
                        contextMenu.PlacementTarget = button;
                        contextMenu.IsOpen = true;
                    }
                }
            }
        }
    }
}
