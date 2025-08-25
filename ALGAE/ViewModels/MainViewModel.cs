using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ALGAE.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ALGAE.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private object? _currentView;
        private MenuItem? _selectedMenuItem;
        private readonly IServiceProvider _serviceProvider;

        public ObservableCollection<MenuItem> MenuItems { get; set; }
        
        public object? CurrentView
        {
            get => _currentView;
            set 
            {
                System.Diagnostics.Debug.WriteLine($"Setting CurrentView to: {value?.GetType().Name ?? "null"}");
                SetProperty(ref _currentView, value);
            }
        }

        public MenuItem? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                System.Diagnostics.Debug.WriteLine($"Setting SelectedMenuItem to: {value?.Name ?? "null"}");
                if (SetProperty(ref _selectedMenuItem, value))
                {
                    System.Diagnostics.Debug.WriteLine($"SelectedMenuItem changed, switching view to: {value?.Name ?? "null"}");
                    SwitchView(_selectedMenuItem);
                }
            }
        }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize menu items
            MenuItems = new ObservableCollection<MenuItem>
            {
                new MenuItem { Name = "Home", ViewName = "Home", Icon = "pack://application:,,,/Resources/home_icon.png" },
                new MenuItem { Name = "Games", ViewName = "Games", Icon = "pack://application:,,,/Resources/games_icon.png" },
                new MenuItem { Name = "Launcher", ViewName = "Launcher", Icon = "pack://application:,,,/Resources/launcher_icon.png" },
            };

            // Set initial view
            _currentView = CreateHomeView();
            
            // Set default selection
            _selectedMenuItem = MenuItems.FirstOrDefault();
            System.Diagnostics.Debug.WriteLine($"MainViewModel constructor: Selected menu item set to: {_selectedMenuItem?.Name ?? "null"}");
        }
        
        [RelayCommand]
        private void NavigateToHome()
        {
            var homeMenuItem = MenuItems.FirstOrDefault(m => m.ViewName == "Home");
            if (homeMenuItem != null)
            {
                SelectedMenuItem = homeMenuItem;
            }
        }
        
        [RelayCommand]
        private void NavigateToGames()
        {
            var gamesMenuItem = MenuItems.FirstOrDefault(m => m.ViewName == "Games");
            if (gamesMenuItem != null)
            {
                SelectedMenuItem = gamesMenuItem;
            }
        }
        
        [RelayCommand]
        private void NavigateToLauncher()
        {
            var launcherMenuItem = MenuItems.FirstOrDefault(m => m.ViewName == "Launcher");
            if (launcherMenuItem != null)
            {
                SelectedMenuItem = launcherMenuItem;
            }
        }
        
        [RelayCommand]
        private void RefreshCurrentView()
        {
            // Refresh the current view by recreating it
            if (_selectedMenuItem != null)
            {
                SwitchView(_selectedMenuItem);
            }
        }


        private object? CreateHomeView()
        {
            try
            {
                var homeViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
                var homeView = new HomeView { DataContext = homeViewModel };
                return homeView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating HomeView: {ex.Message}");
                return null;
            }
        }

        private object? CreateGamesView()
        {
            try
            {
                var gamesViewModel = _serviceProvider.GetRequiredService<GamesViewModel>();
                var gamesView = new GamesView { DataContext = gamesViewModel };
                return gamesView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating GamesView: {ex.Message}");
                return null;
            }
        }

        private object? CreateLauncherView()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CreateLauncherView: Starting to create LauncherView");
                var launcherViewModel = _serviceProvider.GetRequiredService<LauncherViewModel>();
                System.Diagnostics.Debug.WriteLine("CreateLauncherView: LauncherViewModel created successfully");
                var launcherView = new LauncherView { DataContext = launcherViewModel };
                System.Diagnostics.Debug.WriteLine("CreateLauncherView: LauncherView created successfully");
                return launcherView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating LauncherView: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        private void SwitchView(MenuItem? menuItem)
        {
            System.Diagnostics.Debug.WriteLine($"SwitchView called with menuItem: {menuItem?.Name ?? "null"}");
            if (menuItem != null && !string.IsNullOrEmpty(menuItem.ViewName))
            {
                System.Diagnostics.Debug.WriteLine($"Switching to view: {menuItem.ViewName}");
                
                object? newView = menuItem.ViewName switch
                {
                    "Home" => CreateHomeView(),
                    "Games" => CreateGamesView(),
                    "Launcher" => CreateLauncherView(),
                    _ => null
                };
                
                if (newView != null)
                {
                    CurrentView = newView;
                    System.Diagnostics.Debug.WriteLine($"Successfully set CurrentView to: {newView.GetType().Name}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create view for: {menuItem.ViewName}");
                }
            }
        }
    }

    public class MenuItem
    {
        public string Name { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
