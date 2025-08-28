using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ALGAE.DAL.Repositories;
using ALGAE.Views;
using ALGAE.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ALGAE.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGameRepository _gameRepository;
        private readonly INotificationService _notificationService;
        private readonly IGameDetectionService _gameDetectionService;

        public HomeViewModel(IServiceProvider serviceProvider, IGameRepository gameRepository, INotificationService notificationService, IGameDetectionService gameDetectionService)
        {
            _serviceProvider = serviceProvider;
            _gameRepository = gameRepository;
            _notificationService = notificationService;
            _gameDetectionService = gameDetectionService;
        }

        [RelayCommand]
        private async Task AddGameAsync()
        {
            try
            {
                var dialog = new AddEditGameDialog();
                if (dialog.ShowDialog() == true && dialog.Game != null)
                {
                    await _gameRepository.AddAsync(dialog.Game);
                    _notificationService.ShowSuccess("Game added successfully!");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error adding game: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ScanGamesAsync()
        {
            try
            {
                // Get search paths first
                var searchPaths = await _gameDetectionService.GetSearchPathsAsync();
                var directories = searchPaths.Select(sp => sp.Path).ToList();

                if (!directories.Any())
                {
                    // If no search paths configured, get common game directories
                    directories = await _gameDetectionService.GetCommonGameDirectoriesAsync();
                    
                    if (!directories.Any())
                    {
                        _notificationService.ShowWarning("No game directories found to scan. Please configure search paths in settings.");
                        return;
                    }
                }

                // Create and show progress dialog
                var progressViewModel = new GameScanProgressViewModel(_gameDetectionService, _notificationService);
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
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    ShowGameVerificationDialog(result.DetectedGames.ToList());
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
        private async Task ViewRecentGamesAsync()
        {
            try
            {
                // For now, just show the Games view
                // TODO: Could be enhanced to show only recently played games
                _notificationService.ShowInformation("This will show recently played games. For now, use the Games menu to see all games.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading recent games: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            try
            {
                _notificationService.ShowInformation("Settings functionality will be implemented in a future update.");
                
                // TODO: Open settings dialog/view
                // This would allow configuring search paths, preferences, etc.
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error opening settings: {ex.Message}");
            }
        }
        
        private async void ShowGameVerificationDialog(List<DetectedGame> detectedGames)
        {
            try
            {
                // Filter out games that already exist to avoid confusion
                var newGames = detectedGames.Where(g => !g.AlreadyExists).ToList();
                
                if (!newGames.Any())
                {
                    _notificationService.ShowInformation("All detected games are already in your library.");
                    return;
                }
                
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
                        var addedCount = await _gameDetectionService.AddDetectedGamesToLibraryAsync(selectedGames);
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
