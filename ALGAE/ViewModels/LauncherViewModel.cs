using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ALGAE.Models;
using ALGAE.Services;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;

namespace ALGAE.ViewModels
{
    /// <summary>
    /// ViewModel for the Launcher view that monitors and controls running games
    /// </summary>
    public partial class LauncherViewModel : ObservableObject
    {
        private readonly IGameProcessMonitorService _processMonitor;
        private readonly INotificationService _notificationService;
        private readonly IGameLaunchService _gameLaunchService;
        private readonly IProfilesRepository _profilesRepository;
        private readonly Dispatcher _dispatcher;
        private Game? _currentGame;

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

        [ObservableProperty]
        private string _gameTitle = "Game Launcher";

        [ObservableProperty]
        private ObservableCollection<Profile> _profiles = new();

        [ObservableProperty]
        private Profile? _defaultProfile;

        [ObservableProperty]
        private bool _hasProfiles;

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

        public LauncherViewModel(IGameProcessMonitorService processMonitor, INotificationService notificationService, IGameLaunchService gameLaunchService, IProfilesRepository profilesRepository, Dispatcher dispatcher)
        {
            _processMonitor = processMonitor;
            _notificationService = notificationService;
            _gameLaunchService = gameLaunchService;
            _profilesRepository = profilesRepository;
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

        /// <summary>
        /// Sets the current game for this launcher instance
        /// </summary>
        /// <param name="game">The game to set as current</param>
        public async void SetCurrentGame(Game game)
        {
            _currentGame = game;
            GameTitle = game.Name;
            await LoadProfilesAsync();
        }

        [RelayCommand]
        private async Task LaunchGameAsync()
        {
            System.Diagnostics.Debug.WriteLine("[LauncherViewModel] LaunchGameAsync command triggered");
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Current game: {_currentGame?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Game launch service: {(_gameLaunchService != null ? "available" : "null")}");
                System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Default profile: {DefaultProfile?.ProfileName ?? "null"}");
                
                if (_currentGame != null && _gameLaunchService != null)
                {
                    // If we have a default profile, use it; otherwise use the basic launch
                    if (DefaultProfile != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Launching with profile: {DefaultProfile.ProfileName}");
                        await _gameLaunchService.LaunchGameAsync(_currentGame, DefaultProfile);
                        _notificationService.ShowInformation($"Launching {_currentGame.Name} with profile '{DefaultProfile.ProfileName}'...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Launching without profile");
                        await _gameLaunchService.LaunchGameAsync(_currentGame);
                        _notificationService.ShowInformation($"Launching {_currentGame.Name}...");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Cannot launch - missing game or service");
                    _notificationService.ShowWarning("No game selected or launch service unavailable");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Launch error: {ex.Message}");
                _notificationService.ShowError($"Error launching game: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LaunchGameWithProfileAsync(Profile profile)
        {
            try
            {
                if (_currentGame != null && _gameLaunchService != null && profile != null)
                {
                    await _gameLaunchService.LaunchGameAsync(_currentGame, profile);
                    _notificationService.ShowInformation($"Launching {_currentGame.Name} with profile '{profile.ProfileName}'...");
                }
                else
                {
                    _notificationService.ShowWarning("No game selected, launch service unavailable, or no profile selected");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error launching game with profile: {ex.Message}");
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

        /// <summary>
        /// Loads profiles for the current game
        /// </summary>
        private async Task LoadProfilesAsync()
        {
            try
            {
                if (_currentGame != null && _profilesRepository != null)
                {
                    var profiles = await _profilesRepository.GetAllByGameIdAsync(_currentGame.GameId);
                    
                    _dispatcher.Invoke(() =>
                    {
                        Profiles.Clear();
                        foreach (var profile in profiles)
                        {
                            Profiles.Add(profile);
                        }
                        
                        HasProfiles = Profiles.Count > 0;
                        
                        // Set the first profile as default (or could be based on a specific criteria)
                        DefaultProfile = Profiles.FirstOrDefault();
                    });
                }
                else
                {
                    _dispatcher.Invoke(() =>
                    {
                        Profiles.Clear();
                        DefaultProfile = null;
                        HasProfiles = false;
                    });
                }
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                {
                    Profiles.Clear();
                    DefaultProfile = null;
                    HasProfiles = false;
                });
                
                _notificationService.ShowError($"Error loading profiles: {ex.Message}");
            }
        }
    }
}
