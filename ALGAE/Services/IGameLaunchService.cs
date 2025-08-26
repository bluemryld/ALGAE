using Algae.DAL.Models;
using System.Diagnostics;

namespace ALGAE.Services
{
    /// <summary>
    /// Service for launching games and handling launch operations
    /// </summary>
    public interface IGameLaunchService
    {
        /// <summary>
        /// Event raised when a game launch succeeds
        /// </summary>
        event EventHandler<GameLaunchEventArgs>? GameLaunched;

        /// <summary>
        /// Event raised when a game launch fails
        /// </summary>
        event EventHandler<GameLaunchFailedEventArgs>? GameLaunchFailed;

        /// <summary>
        /// Validates if a game can be launched (executable exists, paths are valid)
        /// </summary>
        /// <param name="game">The game to validate</param>
        /// <returns>Validation result with success flag and error message if applicable</returns>
        Task<GameValidationResult> ValidateGameAsync(Game game);

        /// <summary>
        /// Launches a game and starts monitoring the process
        /// </summary>
        /// <param name="game">The game to launch</param>
        /// <returns>Launch result with success flag, process info, and error message if applicable</returns>
        Task<GameLaunchResult> LaunchGameAsync(Game game);

        /// <summary>
        /// Gets the full executable path for a game
        /// </summary>
        /// <param name="game">The game to get the executable path for</param>
        /// <returns>The full path to the game executable</returns>
        string GetGameExecutablePath(Game game);

        /// <summary>
        /// Gets the working directory for a game
        /// </summary>
        /// <param name="game">The game to get the working directory for</param>
        /// <returns>The working directory path</returns>
        string GetGameWorkingDirectory(Game game);
    }

    /// <summary>
    /// Event arguments for successful game launch
    /// </summary>
    public class GameLaunchEventArgs : EventArgs
    {
        public Game Game { get; }
        public Process Process { get; }
        public DateTime LaunchTime { get; }

        public GameLaunchEventArgs(Game game, Process process, DateTime launchTime)
        {
            Game = game;
            Process = process;
            LaunchTime = launchTime;
        }
    }

    /// <summary>
    /// Event arguments for failed game launch
    /// </summary>
    public class GameLaunchFailedEventArgs : EventArgs
    {
        public Game Game { get; }
        public string ErrorMessage { get; }
        public Exception? Exception { get; }
        public DateTime AttemptTime { get; }

        public GameLaunchFailedEventArgs(Game game, string errorMessage, Exception? exception = null)
        {
            Game = game;
            ErrorMessage = errorMessage;
            Exception = exception;
            AttemptTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Result of game validation
    /// </summary>
    public class GameValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }
        public List<string> Warnings { get; }

        public GameValidationResult(bool isValid, string? errorMessage = null, List<string>? warnings = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            Warnings = warnings ?? new List<string>();
        }

        public static GameValidationResult Success(List<string>? warnings = null) =>
            new GameValidationResult(true, null, warnings);

        public static GameValidationResult Failure(string errorMessage) =>
            new GameValidationResult(false, errorMessage);
    }

    /// <summary>
    /// Result of game launch attempt
    /// </summary>
    public class GameLaunchResult
    {
        public bool Success { get; }
        public Process? Process { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }
        public DateTime LaunchTime { get; }

        public GameLaunchResult(bool success, Process? process = null, string? errorMessage = null, Exception? exception = null)
        {
            Success = success;
            Process = process;
            ErrorMessage = errorMessage;
            Exception = exception;
            LaunchTime = DateTime.Now;
        }

        public static GameLaunchResult Successful(Process process) =>
            new GameLaunchResult(true, process);

        public static GameLaunchResult Failed(string errorMessage, Exception? exception = null) =>
            new GameLaunchResult(false, null, errorMessage, exception);
    }
}
