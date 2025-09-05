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
        private readonly IGameLaunchService? _gameLaunchService;
        private readonly IProfilesRepository? _profilesRepository;
        private readonly ICompanionRepository? _companionRepository;
        private readonly ICompanionProfileRepository? _companionProfileRepository;
        private readonly ICompanionLaunchService? _companionLaunchService;
        private readonly Dispatcher _dispatcher;
        private Game? _currentGame;
        private Profile? _currentProfile;

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

        [ObservableProperty]
        private ObservableCollection<CompanionStatus> _companions = new();

        [ObservableProperty]
        private bool _hasCompanions;

        [ObservableProperty]
        private int _runningCompanionsCount;

        [ObservableProperty]
        private string _companionsStatus = "No companions";

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

        public LauncherViewModel(
            IGameProcessMonitorService processMonitor, 
            INotificationService notificationService, 
            IGameLaunchService gameLaunchService, 
            IProfilesRepository profilesRepository, 
            ICompanionRepository companionRepository,
            ICompanionProfileRepository companionProfileRepository,
            ICompanionLaunchService companionLaunchService,
            Dispatcher dispatcher)
        {
            _processMonitor = processMonitor;
            _notificationService = notificationService;
            _gameLaunchService = gameLaunchService;
            _profilesRepository = profilesRepository;
            _companionRepository = companionRepository;
            _companionProfileRepository = companionProfileRepository;
            _companionLaunchService = companionLaunchService;
            _dispatcher = dispatcher;

            // Subscribe to process monitor events
            _processMonitor.GameStarted += OnGameStarted;
            _processMonitor.GameStopped += OnGameStopped;
            _processMonitor.StatsUpdated += OnStatsUpdated;

            // Initialize collections
            Companions.CollectionChanged += (s, e) =>
            {
                HasCompanions = Companions.Count > 0;
                UpdateCompanionsStatus();
            };

            // Initialize with current state
            UpdateGameState();
            UpdateSessionStats();
            
            // Start companion monitoring timer
            StartCompanionMonitoring();
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
                    // Check if this is "Game only" mode
                    if (DefaultProfile != null && DefaultProfile.ProfileId == -1)
                    {
                        // "Game only" mode - launch with game's command line args only, no companions
                        System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Launching in 'Game only' mode");
                        await _gameLaunchService.LaunchGameAsync(_currentGame);
                        _notificationService.ShowInformation($"Launching {_currentGame.Name} (Game only)...");
                    }
                    else if (DefaultProfile != null)
                    {
                        // Real profile - launch with profile
                        System.Diagnostics.Debug.WriteLine($"[LauncherViewModel] Launching with profile: {DefaultProfile.ProfileName}");
                        await _gameLaunchService.LaunchGameAsync(_currentGame, DefaultProfile);
                        _notificationService.ShowInformation($"Launching {_currentGame.Name} with profile '{DefaultProfile.ProfileName}'...");
                    }
                    else
                    {
                        // Fallback - basic launch
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
                    // Check if this is "Game only" mode
                    if (profile.ProfileId == -1)
                    {
                        // "Game only" mode - launch with game's command line args only
                        await _gameLaunchService.LaunchGameAsync(_currentGame);
                        _notificationService.ShowInformation($"Launching {_currentGame.Name} (Game only)...");
                    }
                    else
                    {
                        // Real profile - launch with profile
                        await _gameLaunchService.LaunchGameAsync(_currentGame, profile);
                        _notificationService.ShowInformation($"Launching {_currentGame.Name} with profile '{profile.ProfileName}'...");
                    }
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
                        
                        // Add "Game only" as the first/default option
                        var gameOnlyProfile = new Profile
                        {
                            ProfileId = -1, // Special ID for "Game only" mode
                            GameId = _currentGame.GameId,
                            ProfileName = "Game only",
                            CommandLineArgs = _currentGame.GameArgs
                        };
                        Profiles.Add(gameOnlyProfile);
                        
                        // Add actual profiles
                        foreach (var profile in profiles)
                        {
                            Profiles.Add(profile);
                        }
                        
                        HasProfiles = Profiles.Count > 1; // Only true if there are actual profiles beyond "Game only"
                        
                        // Set "Game only" as default
                        DefaultProfile = gameOnlyProfile;
                    });
                }
                else
                {
                    _dispatcher.Invoke(() =>
                    {
                        Profiles.Clear();
                        
                        if (_currentGame != null)
                        {
                            // Even without profiles repository, create "Game only" option
                            var gameOnlyProfile = new Profile
                            {
                                ProfileId = -1,
                                GameId = _currentGame.GameId,
                                ProfileName = "Game only",
                                CommandLineArgs = _currentGame.GameArgs
                            };
                            Profiles.Add(gameOnlyProfile);
                            DefaultProfile = gameOnlyProfile;
                            HasProfiles = false; // No actual profiles available
                        }
                        else
                        {
                            DefaultProfile = null;
                            HasProfiles = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                {
                    Profiles.Clear();
                    
                    if (_currentGame != null)
                    {
                        // Create "Game only" option even on error
                        var gameOnlyProfile = new Profile
                        {
                            ProfileId = -1,
                            GameId = _currentGame.GameId,
                            ProfileName = "Game only",
                            CommandLineArgs = _currentGame.GameArgs
                        };
                        Profiles.Add(gameOnlyProfile);
                        DefaultProfile = gameOnlyProfile;
                    }
                    else
                    {
                        DefaultProfile = null;
                    }
                    HasProfiles = false;
                });
                
                _notificationService.ShowError($"Error loading profiles: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the current profile and loads its companions
        /// </summary>
        public async Task SetCurrentProfileAsync(Profile? profile)
        {
            _currentProfile = profile;
            
            // Only load companions if this is not "Game only" mode
            if (profile == null || profile.ProfileId == -1)
            {
                // "Game only" mode - clear companions
                _dispatcher.Invoke(() =>
                {
                    Companions.Clear();
                    HasCompanions = false;
                    UpdateCompanionsStatus();
                });
            }
            else
            {
                // Real profile - load its companions
                await LoadCompanionsAsync();
            }
        }

        /// <summary>
        /// Loads companions for the current profile
        /// </summary>
        private async Task LoadCompanionsAsync()
        {
            try
            {
                if (_currentProfile != null && _companionRepository != null && _companionProfileRepository != null)
                {
                    // Get companions associated with this profile
                    var companionProfiles = await _companionProfileRepository.GetByProfileIdAsync(_currentProfile.ProfileId);
                    var companions = new List<Companion>();
                    
                    foreach (var cp in companionProfiles.Where(cp => cp.IsEnabled))
                    {
                        var companion = await _companionRepository.GetByIdAsync(cp.CompanionId);
                        if (companion != null)
                        {
                            companions.Add(companion);
                        }
                    }

                    _dispatcher.Invoke(() =>
                    {
                        Companions.Clear();
                        foreach (var companion in companions)
                        {
                            var status = new CompanionStatus
                            {
                                Companion = companion,
                                IsRunning = false,
                                Status = "Stopped",
                                StatusIcon = "Stop",
                                StatusColor = Brushes.Gray
                            };
                            Companions.Add(status);
                        }
                        
                        HasCompanions = Companions.Count > 0;
                        UpdateCompanionsStatus();
                    });
                }
                else if (_currentGame != null && _companionRepository != null)
                {
                    // Load global and game-specific companions when no profile is set
                    var companions = await _companionRepository.GetForGameAsync(_currentGame.GameId);
                    
                    _dispatcher.Invoke(() =>
                    {
                        Companions.Clear();
                        foreach (var companion in companions)
                        {
                            var status = new CompanionStatus
                            {
                                Companion = companion,
                                IsRunning = false,
                                Status = "Stopped",
                                StatusIcon = "Stop",
                                StatusColor = Brushes.Gray
                            };
                            Companions.Add(status);
                        }
                        
                        HasCompanions = Companions.Count > 0;
                        UpdateCompanionsStatus();
                    });
                }
                else
                {
                    _dispatcher.Invoke(() =>
                    {
                        Companions.Clear();
                        HasCompanions = false;
                        UpdateCompanionsStatus();
                    });
                }
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                {
                    Companions.Clear();
                    HasCompanions = false;
                    UpdateCompanionsStatus();
                });
                
                _notificationService.ShowError($"Error loading companions: {ex.Message}");
            }
        }

        /// <summary>
        /// Start a specific companion
        /// </summary>
        [RelayCommand]
        private async Task StartCompanionAsync(CompanionStatus companionStatus)
        {
            if (companionStatus?.Companion == null || companionStatus.IsRunning) return;

            try
            {
                var companionProcess = await _companionLaunchService.LaunchCompanionAsync(companionStatus.Companion);
                if (companionProcess?.Process != null)
                {
                    companionStatus.Process = companionProcess.Process;
                    companionStatus.IsRunning = true;
                    companionStatus.StartTime = DateTime.Now;
                    companionStatus.Status = "Running";
                    companionStatus.StatusIcon = "CheckCircle";
                    companionStatus.StatusColor = Brushes.Green;
                    companionStatus.ErrorMessage = string.Empty;

                    _notificationService.ShowSuccess($"Started {companionStatus.Companion.Name}");
                    UpdateCompanionsStatus();
                }
            }
            catch (Exception ex)
            {
                companionStatus.ErrorMessage = ex.Message;
                companionStatus.Status = "Error";
                companionStatus.StatusIcon = "AlertCircle";
                companionStatus.StatusColor = Brushes.Red;
                
                _notificationService.ShowError($"Failed to start {companionStatus.Companion.Name}: {ex.Message}");
                UpdateCompanionsStatus();
            }
        }

        /// <summary>
        /// Stop a specific companion
        /// </summary>
        [RelayCommand]
        private async Task StopCompanionAsync(CompanionStatus companionStatus)
        {
            if (companionStatus?.Process == null || !companionStatus.IsRunning) return;

            try
            {
                // Create a CompanionProcess wrapper for the stop method
                var companionProcess = new CompanionProcess
                {
                    CompanionId = companionStatus.Companion.CompanionId,
                    CompanionName = companionStatus.Companion.Name ?? string.Empty,
                    ProcessId = companionStatus.Process.Id,
                    Process = companionStatus.Process
                };
                
                await _companionLaunchService.StopCompanionAsync(companionProcess);
                
                companionStatus.Process = null;
                companionStatus.IsRunning = false;
                companionStatus.StartTime = null;
                companionStatus.Status = "Stopped";
                companionStatus.StatusIcon = "Stop";
                companionStatus.StatusColor = Brushes.Gray;
                companionStatus.ErrorMessage = string.Empty;

                _notificationService.ShowInformation($"Stopped {companionStatus.Companion.Name}");
                UpdateCompanionsStatus();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to stop {companionStatus.Companion.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Start all companions
        /// </summary>
        [RelayCommand]
        private async Task StartAllCompanionsAsync()
        {
            var companionsToStart = Companions.Where(c => c.CanStart).ToList();
            var startedCount = 0;

            foreach (var companion in companionsToStart)
            {
                try
                {
                    await StartCompanionAsync(companion);
                    startedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error starting companion {companion.Companion.Name}: {ex.Message}");
                }
            }

            if (startedCount > 0)
            {
                _notificationService.ShowSuccess($"Started {startedCount} companion(s)");
            }
        }

        /// <summary>
        /// Stop all running companions
        /// </summary>
        [RelayCommand]
        private async Task StopAllCompanionsAsync()
        {
            var companionsToStop = Companions.Where(c => c.CanStop).ToList();
            var stoppedCount = 0;

            foreach (var companion in companionsToStop)
            {
                try
                {
                    await StopCompanionAsync(companion);
                    stoppedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping companion {companion.Companion.Name}: {ex.Message}");
                }
            }

            if (stoppedCount > 0)
            {
                _notificationService.ShowInformation($"Stopped {stoppedCount} companion(s)");
            }
        }

        /// <summary>
        /// Updates the companions status summary
        /// </summary>
        private void UpdateCompanionsStatus()
        {
            RunningCompanionsCount = Companions.Count(c => c.IsRunning);
            
            if (Companions.Count == 0)
            {
                CompanionsStatus = "No companions";
            }
            else if (RunningCompanionsCount == 0)
            {
                CompanionsStatus = $"{Companions.Count} companion(s) - None running";
            }
            else if (RunningCompanionsCount == Companions.Count)
            {
                CompanionsStatus = $"{Companions.Count} companion(s) - All running";
            }
            else
            {
                CompanionsStatus = $"{RunningCompanionsCount}/{Companions.Count} companions running";
            }
        }

        private DispatcherTimer? _companionMonitorTimer;

        /// <summary>
        /// Starts monitoring companion processes
        /// </summary>
        private void StartCompanionMonitoring()
        {
            _companionMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            
            _companionMonitorTimer.Tick += (s, e) => MonitorCompanions();
            _companionMonitorTimer.Start();
        }

        /// <summary>
        /// Monitors companion processes for status changes
        /// </summary>
        private void MonitorCompanions()
        {
            foreach (var companionStatus in Companions)
            {
                if (companionStatus.Process != null)
                {
                    try
                    {
                        // Check if process is still running
                        companionStatus.Process.Refresh();
                        if (companionStatus.Process.HasExited)
                        {
                            companionStatus.Process = null;
                            companionStatus.IsRunning = false;
                            companionStatus.StartTime = null;
                            companionStatus.Status = "Stopped";
                            companionStatus.StatusIcon = "Stop";
                            companionStatus.StatusColor = Brushes.Gray;
                        }
                    }
                    catch
                    {
                        // Process might be null or disposed
                        companionStatus.Process = null;
                        companionStatus.IsRunning = false;
                        companionStatus.StartTime = null;
                        companionStatus.Status = "Stopped";
                        companionStatus.StatusIcon = "Stop";
                        companionStatus.StatusColor = Brushes.Gray;
                    }
                }
            }
            
            UpdateCompanionsStatus();
        }
    }
}
