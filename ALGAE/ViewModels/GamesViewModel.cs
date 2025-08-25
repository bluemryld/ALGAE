using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;
using ALGAE.Views;
using ALGAE.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ALGAE.ViewModels
{
    public partial class GamesViewModel : ObservableObject
    {
        private readonly IGameRepository _gameRepository;
        private readonly INotificationService _notificationService;
        private readonly IGameProcessMonitorService _processMonitor;
        private readonly IServiceProvider _serviceProvider;
        private readonly IProfilesRepository _profilesRepository;
        private readonly ICompanionLaunchService _companionLaunchService;

        [ObservableProperty]
        private ObservableCollection<Game> _games = new();

        [ObservableProperty]
        private ObservableCollection<Game> _filteredGames = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isEmpty;
        
        [ObservableProperty]
        private bool _isFiltered;
        
        [ObservableProperty]
        private bool _hasSearchText;

        public GamesViewModel(
            IGameRepository gameRepository, 
            INotificationService notificationService, 
            IGameProcessMonitorService processMonitor, 
            IServiceProvider serviceProvider,
            IProfilesRepository profilesRepository,
            ICompanionLaunchService companionLaunchService)
        {
            _gameRepository = gameRepository;
            _notificationService = notificationService;
            _processMonitor = processMonitor;
            _serviceProvider = serviceProvider;
            _profilesRepository = profilesRepository;
            _companionLaunchService = companionLaunchService;
            Games.CollectionChanged += (s, e) => UpdateFilteredGames();
            PropertyChanged += OnPropertyChanged;
            
            // Load games on startup
            _ = LoadGamesAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            HasSearchText = !string.IsNullOrWhiteSpace(value);
            UpdateFilteredGames();
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Games))
            {
                IsEmpty = !Games.Any();
            }
        }

        private void UpdateFilteredGames()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredGames = new ObservableCollection<Game>(Games.OrderBy(g => g.Name));
                IsFiltered = false;
            }
            else
            {
                var filtered = Games.Where(g => 
                    g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (g.Publisher?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (g.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (g.Version?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (g.ShortName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                ).OrderBy(g => g.Name);
                FilteredGames = new ObservableCollection<Game>(filtered);
                IsFiltered = true;
            }
            IsEmpty = !FilteredGames.Any();
        }

        [RelayCommand]
        private async Task LoadGamesAsync()
        {
            try
            {
                IsLoading = true;
                var games = await _gameRepository.GetAllAsync();
                Games.Clear();
                foreach (var game in games)
                {
                    Games.Add(game);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading games: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadGamesAsync();
        }

        [RelayCommand]
        private async Task AddGameAsync()
        {
            var dialog = new AddEditGameDialog();
            if (dialog.ShowDialog() == true && dialog.Game != null)
            {
                try
                {
                    await _gameRepository.AddAsync(dialog.Game);
                    Games.Add(dialog.Game);
                    _notificationService.ShowSuccess("Game added successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error adding game: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task EditGameAsync(Game game)
        {
            if (game == null) return;

            var dialog = new AddEditGameDialog(game);
            if (dialog.ShowDialog() == true && dialog.Game != null)
            {
                try
                {
                    await _gameRepository.UpdateAsync(dialog.Game);
                    
                    // Update the game in the collection
                    var index = Games.IndexOf(game);
                    if (index >= 0)
                    {
                        Games[index] = dialog.Game;
                    }
                    
                    _notificationService.ShowSuccess("Game updated successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error updating game: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteGameAsync(Game game)
        {
            if (game == null) return;

            var confirmed = await _notificationService.ShowWarningConfirmationAsync(
                "Confirm Delete", 
                $"Are you sure you want to delete '{game.Name}'? This action cannot be undone.",
                "Delete", "Cancel");

            if (confirmed)
            {
                try
                {
                    await _gameRepository.DeleteAsync(game.GameId);
                    Games.Remove(game);
                    _notificationService.ShowSuccess("Game deleted successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error deleting game: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task LaunchGameAsync(Game game)
        {
            if (game == null) return;
            await LaunchGameWithProfileAsync(game, null);
        }

        /// <summary>
        /// Launch a game with a specific profile and its associated companions
        /// </summary>
        /// <param name="game">The game to launch</param>
        /// <param name="profile">The profile to use (null for default launch)</param>
        public async Task LaunchGameWithProfileAsync(Game game, Profile? profile)
        {
            if (game == null) return;

            try
            {
                // Validate game executable exists
                var executablePath = Path.Combine(game.InstallPath, game.ExecutableName ?? "");
                
                if (string.IsNullOrEmpty(game.ExecutableName))
                {
                    var editGame = await _notificationService.ShowWarningConfirmationAsync(
                        "Cannot Launch Game", 
                        $"No executable is specified for '{game.Name}'. Would you like to edit the game settings?",
                        "Edit Game", "Cancel");
                        
                    if (editGame)
                    {
                        await EditGameAsync(game);
                    }
                    return;
                }

                if (!File.Exists(executablePath))
                {
                    var editGame = await _notificationService.ShowWarningConfirmationAsync(
                        "Cannot Launch Game", 
                        $"Game executable not found at: {executablePath}\n\nThe file may have been moved or deleted. Would you like to edit the game settings?",
                        "Edit Game", "Cancel");
                        
                    if (editGame)
                    {
                        await EditGameAsync(game);
                    }
                    return;
                }

                // Check if working directory exists
                var workingDir = game.GameWorkingPath ?? game.InstallPath;
                if (!Directory.Exists(workingDir))
                {
                    _notificationService.ShowWarning($"Working directory not found: {workingDir}. Using executable directory instead.");
                    workingDir = Path.GetDirectoryName(executablePath) ?? game.InstallPath;
                }

                // Determine arguments to use
                string gameArguments;
                if (profile != null && !string.IsNullOrEmpty(profile.CommandLineArgs))
                {
                    gameArguments = profile.CommandLineArgs;
                    Debug.WriteLine($"LaunchGameWithProfileAsync: Using profile '{profile.ProfileName}' arguments: {gameArguments}");
                }
                else
                {
                    gameArguments = game.GameArgs ?? "";
                    Debug.WriteLine($"LaunchGameWithProfileAsync: Using default game arguments: {gameArguments}");
                }

                // Launch companions first if using a profile
                if (profile != null)
                {
                    Debug.WriteLine($"LaunchGameWithProfileAsync: Launching companions for profile {profile.ProfileId}");
                    try
                    {
                        var launchedCompanions = await _companionLaunchService.LaunchCompanionsForProfileAsync(profile.ProfileId);
                        Debug.WriteLine($"LaunchGameWithProfileAsync: Successfully launched {launchedCompanions.Count()} companions");
                    }
                    catch (Exception companionEx)
                    {
                        Debug.WriteLine($"LaunchGameWithProfileAsync: Error launching companions: {companionEx.Message}");
                        _notificationService.ShowWarning($"Some companions failed to start: {companionEx.Message}");
                    }
                }

                // Launch the game
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = workingDir,
                    Arguments = gameArguments,
                    UseShellExecute = true
                };

                var process = Process.Start(startInfo);
                
                if (process != null)
                {
                    // Start monitoring the game process
                    _processMonitor.StartMonitoring(game, process);
                    
                    var profileInfo = profile != null ? $" with profile '{profile.ProfileName}'" : "";
                    _notificationService.ShowSuccess($"Successfully launched {game.Name}{profileInfo}");
                    Debug.WriteLine($"Successfully launched {game.Name} (PID: {process.Id}){profileInfo}");
                }
                else
                {
                    _notificationService.ShowError($"Failed to start {game.Name}. The process could not be created.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LaunchGameWithProfileAsync: Error launching {game.Name}: {ex.Message}");
                _notificationService.ShowError($"Error launching {game.Name}: {ex.Message}. Please check the game's installation and try again.");
            }
        }

        [RelayCommand]
        private async Task ScanGamesAsync()
        {
            try
            {
                _notificationService.ShowInformation("Game scanning functionality will be implemented in a future update.");
                
                // TODO: Implement game scanning using SearchPathRepository and KnownGameRepository
                // This would:
                // 1. Get all search paths from SearchPathRepository
                // 2. Scan directories for executables
                // 3. Match against KnownGameRepository patterns
                // 4. Auto-add discovered games
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error scanning for games: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Search()
        {
            UpdateFilteredGames();
        }
        
        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }
        
        [RelayCommand]
        private async Task ViewGameDetailsAsync(Game game)
        {
            if (game == null)
            {
                System.Diagnostics.Debug.WriteLine("ViewGameDetailsAsync: game is null");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"ViewGameDetailsAsync: Starting for game {game.Name}");
                
                // Create the detail window and view model
                System.Diagnostics.Debug.WriteLine("ViewGameDetailsAsync: Creating GameDetailWindow");
                var detailWindow = new GameDetailWindow();
                
                System.Diagnostics.Debug.WriteLine("ViewGameDetailsAsync: Getting GameDetailViewModel from service provider");
                var detailViewModel = _serviceProvider.GetRequiredService<GameDetailViewModel>();
                
                System.Diagnostics.Debug.WriteLine($"ViewGameDetailsAsync: Loading game data for GameId {game.GameId}");
                // Load the game data
                await detailViewModel.LoadGameAsync(game.GameId);
                
                System.Diagnostics.Debug.WriteLine("ViewGameDetailsAsync: Setting up window");
                // Set up the window
                detailWindow.DataContext = detailViewModel;
                detailWindow.Owner = Application.Current.MainWindow;
                
                System.Diagnostics.Debug.WriteLine("ViewGameDetailsAsync: Showing window");
                // Show the window
                detailWindow.Show();
                
                System.Diagnostics.Debug.WriteLine("ViewGameDetailsAsync: Window shown successfully");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error opening game details: {ex.Message}";
                _notificationService.ShowError(errorMsg);
                System.Diagnostics.Debug.WriteLine($"ViewGameDetailsAsync Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Also show in console for debugging
                Console.WriteLine($"ViewGameDetailsAsync Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
