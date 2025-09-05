using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;
using ALGAE.Services;

namespace ALGAE.ViewModels
{
    public partial class AddEditCompanionViewModel : ObservableObject
    {
        private readonly IGameRepository _gameRepository;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _type = "Executable";

        [ObservableProperty]
        private string _pathOrURL = string.Empty;

        [ObservableProperty]
        private string _launchHelper = string.Empty;

        [ObservableProperty]
        private string _browser = string.Empty;

        [ObservableProperty]
        private bool _openInNewWindow = false;

        [ObservableProperty]
        private Game? _selectedGame;

        [ObservableProperty]
        private bool _isGlobalCompanion = true;

        [ObservableProperty]
        private ObservableCollection<Game> _availableGames = new();

        [ObservableProperty]
        private bool _isLoading = false;

        public int? CompanionId { get; set; }

        public bool IsEditMode => CompanionId.HasValue;

        public string DialogTitle => IsEditMode ? "Edit Companion Application" : "Add Companion Application";

        public AddEditCompanionViewModel(
            IGameRepository gameRepository,
            INotificationService notificationService)
        {
            _gameRepository = gameRepository;
            _notificationService = notificationService;

            // Watch for changes in IsGlobalCompanion to clear selected game
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsGlobalCompanion) && IsGlobalCompanion)
            {
                SelectedGame = null;
            }
        }

        public async Task LoadGamesAsync()
        {
            try
            {
                IsLoading = true;
                var games = await _gameRepository.GetAllAsync();
                
                AvailableGames.Clear();
                foreach (var game in games.OrderBy(g => g.Name))
                {
                    AvailableGames.Add(game);
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

        public void LoadCompanion(Companion companion)
        {
            CompanionId = companion.CompanionId;
            Name = companion.Name ?? string.Empty;
            Type = companion.Type ?? "Executable";
            PathOrURL = companion.PathOrURL ?? string.Empty;
            LaunchHelper = companion.LaunchHelper ?? string.Empty;
            Browser = companion.Browser ?? string.Empty;
            OpenInNewWindow = companion.OpenInNewWindow;

            // Set game association
            if (companion.GameId.HasValue)
            {
                IsGlobalCompanion = false;
                SelectedGame = AvailableGames.FirstOrDefault(g => g.GameId == companion.GameId.Value);
            }
            else
            {
                IsGlobalCompanion = true;
                SelectedGame = null;
            }
        }

        public void SetPreselectedGame(Game? game)
        {
            if (game != null)
            {
                SelectedGame = game;
                IsGlobalCompanion = false;
            }
            else
            {
                IsGlobalCompanion = true;
                SelectedGame = null;
            }
        }

        public Companion CreateCompanion()
        {
            return new Companion
            {
                CompanionId = CompanionId ?? 0,
                GameId = IsGlobalCompanion ? null : SelectedGame?.GameId,
                Name = Name,
                Type = Type,
                PathOrURL = PathOrURL,
                LaunchHelper = string.IsNullOrWhiteSpace(LaunchHelper) ? null : LaunchHelper,
                Browser = string.IsNullOrWhiteSpace(Browser) ? null : Browser,
                OpenInNewWindow = OpenInNewWindow
            };
        }

        public bool ValidateCompanion()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                _notificationService.ShowError("Companion name is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Type))
            {
                _notificationService.ShowError("Companion type is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PathOrURL))
            {
                _notificationService.ShowError("Path or URL is required.");
                return false;
            }

            if (!IsGlobalCompanion && SelectedGame == null)
            {
                _notificationService.ShowError("Please select a game for this companion or set it as global.");
                return false;
            }

            return true;
        }

        [RelayCommand]
        private void BrowseForFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();

                if (Type == "Executable" || Type == "Script")
                {
                    openFileDialog.Title = "Select Application";
                    openFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
                }
                else if (Type == "Document")
                {
                    openFileDialog.Title = "Select Document";
                    openFileDialog.Filter = "All files (*.*)|*.*";
                }
                else
                {
                    return; // URLs don't need file browsing
                }

                openFileDialog.CheckFileExists = true;

                if (openFileDialog.ShowDialog() == true)
                {
                    PathOrURL = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error browsing for file: {ex.Message}");
            }
        }
    }
}
