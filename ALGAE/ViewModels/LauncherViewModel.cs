using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ALGAE.Models;
using ALGAE.Services;
using Algae.DAL.Models;

namespace ALGAE.ViewModels
{
    /// <summary>
    /// ViewModel for the Launcher view that monitors and controls running games
    /// </summary>
    public partial class LauncherViewModel : ObservableObject
    {
        private readonly IGameProcessMonitorService _processMonitor;
        private readonly INotificationService _notificationService;
        private readonly Dispatcher _dispatcher;

        [ObservableProperty]
        private bool _hasRunningGame;

        [ObservableProperty]
        private Game? _runningGame;

        [ObservableProperty]
        private string _playTime = "00:00:00";

        [ObservableProperty]
        private string _sessionTime = "00:00:00";

        [ObservableProperty]
        private double _cpuUsage;

        [ObservableProperty]
        private string _memoryUsage = "0 MB";

        [ObservableProperty]
        private string _gameStatus = "Idle";

        [ObservableProperty]
        private string _statusIcon = "Sleep";

        [ObservableProperty]
        private Brush _statusColor = Brushes.Gray;

        [ObservableProperty]
        private int _processId;

        [ObservableProperty]
        private DateTime _startTime;

        [ObservableProperty]
        private bool _hasRecentSessions;

        public ObservableCollection<GameSession> RecentSessions => _processMonitor.RecentSessions;

        public LauncherViewModel(IGameProcessMonitorService processMonitor, INotificationService notificationService, Dispatcher dispatcher)
        {
            _processMonitor = processMonitor;
            _notificationService = notificationService;
            _dispatcher = dispatcher;

            // Subscribe to process monitor events
            _processMonitor.GameStarted += OnGameStarted;
            _processMonitor.GameStopped += OnGameStopped;
            _processMonitor.StatsUpdated += OnStatsUpdated;

            // Initialize with current state
            UpdateGameState();
            UpdateSessionStats();
        }

        [RelayCommand]
        private async Task StopGameAsync()
        {
            try
            {
                if (HasRunningGame)
                {
                    await _processMonitor.StopGameAsync();
                    _notificationService.ShowInformation($"Stopped {RunningGame?.Name}");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error stopping game: {ex.Message}");
            }
        }

        [RelayCommand]
        private void MinimizeGame()
        {
            try
            {
                if (HasRunningGame)
                {
                    _processMonitor.MinimizeGame();
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error minimizing game: {ex.Message}");
            }
        }

        [RelayCommand]
        private void BringToFront()
        {
            try
            {
                if (HasRunningGame)
                {
                    _processMonitor.BringGameToFront();
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error bringing game to front: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenGameFolder()
        {
            try
            {
                if (HasRunningGame && RunningGame != null)
                {
                    string? folderPath = Path.GetDirectoryName(RunningGame.InstallPath);
                    if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                    {
                        Process.Start("explorer.exe", folderPath);
                    }
                    else
                    {
                        _notificationService.ShowWarning("Game folder not found or invalid path");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error opening game folder: {ex.Message}");
            }
        }

        [RelayCommand]
        private void RefreshStats()
        {
            try
            {
                _processMonitor.RefreshStats();
                UpdateSessionStats();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error refreshing stats: {ex.Message}");
            }
        }

        private void OnGameStarted(object? sender, Game game)
        {
            _dispatcher.Invoke(() =>
            {
                UpdateGameState();
                UpdateSessionStats();
            });
        }

        private void OnGameStopped(object? sender, Game game)
        {
            _dispatcher.Invoke(() =>
            {
                UpdateGameState();
                UpdateSessionStats();
            });
        }

        private void OnStatsUpdated(object? sender, EventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                UpdateSessionStats();
            });
        }

        private void UpdateGameState()
        {
            HasRunningGame = _processMonitor.HasRunningGame;
            RunningGame = _processMonitor.RunningGame;
            
            if (HasRunningGame && _processMonitor.GameProcess != null)
            {
                ProcessId = _processMonitor.GameProcess.Id;
                StartTime = _processMonitor.GameProcess.StartTime;
                GameStatus = "Running";
                StatusIcon = "CheckCircle";
                StatusColor = Brushes.Green;
            }
            else
            {
                ProcessId = 0;
                StartTime = default;
                GameStatus = "Idle";
                StatusIcon = "Sleep";
                StatusColor = Brushes.Gray;
            }

            HasRecentSessions = RecentSessions.Count > 0;
        }

        private void UpdateSessionStats()
        {
            if (HasRunningGame)
            {
                var sessionTimeSpan = _processMonitor.SessionTime;
                SessionTime = $"{sessionTimeSpan:hh\\:mm\\:ss}";
                PlayTime = SessionTime; // For now, play time equals session time
                
                CpuUsage = _processMonitor.CpuUsage;
                MemoryUsage = _processMonitor.MemoryUsage;
            }
            else
            {
                SessionTime = "00:00:00";
                PlayTime = "00:00:00";
                CpuUsage = 0;
                MemoryUsage = "0 MB";
            }
        }
    }
}
