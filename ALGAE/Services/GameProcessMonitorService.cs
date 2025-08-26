using ALGAE.Models;
using Algae.DAL.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;

namespace ALGAE.Services
{
    /// <summary>
    /// Service for monitoring game processes and tracking game sessions
    /// </summary>
    public class GameProcessMonitorService : IGameProcessMonitorService, IDisposable
    {
        private readonly System.Timers.Timer _monitoringTimer;
        private readonly PerformanceCounter? _cpuCounter;
        private Process? _gameProcess;
        private Game? _runningGame;
        private DateTime _sessionStartTime;
        private GameSession? _currentSession;

        public Game? RunningGame => _runningGame;
        public Process? GameProcess => _gameProcess;
        public bool HasRunningGame => _gameProcess?.HasExited == false;
        public TimeSpan SessionTime => HasRunningGame ? DateTime.Now - _sessionStartTime : TimeSpan.Zero;
        public double CpuUsage { get; private set; }
        public string MemoryUsage { get; private set; } = "0 MB";
        public ObservableCollection<GameSession> RecentSessions { get; } = new();

        public event EventHandler<Game>? GameStarted;
        public event EventHandler<Game>? GameStopped;
        public event EventHandler? StatsUpdated;

        // Windows API imports for window manipulation
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        public GameProcessMonitorService()
        {
            // Initialize performance monitoring timer
            _monitoringTimer = new System.Timers.Timer(2000); // Update every 2 seconds
            _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
            _monitoringTimer.AutoReset = true;
            _monitoringTimer.Enabled = false; // Start disabled

            try
            {
                _cpuCounter = new PerformanceCounter("Process", "% Processor Time", "_Total");
            }
            catch
            {
                // If performance counters aren't available, we'll work without them
                _cpuCounter = null;
            }
        }

        public void StartMonitoring(Game game, Process process)
        {
            StopMonitoring(); // Stop any existing monitoring

            _runningGame = game;
            _gameProcess = process;
            _sessionStartTime = DateTime.Now;

            _currentSession = new GameSession
            {
                GameName = game.Name,
                StartTime = _sessionStartTime,
                ProcessId = process.Id
            };

            // Enable process exit event
            _gameProcess.EnableRaisingEvents = true;
            _gameProcess.Exited += OnGameProcessExited;

            _monitoringTimer.Start();
            GameStarted?.Invoke(this, game);
        }

        public void StopMonitoring()
        {
            if (_gameProcess != null)
            {
                _gameProcess.Exited -= OnGameProcessExited;
                _gameProcess = null;
            }

            if (_currentSession != null)
            {
                _currentSession.EndTime = DateTime.Now;
                RecentSessions.Insert(0, _currentSession);
                
                // Keep only the last 20 sessions
                while (RecentSessions.Count > 20)
                {
                    RecentSessions.RemoveAt(RecentSessions.Count - 1);
                }

                _currentSession = null;
            }

            _monitoringTimer.Stop();
            
            if (_runningGame != null)
            {
                GameStopped?.Invoke(this, _runningGame);
                _runningGame = null;
            }
        }

        public async Task StopGameAsync()
        {
            if (_gameProcess?.HasExited == false)
            {
                try
                {
                    // Try graceful shutdown first
                    _gameProcess.CloseMainWindow();
                    
                    // Wait a bit for graceful shutdown
                    await Task.Delay(3000);
                    
                    // Force kill if still running
                    if (!_gameProcess.HasExited)
                    {
                        _gameProcess.Kill();
                    }
                }
                catch (Exception)
                {
                    // Process might have already exited
                }
            }
        }

        public void BringGameToFront()
        {
            if (_gameProcess?.HasExited == false && _gameProcess.MainWindowHandle != IntPtr.Zero)
            {
                try
                {
                    ShowWindow(_gameProcess.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(_gameProcess.MainWindowHandle);
                }
                catch (Exception)
                {
                    // Window manipulation might fail
                }
            }
        }

        public void MinimizeGame()
        {
            if (_gameProcess?.HasExited == false && _gameProcess.MainWindowHandle != IntPtr.Zero)
            {
                try
                {
                    ShowWindow(_gameProcess.MainWindowHandle, SW_MINIMIZE);
                }
                catch (Exception)
                {
                    // Window manipulation might fail
                }
            }
        }

        public void RefreshStats()
        {
            UpdatePerformanceStats();
        }

        private void OnMonitoringTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_gameProcess?.HasExited != false)
            {
                StopMonitoring();
                return;
            }

            UpdatePerformanceStats();
            StatsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnGameProcessExited(object? sender, EventArgs e)
        {
            StopMonitoring();
        }

        private void UpdatePerformanceStats()
        {
            if (_gameProcess?.HasExited != false)
                return;

            try
            {
                // Update memory usage
                _gameProcess.Refresh();
                long memoryBytes = _gameProcess.WorkingSet64;
                MemoryUsage = FormatBytes(memoryBytes);

                // Update CPU usage (simplified approach)
                // Note: Real CPU usage per process is complex and would require more sophisticated monitoring
                CpuUsage = 0; // Placeholder for now
            }
            catch (Exception)
            {
                // Process might have exited or access denied
                MemoryUsage = "Unknown";
                CpuUsage = 0;
            }
        }

        private static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return $"{decimal.Divide(bytes, max):##.##} {order}";

                max /= scale;
            }
            return "0 Bytes";
        }

        public void Dispose()
        {
            StopMonitoring();
            _monitoringTimer?.Dispose();
            _cpuCounter?.Dispose();
        }
    }
}
