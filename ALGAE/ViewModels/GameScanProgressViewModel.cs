using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ALGAE.Services;
using MaterialDesignThemes.Wpf;

namespace ALGAE.ViewModels
{
    /// <summary>
    /// ViewModel for the game scan progress dialog
    /// </summary>
    public partial class GameScanProgressViewModel : ObservableObject
    {
        private readonly IGameDetectionService _gameDetectionService;
        private readonly INotificationService _notificationService;
        private CancellationTokenSource? _cancellationTokenSource;
        private GameDetectionResult? _result;

        [ObservableProperty]
        private string _statusMessage = "Preparing to scan...";

        [ObservableProperty]
        private string _currentPath = "";

        [ObservableProperty]
        private int _filesScanned = 0;

        [ObservableProperty]
        private int _totalFiles = 0;

        [ObservableProperty]
        private int _gamesFound = 0;

        [ObservableProperty]
        private double _progressPercentage = 0;

        [ObservableProperty]
        private bool _isCompleted = false;

        [ObservableProperty]
        private bool _canCancel = true;

        [ObservableProperty]
        private ObservableCollection<ScanDirectoryItem> _scanDirectories = new();

        public GameDetectionResult? Result => _result;
        public bool WasCancelled => _cancellationTokenSource?.Token.IsCancellationRequested ?? false;

        public GameScanProgressViewModel(IGameDetectionService gameDetectionService, INotificationService notificationService)
        {
            _gameDetectionService = gameDetectionService;
            _notificationService = notificationService;
            _gameDetectionService.ProgressUpdated += OnProgressUpdated;
        }

        [RelayCommand]
        private void Cancel()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                StatusMessage = "Cancelling scan...";
                _cancellationTokenSource.Cancel();
                CanCancel = false;
            }
        }

        [RelayCommand]
        private void Close()
        {
            if (IsCompleted)
            {
                // Close the dialog - handled by the view
                OnCloseRequested();
            }
        }

        public async Task StartScanAsync(List<string> directories)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Initialize scan directories on UI thread
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    ScanDirectories.Clear();
                    foreach (var directory in directories)
                    {
                        ScanDirectories.Add(new ScanDirectoryItem
                        {
                            Path = directory,
                            IconKind = PackIconKind.Folder,
                            StatusColor = Brushes.Gray
                        });
                    }
                    StatusMessage = "Starting scan...";
                });
                
                // Start the scan on background thread
                _result = await Task.Run(() => 
                    _gameDetectionService.ScanDirectoriesAsync(directories, true, _cancellationTokenSource.Token));
                
                // Update completion status on UI thread
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        StatusMessage = "Scan cancelled";
                    }
                    else if (_result != null)
                    {
                        var newGames = _result.DetectedGames.Where(g => !g.AlreadyExists).Count();
                        StatusMessage = $"Scan completed! Found {_result.DetectedGames.Count} games ({newGames} new)";
                    }
                });
            }
            catch (OperationCanceledException)
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Scan cancelled";
                });
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Scan error: {ex.Message}";
                });
                _notificationService.ShowError($"Error during game scan: {ex.Message}");
            }
            finally
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsCompleted = true;
                    CanCancel = false;
                    ProgressPercentage = 100;
                    
                    // Mark all directories as completed
                    foreach (var dir in ScanDirectories)
                    {
                        dir.IconKind = PackIconKind.CheckCircle;
                        dir.StatusColor = Brushes.Green;
                    }
                });
                
                // Fire the scan completed event
                OnScanCompleted();
            }
        }

        private void OnProgressUpdated(object? sender, GameDetectionProgressEventArgs e)
        {
            // Update progress on UI thread using BeginInvoke to avoid blocking
            App.Current.Dispatcher.BeginInvoke(() =>
            {
                StatusMessage = e.Status;
                CurrentPath = e.CurrentPath;
                FilesScanned = e.FilesScanned;
                TotalFiles = e.TotalFiles;
                GamesFound = e.GamesFound;
                
                if (TotalFiles > 0)
                {
                    ProgressPercentage = (double)FilesScanned / TotalFiles * 100;
                }

                // Update current scanning directory
                var currentDir = ScanDirectories.FirstOrDefault(d => e.CurrentPath.StartsWith(d.Path, StringComparison.OrdinalIgnoreCase));
                if (currentDir != null)
                {
                    // Reset all to pending
                    foreach (var dir in ScanDirectories)
                    {
                        if (dir != currentDir)
                        {
                            dir.IconKind = PackIconKind.Folder;
                            dir.StatusColor = Brushes.Gray;
                        }
                    }
                    
                    // Mark current as active
                    currentDir.IconKind = PackIconKind.FolderSearch;
                    currentDir.StatusColor = Brushes.Orange;
                }
            });
        }

        public event EventHandler? CloseRequested;
        public event EventHandler? ScanCompleted;
        
        private void OnCloseRequested()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        
        private void OnScanCompleted()
        {
            ScanCompleted?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            // Update command states when properties change
            if (e.PropertyName == nameof(IsCompleted))
            {
                CloseCommand.NotifyCanExecuteChanged();
            }
            if (e.PropertyName == nameof(CanCancel))
            {
                CancelCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Represents a directory being scanned
    /// </summary>
    public partial class ScanDirectoryItem : ObservableObject
    {
        [ObservableProperty]
        private string _path = "";

        [ObservableProperty]
        private PackIconKind _iconKind = PackIconKind.Folder;

        [ObservableProperty]
        private Brush _statusColor = Brushes.Gray;
    }
}
