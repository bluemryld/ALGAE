using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;
using ALGAE.Services;
using ALGAE.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ALGAE.ViewModels
{
    public partial class CompanionsViewModel : ObservableObject
    {
        private readonly ICompanionRepository _companionRepository;
        private readonly IGameRepository _gameRepository;
        private readonly INotificationService _notificationService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<Companion> _companions = new();

        [ObservableProperty]
        private ObservableCollection<CompanionDisplayItem> _companionDisplayItems = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _hasCompanions = false;

        [ObservableProperty]
        private string _filterType = "All";

        public ObservableCollection<string> FilterTypes { get; } = new()
        {
            "All", "Global", "Game-Specific", "Executable", "URL", "Document", "Script"
        };

        public CompanionsViewModel(
            ICompanionRepository companionRepository,
            IGameRepository gameRepository,
            INotificationService notificationService,
            IServiceProvider serviceProvider)
        {
            _companionRepository = companionRepository;
            _gameRepository = gameRepository;
            _notificationService = notificationService;
            _serviceProvider = serviceProvider;

            Companions.CollectionChanged += (s, e) => 
            {
                HasCompanions = Companions.Count > 0;
                FilterCompanions();
            };

            // Watch for search and filter changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchText) || e.PropertyName == nameof(FilterType))
                {
                    FilterCompanions();
                }
            };
        }

        public async Task LoadCompanionsAsync()
        {
            try
            {
                IsLoading = true;
                var companions = await _companionRepository.GetAllAsync();
                var games = await _gameRepository.GetAllAsync();
                var gamesDict = games.ToDictionary(g => g.GameId, g => g.Name);

                Companions.Clear();
                foreach (var companion in companions.OrderBy(c => c.Name))
                {
                    Companions.Add(companion);
                }

                await UpdateDisplayItemsAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading companions: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateDisplayItemsAsync()
        {
            try
            {
                var games = await _gameRepository.GetAllAsync();
                var gamesDict = games.ToDictionary(g => g.GameId, g => g.Name);

                var displayItems = Companions.Select(c => new CompanionDisplayItem
                {
                    Companion = c,
                    GameName = c.GameId.HasValue ? gamesDict.GetValueOrDefault(c.GameId.Value, "Unknown Game") : "Global",
                    IsGlobal = !c.GameId.HasValue
                }).ToList();

                CompanionDisplayItems.Clear();
                foreach (var item in displayItems)
                {
                    CompanionDisplayItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error updating display items: {ex.Message}");
            }
        }

        private void FilterCompanions()
        {
            if (Companions == null) return;

            var filtered = Companions.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(c => 
                    c.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                    c.PathOrURL?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
            }

            // Apply type filter
            if (FilterType != "All")
            {
                filtered = FilterType switch
                {
                    "Global" => filtered.Where(c => !c.GameId.HasValue),
                    "Game-Specific" => filtered.Where(c => c.GameId.HasValue),
                    _ => filtered.Where(c => c.Type == FilterType)
                };
            }

            // Update display items based on filtered companions
            var gamesDict = new Dictionary<int, string>();
            Task.Run(async () =>
            {
                try
                {
                    var games = await _gameRepository.GetAllAsync();
                    foreach (var game in games)
                    {
                        gamesDict[game.GameId] = game.Name;
                    }
                }
                catch { }
            });

            var displayItems = filtered.Select(c => new CompanionDisplayItem
            {
                Companion = c,
                GameName = c.GameId.HasValue ? gamesDict.GetValueOrDefault(c.GameId.Value, "Unknown Game") : "Global",
                IsGlobal = !c.GameId.HasValue
            });

            CompanionDisplayItems.Clear();
            foreach (var item in displayItems)
            {
                CompanionDisplayItems.Add(item);
            }
        }

        [RelayCommand]
        private async Task AddCompanionAsync()
        {
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<AddEditCompanionViewModel>();
                await viewModel.LoadGamesAsync();

                var dialog = new AddEditCompanionDialog();
                dialog.DataContext = viewModel;

                if (dialog.ShowDialog() == true)
                {
                    var companion = viewModel.CreateCompanion();
                    await _companionRepository.AddAsync(companion);
                    
                    Companions.Add(companion);
                    await UpdateDisplayItemsAsync();
                    _notificationService.ShowSuccess("Companion added successfully!");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error adding companion: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task EditCompanionAsync(CompanionDisplayItem? displayItem)
        {
            if (displayItem?.Companion == null) return;

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<AddEditCompanionViewModel>();
                await viewModel.LoadGamesAsync();
                viewModel.LoadCompanion(displayItem.Companion);

                var dialog = new AddEditCompanionDialog();
                dialog.DataContext = viewModel;

                if (dialog.ShowDialog() == true)
                {
                    var editedCompanion = viewModel.CreateCompanion();
                    await _companionRepository.UpdateAsync(editedCompanion);
                    
                    // Update in the collection
                    var index = Companions.IndexOf(displayItem.Companion);
                    if (index >= 0)
                    {
                        Companions[index] = editedCompanion;
                    }
                    
                    await UpdateDisplayItemsAsync();
                    _notificationService.ShowSuccess("Companion updated successfully!");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error editing companion: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DeleteCompanionAsync(CompanionDisplayItem? displayItem)
        {
            if (displayItem?.Companion == null) return;

            var confirmed = await _notificationService.ShowWarningConfirmationAsync(
                "Delete Companion",
                $"Are you sure you want to delete '{displayItem.Companion.Name}'?",
                "Delete", "Cancel");

            if (confirmed)
            {
                try
                {
                    await _companionRepository.DeleteAsync(displayItem.Companion.CompanionId);
                    Companions.Remove(displayItem.Companion);
                    await UpdateDisplayItemsAsync();
                    _notificationService.ShowSuccess("Companion deleted successfully!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Error deleting companion: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadCompanionsAsync();
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            FilterType = "All";
        }
    }

    public class CompanionDisplayItem
    {
        public Companion Companion { get; set; } = null!;
        public string GameName { get; set; } = string.Empty;
        public bool IsGlobal { get; set; }
    }
}
