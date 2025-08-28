using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ALGAE.Services;

namespace ALGAE.ViewModels
{
    /// <summary>
    /// ViewModel for the game verification dialog
    /// </summary>
    public partial class GameVerificationViewModel : ObservableObject
    {
        private readonly List<DetectedGame> _originalDetectedGames;

        [ObservableProperty]
        private ObservableCollection<DetectedGameViewModel> _detectedGames = new();

        [ObservableProperty]
        private int _totalGamesFound;

        [ObservableProperty]
        private int _selectedCount;

        [ObservableProperty]
        private int _highConfidenceCount;

        [ObservableProperty]
        private bool _hasSelectedGames;

        [ObservableProperty]
        private string _addButtonText = "Add Selected Games";

        public event EventHandler<bool>? CloseRequested;

        public GameVerificationViewModel(List<DetectedGame> detectedGames)
        {
            _originalDetectedGames = detectedGames;
            InitializeData();
        }

        private void InitializeData()
        {
            DetectedGames.Clear();
            TotalGamesFound = _originalDetectedGames.Count;
            HighConfidenceCount = _originalDetectedGames.Count(g => g.ConfidenceScore >= 0.7f);

            foreach (var game in _originalDetectedGames.OrderByDescending(g => g.ConfidenceScore))
            {
                var gameViewModel = new DetectedGameViewModel(game);
                gameViewModel.PropertyChanged += OnGameSelectionChanged;
                
                // Auto-select high confidence games that aren't already in library
                gameViewModel.IsSelected = !game.AlreadyExists && game.ConfidenceScore >= 0.7f;
                
                DetectedGames.Add(gameViewModel);
            }

            UpdateSelectionCounts();
        }

        private void OnGameSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DetectedGameViewModel.IsSelected))
            {
                UpdateSelectionCounts();
            }
        }

        private void UpdateSelectionCounts()
        {
            SelectedCount = DetectedGames.Count(g => g.IsSelected);
            HasSelectedGames = SelectedCount > 0;
            AddButtonText = SelectedCount == 1 ? "Add 1 Game" : $"Add {SelectedCount} Games";
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var game in DetectedGames.Where(g => !g.AlreadyExists))
            {
                game.IsSelected = true;
            }
        }

        [RelayCommand]
        private void SelectNone()
        {
            foreach (var game in DetectedGames)
            {
                game.IsSelected = false;
            }
        }

        [RelayCommand]
        private void SelectHighConfidence()
        {
            foreach (var game in DetectedGames)
            {
                game.IsSelected = !game.AlreadyExists && game.ConfidenceScore >= 0.7f;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(this, false);
        }

        [RelayCommand]
        private void AddSelectedGames()
        {
            CloseRequested?.Invoke(this, true);
        }

        public List<DetectedGame> GetSelectedGames()
        {
            return DetectedGames
                .Where(g => g.IsSelected)
                .Select(g => g.OriginalGame)
                .ToList();
        }
    }

    /// <summary>
    /// ViewModel wrapper for DetectedGame to add UI-specific properties
    /// </summary>
    public partial class DetectedGameViewModel : ObservableObject
    {
        public DetectedGame OriginalGame { get; }

        [ObservableProperty]
        private bool _isSelected;

        public DetectedGameViewModel(DetectedGame detectedGame)
        {
            OriginalGame = detectedGame;
        }

        public string Name => OriginalGame.Name;
        public string? Publisher => OriginalGame.Publisher;
        public string? Version => OriginalGame.Version;
        public string InstallPath => OriginalGame.InstallPath;
        public List<string> DetectionReasons => OriginalGame.DetectionReasons;
        public float ConfidenceScore => OriginalGame.ConfidenceScore;
        public bool AlreadyExists => OriginalGame.AlreadyExists;

        public bool HasPublisher => !string.IsNullOrEmpty(Publisher);
        public bool HasVersion => !string.IsNullOrEmpty(Version);

        public string ConfidenceText => $"{(ConfidenceScore * 100):F0}%";
        public double ConfidencePercentage => ConfidenceScore * 100;

        public Brush ConfidenceColor
        {
            get
            {
                return ConfidenceScore switch
                {
                    >= 0.9f => Brushes.Green,
                    >= 0.7f => Brushes.Orange,
                    >= 0.5f => Brushes.DarkOrange,
                    _ => Brushes.Red
                };
            }
        }

        public string DetectionMethod
        {
            get
            {
                if (OriginalGame.MatchedSignature != null)
                {
                    return "Signature Match";
                }
                return "Heuristic Detection";
            }
        }
    }
}
