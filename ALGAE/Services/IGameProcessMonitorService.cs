using ALGAE.Models;
using Algae.DAL.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ALGAE.Services
{
    /// <summary>
    /// Service for monitoring game processes and tracking game sessions
    /// </summary>
    public interface IGameProcessMonitorService
    {
        /// <summary>
        /// The currently running game, if any
        /// </summary>
        Game? RunningGame { get; }

        /// <summary>
        /// The process of the currently running game
        /// </summary>
        Process? GameProcess { get; }

        /// <summary>
        /// Whether a game is currently running
        /// </summary>
        bool HasRunningGame { get; }

        /// <summary>
        /// Current session time for the running game
        /// </summary>
        TimeSpan SessionTime { get; }

        /// <summary>
        /// CPU usage percentage of the running game
        /// </summary>
        double CpuUsage { get; }

        /// <summary>
        /// Memory usage of the running game
        /// </summary>
        string MemoryUsage { get; }

        /// <summary>
        /// Recent game sessions
        /// </summary>
        ObservableCollection<GameSession> RecentSessions { get; }

        /// <summary>
        /// Event raised when a game starts running
        /// </summary>
        event EventHandler<Game>? GameStarted;

        /// <summary>
        /// Event raised when a game stops running
        /// </summary>
        event EventHandler<Game>? GameStopped;

        /// <summary>
        /// Event raised when game statistics are updated
        /// </summary>
        event EventHandler? StatsUpdated;

        /// <summary>
        /// Start monitoring a game process
        /// </summary>
        /// <param name="game">The game being launched</param>
        /// <param name="process">The game process</param>
        void StartMonitoring(Game game, Process process);

        /// <summary>
        /// Stop monitoring the current game
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Force stop the currently running game
        /// </summary>
        Task StopGameAsync();

        /// <summary>
        /// Bring the game window to front
        /// </summary>
        void BringGameToFront();

        /// <summary>
        /// Minimize the game window
        /// </summary>
        void MinimizeGame();

        /// <summary>
        /// Refresh performance statistics
        /// </summary>
        void RefreshStats();
    }
}
