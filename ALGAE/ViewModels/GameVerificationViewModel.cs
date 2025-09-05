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
        private int _selectedCompanionCount;

        [ObservableProperty]
        private int _highConfidenceCount;

        [ObservableProperty]
        private int _totalCompanionsFound;

        [ObservableProperty]
        private bool _hasSelectedGames;

        [ObservableProperty]
        private bool _hasCompanions;

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
            TotalCompanionsFound = _originalDetectedGames.Sum(g => g.DetectedCompanions.Count);
            HasCompanions = TotalCompanionsFound > 0;

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
            SelectedCompanionCount = DetectedGames.Sum(g => g.SelectedCompanionCount);
            HasSelectedGames = SelectedCount > 0 || SelectedCompanionCount > 0;
            
            var itemText = "";
            if (SelectedCount > 0 && SelectedCompanionCount > 0)
            {
                itemText = $"Add {SelectedCount} Games + {SelectedCompanionCount} Companions";
            }
            else if (SelectedCount > 0)
            {
                itemText = SelectedCount == 1 ? "Add 1 Game" : $"Add {SelectedCount} Games";
            }
            else if (SelectedCompanionCount > 0)
            {
                itemText = SelectedCompanionCount == 1 ? "Add 1 Companion" : $"Add {SelectedCompanionCount} Companions";
            }
            else
            {
                itemText = "Add Selected Items";
            }
            
            AddButtonText = itemText;
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
            CompanionViewModels = new ObservableCollection<DetectedCompanionViewModel>(
                detectedGame.DetectedCompanions.Select(c => new DetectedCompanionViewModel(c))
            );
            
            // Listen for companion selection changes
            foreach (var companion in CompanionViewModels)
            {
                companion.PropertyChanged += OnCompanionSelectionChanged;
            }
        }

        private void OnCompanionSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DetectedCompanionViewModel.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedCompanionCount));
            }
        }

        public int SelectedCompanionCount => CompanionViewModels.Count(c => c.IsSelected);

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

        public bool HasCompanions => OriginalGame.DetectedCompanions.Any();
        public int CompanionCount => OriginalGame.DetectedCompanions.Count;

        public ObservableCollection<DetectedCompanionViewModel> CompanionViewModels { get; }

        partial void OnIsSelectedChanged(bool value)
        {
            // When game selection changes, update companion selections
            if (HasCompanions)
            {
                foreach (var companion in CompanionViewModels)
                {
                    // Auto-select companions when game is selected (but allow individual control)
                    if (value && !companion.OriginalCompanion.AlreadyExists && companion.ConfidenceScore >= 0.7f)
                    {
                        companion.IsSelected = true;
                    }
                    else if (!value)
                    {
                        companion.IsSelected = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// ViewModel wrapper for DetectedCompanion to add UI-specific properties
    /// </summary>
    public partial class DetectedCompanionViewModel : ObservableObject
    {
        public DetectedCompanion OriginalCompanion { get; }

        [ObservableProperty]
        private bool _isSelected;

        public DetectedCompanionViewModel(DetectedCompanion detectedCompanion)
        {
            OriginalCompanion = detectedCompanion;
        }

        public string Name => OriginalCompanion.Name;
        public string? Description => OriginalCompanion.Description;
        public string? Publisher => OriginalCompanion.Publisher;
        public string? Version => OriginalCompanion.Version;
        public string ExecutablePath => OriginalCompanion.ExecutablePath;
        public List<string> DetectionReasons => OriginalCompanion.DetectionReasons;
        public float ConfidenceScore => OriginalCompanion.ConfidenceScore;
        public bool AlreadyExists => OriginalCompanion.AlreadyExists;
        public string Type => OriginalCompanion.Type;

        public bool HasDescription => !string.IsNullOrEmpty(Description);
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
                if (OriginalCompanion.MatchedSignature != null)
                {
                    return "Signature Match";
                }
                return "Heuristic Detection";
            }
        }
    }
}
