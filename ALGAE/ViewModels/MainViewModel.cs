using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ALGAE.Views;
using Microsoft.Extensions.DependencyInjection;
using ALGAE.DAL.Repositories;
using ALGAE.Services;
using System.Windows;

namespace ALGAE.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private object? _currentView;
        private MenuItem? _selectedMenuItem;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggingService _logger;

        public ObservableCollection<MenuItem> MenuItems { get; set; }
        
        public object? CurrentView
        {
            get => _currentView;
            set 
            {
                _logger.LogDebug("MainViewModel", $"Setting CurrentView to: {value?.GetType().Name ?? "null"}");
                SetProperty(ref _currentView, value);
            }
        }

        public MenuItem? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                _logger.LogInformation("MainViewModel", $"SelectedMenuItem setter called with: {value?.Name ?? "null"}");
                if (SetProperty(ref _selectedMenuItem, value))
                {
                    _logger.LogInformation("MainViewModel", $"SelectedMenuItem changed, switching view to: {value?.Name ?? "null"}");
                    SwitchView(_selectedMenuItem);
                }
                else
                {
                    _logger.LogDebug("MainViewModel", "SelectedMenuItem setter called but value was the same, no change");
                }
            }
        }

        public MainViewModel(IServiceProvider serviceProvider, ILoggingService logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Initialize menu items
            MenuItems = new ObservableCollection<MenuItem>
            {
                new MenuItem { Name = "Home", ViewName = "Home", Icon = "pack://application:,,,/Resources/home_icon.png" },
                new MenuItem { Name = "Games", ViewName = "Games", Icon = "pack://application:,,,/Resources/games_icon.png" },
                new MenuItem { Name = "Signatures", ViewName = "Signatures", Icon = "pack://application:,,,/Resources/signatures_icon.png" },
                new MenuItem { Name = "Companions", ViewName = "Companions", Icon = "pack://application:,,,/Resources/apps_icon.png" },
                new MenuItem { Name = "Launcher", ViewName = "Launcher", Icon = "pack://application:,,,/Resources/launcher_icon.png" },
                new MenuItem { Name = "Settings", ViewName = "Settings", Icon = "pack://application:,,,/Resources/settings_icon.png" },
            };

            // Set initial view
            _currentView = CreateHomeView();
            
            // Set default selection
            _selectedMenuItem = MenuItems.FirstOrDefault();
            _logger.LogInformation("MainViewModel", $"MainViewModel initialized. Selected menu item: {_selectedMenuItem?.Name ?? "null"}");
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
        private void NavigateToCompanions()
        {
            var companionsMenuItem = MenuItems.FirstOrDefault(m => m.ViewName == "Companions");
            if (companionsMenuItem != null)
            {
                SelectedMenuItem = companionsMenuItem;
            }
        }
        
        [RelayCommand]
        private void NavigateToSignatures()
        {
            _logger.LogInformation("MainViewModel", "NavigateToSignatures: Command triggered");
            var signaturesMenuItem = MenuItems.FirstOrDefault(m => m.ViewName == "Signatures");
            _logger.LogDebug("MainViewModel", $"NavigateToSignatures: Found menu item: {signaturesMenuItem?.Name ?? "null"}");
            if (signaturesMenuItem != null)
            {
                _logger.LogDebug("MainViewModel", $"NavigateToSignatures: Setting SelectedMenuItem to {signaturesMenuItem.Name}");
                SelectedMenuItem = signaturesMenuItem;
            }
        }
        
        [RelayCommand]
        private void NavigateToSettings()
        {
            _logger.LogInformation("MainViewModel", "NavigateToSettings: Command triggered");
            var settingsMenuItem = MenuItems.FirstOrDefault(m => m.ViewName == "Settings");
            _logger.LogDebug("MainViewModel", $"NavigateToSettings: Found menu item: {settingsMenuItem?.Name ?? "null"}");
            if (settingsMenuItem != null)
            {
                _logger.LogDebug("MainViewModel", $"NavigateToSettings: Setting SelectedMenuItem to {settingsMenuItem.Name}");
                SelectedMenuItem = settingsMenuItem;
            }
        }
        
        [RelayCommand]
        private void NavigateToLauncher()
        {
            try
            {
                var launcherWindow = _serviceProvider.GetRequiredService<LauncherWindow>();
                launcherWindow.ShowAndActivate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening launcher window: {ex.Message}");
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

        [RelayCommand]
        private void ManageSearchPaths()
        {
            try
            {
                var searchPathRepository = _serviceProvider.GetRequiredService<ISearchPathRepository>();
                var dialog = new SearchPathManagementDialog(searchPathRepository)
                {
                    Owner = Application.Current.MainWindow
                };
                
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening search path management dialog: {ex.Message}");
                MessageBox.Show($"Error opening search path management dialog: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        private object? CreateCompanionsView()
        {
            try
            {
                var companionsViewModel = _serviceProvider.GetRequiredService<CompanionsViewModel>();
                var companionsView = new CompanionsView { DataContext = companionsViewModel };
                return companionsView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating CompanionsView: {ex.Message}");
                return null;
            }
        }

        private object? CreateGameSignaturesView()
        {
            try
            {
                _logger.LogDebug("MainViewModel", "CreateGameSignaturesView: Starting to create view");
                var gameSignaturesViewModel = _serviceProvider.GetRequiredService<ALGAE.ViewModels.GameSignaturesViewModel>();
                _logger.LogDebug("MainViewModel", "CreateGameSignaturesView: ViewModel resolved successfully");
                var gameSignaturesView = new GameSignaturesView { DataContext = gameSignaturesViewModel };
                _logger.LogDebug("MainViewModel", "CreateGameSignaturesView: View created successfully");
                return gameSignaturesView;
            }
            catch (Exception ex)
            {
                _logger.LogError("MainViewModel", $"Error creating GameSignaturesView: {ex.Message}", ex);
                return null;
            }
        }

        private object? CreateSettingsView()
        {
            try
            {
                _logger.LogDebug("MainViewModel", "CreateSettingsView: Starting to create view");
                var settingsViewModel = _serviceProvider.GetRequiredService<ALGAE.ViewModels.SettingsViewModel>();
                _logger.LogDebug("MainViewModel", "CreateSettingsView: ViewModel resolved successfully");
                var settingsView = new SettingsView { DataContext = settingsViewModel };
                _logger.LogDebug("MainViewModel", "CreateSettingsView: View created successfully");
                return settingsView;
            }
            catch (Exception ex)
            {
                _logger.LogError("MainViewModel", $"Error creating SettingsView: {ex.Message}", ex);
                return null;
            }
        }

        private void SwitchView(MenuItem? menuItem)
        {
            _logger.LogInformation("MainViewModel", $"SwitchView called with menuItem: {menuItem?.Name ?? "null"}");
            if (menuItem != null && !string.IsNullOrEmpty(menuItem.ViewName))
            {
                _logger.LogInformation("MainViewModel", $"Switching to view: {menuItem.ViewName}");
                
                // Skip launcher as it opens in separate window
                if (menuItem.ViewName == "Launcher")
                {
                    _logger.LogInformation("MainViewModel", "Opening Launcher in separate window");
                    NavigateToLauncher();
                    return;
                }
                
                _logger.LogDebug("MainViewModel", $"Creating view for ViewName: '{menuItem.ViewName}'");
                
                object? newView = menuItem.ViewName switch
                {
                    "Home" => CreateHomeView(),
                    "Games" => CreateGamesView(),
                    "Signatures" => CreateGameSignaturesView(),
                    "Companions" => CreateCompanionsView(),
                    "Settings" => CreateSettingsView(),
                    _ => null
                };
                
                _logger.LogDebug("MainViewModel", $"Created view: {(newView != null ? newView.GetType().Name : "null")}");
                
                if (newView != null)
                {
                    CurrentView = newView;
                    _logger.LogInformation("MainViewModel", $"Successfully set CurrentView to: {newView.GetType().Name}");
                }
                else
                {
                    _logger.LogError("MainViewModel", $"Failed to create view for ViewName: '{menuItem.ViewName}'");
                }
            }
            else
            {
                _logger.LogWarning("MainViewModel", "SwitchView called with null or empty ViewName");
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
