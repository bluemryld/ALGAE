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

        public HomeViewModel(IServiceProvider serviceProvider, IGameRepository gameRepository, INotificationService notificationService)
        {
            _serviceProvider = serviceProvider;
            _gameRepository = gameRepository;
            _notificationService = notificationService;
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
                _notificationService.ShowInformation("Game scanning functionality will be implemented in a future update.");
                
                // TODO: Implement game scanning
                // This would scan common game directories and auto-add discovered games
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error scanning for games: {ex.Message}");
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
    }
}
