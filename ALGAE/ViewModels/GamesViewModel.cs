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
        private readonly IGameLaunchService _gameLaunchService;

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
            ICompanionLaunchService companionLaunchService,
            IGameLaunchService gameLaunchService)
        {
            _gameRepository = gameRepository;
            _notificationService = notificationService;
            _processMonitor = processMonitor;
            _serviceProvider = serviceProvider;
            _profilesRepository = profilesRepository;
            _companionLaunchService = companionLaunchService;
            _gameLaunchService = gameLaunchService;
            Games.CollectionChanged += OnGamesCollectionChanged;
            PropertyChanged += OnPropertyChanged;
            
            // Load games asynchronously without blocking constructor
            Task.Run(async () => await LoadGamesAsync());
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

        private void OnGamesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateFilteredGames();
        }

        private void UpdateFilteredGames()
        {
            // Clear and repopulate the existing collection instead of creating a new one
            FilteredGames.Clear();
            
            IEnumerable<Game> gamesToShow;
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                gamesToShow = Games.OrderBy(g => g.Name);
                IsFiltered = false;
            }
            else
            {
                gamesToShow = Games.Where(g => 
                    g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (g.Publisher?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (g.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (g.Version?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (g.ShortName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                ).OrderBy(g => g.Name);
                IsFiltered = true;
            }
            
            // Add items to existing collection to avoid recreating it
            foreach (var game in gamesToShow)
            {
                FilteredGames.Add(game);
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
                
                // Temporarily disconnect the collection changed event to avoid multiple updates
                Games.CollectionChanged -= OnGamesCollectionChanged;
                
                try
                {
                    // Clear and add all items at once to minimize UI updates
                    Games.Clear();
                    var gameList = games.ToList();
                    foreach (var game in gameList)
                    {
                        Games.Add(game);
                    }
                }
                finally
                {
                    // Reconnect the collection changed event
                    Games.CollectionChanged += OnGamesCollectionChanged;
                }
                
                // Manually trigger a single update after all games are loaded
                UpdateFilteredGames();
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
                // Open the launcher window immediately to show launch progress
                try
                {
                    var launcherWindow = _serviceProvider.GetService<LauncherWindow>();
                    if (launcherWindow != null)
                    {
                        launcherWindow.Show();
                        launcherWindow.Activate();
                        Debug.WriteLine("Launcher window opened to show launch progress");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error opening launcher window: {ex.Message}");
                    // Continue with launch even if launcher window fails to open
                }

                // If using a profile, override the game arguments
                Game gameToLaunch = game;
                if (profile != null && !string.IsNullOrEmpty(profile.CommandLineArgs))
                {
                    // Create a copy of the game with profile-specific arguments
                    gameToLaunch = new Game
                    {
                        GameId = game.GameId,
                        Name = game.Name,
                        ShortName = game.ShortName,
                        Description = game.Description,
                        GameImage = game.GameImage,
                        ThemeName = game.ThemeName,
                        InstallPath = game.InstallPath,
                        GameWorkingPath = game.GameWorkingPath,
                        ExecutableName = game.ExecutableName,
                        GameArgs = profile.CommandLineArgs, // Use profile arguments
                        Version = game.Version,
                        Publisher = game.Publisher
                    };
                    Debug.WriteLine($"LaunchGameWithProfileAsync: Using profile '{profile.ProfileName}' arguments: {profile.CommandLineArgs}");
                }
                else
                {
                    Debug.WriteLine($"LaunchGameWithProfileAsync: Using default game arguments: {game.GameArgs}");
                }

                // First validate the game using the launch service
                var validation = await _gameLaunchService.ValidateGameAsync(gameToLaunch);
                if (!validation.IsValid)
                {
                    var editGame = await _notificationService.ShowWarningConfirmationAsync(
                        "Cannot Launch Game", 
                        $"{validation.ErrorMessage}\n\nWould you like to edit the game settings?",
                        "Edit Game", "Cancel");
                        
                    if (editGame)
                    {
                        await EditGameAsync(game);
                    }
                    return;
                }

                // Show warnings if any
                if (validation.Warnings.Any())
                {
                    foreach (var warning in validation.Warnings)
                    {
                        Debug.WriteLine($"Game launch warning: {warning}");
                    }
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

                // Launch the game using the launch service
                var launchResult = await _gameLaunchService.LaunchGameAsync(gameToLaunch);
                
                if (launchResult.Success)
                {
                    var profileInfo = profile != null ? $" with profile '{profile.ProfileName}'" : "";
                    _notificationService.ShowSuccess($"Successfully launched {game.Name}{profileInfo}");
                    Debug.WriteLine($"Successfully launched {game.Name} (PID: {launchResult.Process?.Id}){profileInfo}");
                }
                else
                {
                    _notificationService.ShowError($"Failed to start {game.Name}: {launchResult.ErrorMessage}");
                    Debug.WriteLine($"Failed to launch {game.Name}: {launchResult.ErrorMessage}");
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
                // Get the game detection service
                var gameDetectionService = _serviceProvider.GetRequiredService<IGameDetectionService>();
                
                // Get search paths first
                var searchPaths = await gameDetectionService.GetSearchPathsAsync();
                var directories = searchPaths.Select(sp => sp.Path).ToList();

                if (!directories.Any())
                {
                    // If no search paths configured, get common game directories
                    directories = await gameDetectionService.GetCommonGameDirectoriesAsync();
                    
                    if (!directories.Any())
                    {
                        _notificationService.ShowWarning("No game directories found to scan. Please configure search paths in settings.");
                        return;
                    }
                }

                // Create and show progress dialog
                var progressViewModel = new GameScanProgressViewModel(gameDetectionService, _notificationService);
                var progressDialog = new Views.GameScanProgressDialog(progressViewModel)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                // Handle dialog close and scan completion
                progressViewModel.CloseRequested += (s, e) => progressDialog.Close();

                // Set up scan completion handler to show verification dialog
                progressViewModel.ScanCompleted += async (s, e) =>
                {
                    try
                    {
                        // Process results if scan completed successfully
                        if (progressViewModel.Result != null && !progressViewModel.WasCancelled)
                        {
                            var result = progressViewModel.Result;
                            
                            if (result.DetectedGames.Any())
                            {
                                // Show verification dialog for user approval
                                await Application.Current.Dispatcher.InvokeAsync(async () =>
                                {
                                    await ShowGameVerificationDialog(result.DetectedGames.ToList());
                                });
                            }
                            else
                            {
                                _notificationService.ShowInformation("No games were detected in the scanned directories.");
                            }
                            
                            if (result.Errors.Any())
                            {
                                var errorMessage = string.Join("\n", result.Errors.Take(3));
                                _notificationService.ShowWarning($"Scan completed with some errors:\n{errorMessage}");
                            }
                        }
                        else if (progressViewModel.WasCancelled)
                        {
                            _notificationService.ShowInformation("Game scan was cancelled.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _notificationService.ShowError($"Error processing scan results: {ex.Message}");
                    }
                };

                // Start the scan asynchronously (don't await here)
                _ = progressViewModel.StartScanAsync(directories);
                
                // Show the dialog (non-blocking)
                progressDialog.Show();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error during game scan: {ex.Message}");
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
        
        private async Task ShowGameVerificationDialog(List<DetectedGame> detectedGames)
        {
            try
            {
                var verificationViewModel = new GameVerificationViewModel(detectedGames);
                var verificationDialog = new GameVerificationDialog(verificationViewModel)
                {
                    Owner = Application.Current.MainWindow
                };
                
                var result = verificationDialog.ShowDialog();
                
                if (result == true)
                {
                    var selectedGames = verificationViewModel.GetSelectedGames();
                    if (selectedGames.Any())
                    {
                        var gameDetectionService = _serviceProvider.GetRequiredService<IGameDetectionService>();
                        var addedCount = await gameDetectionService.AddDetectedGamesToLibraryAsync(selectedGames);
                        
                        // Refresh the games list to show new games
                        await LoadGamesAsync();
                        _notificationService.ShowSuccess($"Added {addedCount} games to your library!");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error showing game verification dialog: {ex.Message}");
            }
        }
    }
}
