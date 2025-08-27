using Algae.DAL.Models;
using ALGAE.DAL.Repositories;
using System.Diagnostics;
using System.IO;

namespace ALGAE.Services
{
    /// <summary>
    /// Service for launching games and handling launch operations
    /// </summary>
    public class GameLaunchService : IGameLaunchService
    {
        private readonly IGameProcessMonitorService _processMonitorService;
        private readonly ILaunchHistoryRepository _launchHistoryRepository;
        private readonly Dictionary<int, int> _processToLaunchId = new();

        public event EventHandler<GameLaunchEventArgs>? GameLaunched;
        public event EventHandler<GameLaunchFailedEventArgs>? GameLaunchFailed;

        public GameLaunchService(IGameProcessMonitorService processMonitorService, ILaunchHistoryRepository launchHistoryRepository)
        {
            _processMonitorService = processMonitorService;
            _launchHistoryRepository = launchHistoryRepository;
            
            // Subscribe to process monitor events to update launch history
            _processMonitorService.GameStopped += OnGameStopped;
        }

        public async Task<GameValidationResult> ValidateGameAsync(Game game)
        {
            var warnings = new List<string>();

            // Check if game object has required data
            if (string.IsNullOrWhiteSpace(game.InstallPath))
            {
                return GameValidationResult.Failure("Game install path is not specified.");
            }

            if (string.IsNullOrWhiteSpace(game.ExecutableName))
            {
                return GameValidationResult.Failure("Game executable name is not specified.");
            }

            // Get full executable path
            var executablePath = GetGameExecutablePath(game);

            // Check if executable file exists
            if (!File.Exists(executablePath))
            {
                return GameValidationResult.Failure($"Game executable not found at: {executablePath}");
            }

            // Check if install path exists
            if (!Directory.Exists(game.InstallPath))
            {
                warnings.Add("Install path directory does not exist. This might cause issues.");
            }

            // Check working directory
            var workingDirectory = GetGameWorkingDirectory(game);
            if (!Directory.Exists(workingDirectory))
            {
                warnings.Add("Working directory does not exist. Using executable directory instead.");
            }

            // Check if executable has proper extension
            var extension = Path.GetExtension(executablePath).ToLower();
            if (extension != ".exe" && extension != ".bat" && extension != ".cmd")
            {
                warnings.Add($"Executable has unusual extension '{extension}'. This might not work on Windows.");
            }

            // Check if another game is already running
            if (_processMonitorService.HasRunningGame)
            {
                warnings.Add($"Another game '{_processMonitorService.RunningGame?.Name}' is currently running.");
            }

            return GameValidationResult.Success(warnings);
        }

        public async Task<GameLaunchResult> LaunchGameAsync(Game game)
        {
            try
            {
                // First validate the game
                var validation = await ValidateGameAsync(game);
                if (!validation.IsValid)
                {
                    var failureArgs = new GameLaunchFailedEventArgs(game, validation.ErrorMessage ?? "Game validation failed");
                    GameLaunchFailed?.Invoke(this, failureArgs);
                    return GameLaunchResult.Failed(validation.ErrorMessage ?? "Game validation failed");
                }

                // Get paths
                var executablePath = GetGameExecutablePath(game);
                var workingDirectory = GetGameWorkingDirectory(game);

                // Prepare process start info
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true // Use shell execute to handle file associations and admin privileges
                };

                // Add arguments if specified
                if (!string.IsNullOrWhiteSpace(game.GameArgs))
                {
                    startInfo.Arguments = game.GameArgs;
                }

                // For debugging - could be configurable
                startInfo.WindowStyle = ProcessWindowStyle.Normal;

                // Start the process
                var process = Process.Start(startInfo);
                
                if (process == null)
                {
                    var errorMessage = "Failed to start game process. The process returned null.";
                    await RecordFailedLaunch(game, errorMessage, executablePath, workingDirectory, game.GameArgs);
                    var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage);
                    GameLaunchFailed?.Invoke(this, failureArgs);
                    return GameLaunchResult.Failed(errorMessage);
                }

                // Wait a moment to see if the process starts successfully
                await Task.Delay(1000);

                // Check if process is still running (it might exit immediately if it's a launcher)
                if (process.HasExited)
                {
                    // Check exit code
                    if (process.ExitCode != 0)
                    {
                        var errorMessage = $"Game process exited immediately with code {process.ExitCode}. This might indicate an error or that the game uses a launcher.";
                        var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage);
                        GameLaunchFailed?.Invoke(this, failureArgs);
                        return GameLaunchResult.Failed(errorMessage);
                    }
                    else
                    {
                        // Exit code 0 might be normal for some launchers - treat as success but with a note
                        var successArgs = new GameLaunchEventArgs(game, process, DateTime.Now);
                        GameLaunched?.Invoke(this, successArgs);

                        return GameLaunchResult.Successful(process);
                    }
                }

                // Record successful launch attempt
                var launchId = await RecordSuccessfulLaunch(game, process, executablePath, workingDirectory, game.GameArgs);
                _processToLaunchId[process.Id] = launchId;

                // Start monitoring the process
                _processMonitorService.StartMonitoring(game, process);

                // Raise success event
                var launchArgs = new GameLaunchEventArgs(game, process, DateTime.Now);
                GameLaunched?.Invoke(this, launchArgs);

                return GameLaunchResult.Successful(process);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Common Windows errors (file not found, access denied, etc.)
                var errorMessage = $"Windows error launching game: {ex.Message}";
                if (ex.NativeErrorCode == 2)
                {
                    errorMessage = "Game executable file not found or cannot be accessed.";
                }
                else if (ex.NativeErrorCode == 5)
                {
                    errorMessage = "Access denied. The game might require administrator privileges.";
                }

                var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage, ex);
                GameLaunchFailed?.Invoke(this, failureArgs);
                return GameLaunchResult.Failed(errorMessage, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMessage = "Access denied. Check file permissions or run as administrator.";
                var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage, ex);
                GameLaunchFailed?.Invoke(this, failureArgs);
                return GameLaunchResult.Failed(errorMessage, ex);
            }
            catch (FileNotFoundException ex)
            {
                var errorMessage = $"Game file not found: {ex.FileName ?? "Unknown file"}";
                var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage, ex);
                GameLaunchFailed?.Invoke(this, failureArgs);
                return GameLaunchResult.Failed(errorMessage, ex);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unexpected error launching game: {ex.Message}";
                var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage, ex);
                GameLaunchFailed?.Invoke(this, failureArgs);
                return GameLaunchResult.Failed(errorMessage, ex);
            }
        }

        public async Task<GameLaunchResult> LaunchGameAsync(Game game, Profile profile)
        {
            try
            {
                // First validate the game
                var validation = await ValidateGameAsync(game);
                if (!validation.IsValid)
                {
                    var failureArgs = new GameLaunchFailedEventArgs(game, validation.ErrorMessage ?? "Game validation failed");
                    GameLaunchFailed?.Invoke(this, failureArgs);
                    return GameLaunchResult.Failed(validation.ErrorMessage ?? "Game validation failed");
                }

                // Get paths
                var executablePath = GetGameExecutablePath(game);
                var workingDirectory = GetGameWorkingDirectory(game);

                // Prepare process start info
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true // Use shell execute to handle file associations and admin privileges
                };

                // Use profile command line arguments first, then fallback to game arguments
                var arguments = !string.IsNullOrWhiteSpace(profile.CommandLineArgs) 
                    ? profile.CommandLineArgs 
                    : game.GameArgs;
                    
                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    startInfo.Arguments = arguments;
                }

                // For debugging - could be configurable
                startInfo.WindowStyle = ProcessWindowStyle.Normal;

                // Start the process
                var process = Process.Start(startInfo);
                
                if (process == null)
                {
                    var errorMessage = "Failed to start game process. The process returned null.";
                    await RecordFailedLaunch(game, errorMessage, executablePath, workingDirectory, arguments, profile.ProfileName);
                    var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage);
                    GameLaunchFailed?.Invoke(this, failureArgs);
                    return GameLaunchResult.Failed(errorMessage);
                }

                // Wait a moment to see if the process starts successfully
                await Task.Delay(1000);

                // Check if process is still running (it might exit immediately if it's a launcher)
                if (process.HasExited)
                {
                    // Check exit code
                    if (process.ExitCode != 0)
                    {
                        var errorMessage = $"Game process exited immediately with code {process.ExitCode}. This might indicate an error or that the game uses a launcher.";
                        var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage);
                        GameLaunchFailed?.Invoke(this, failureArgs);
                        return GameLaunchResult.Failed(errorMessage);
                    }
                    else
                    {
                        // Exit code 0 might be normal for some launchers - treat as success but with a note
                        var successArgs = new GameLaunchEventArgs(game, process, DateTime.Now);
                        GameLaunched?.Invoke(this, successArgs);

                        return GameLaunchResult.Successful(process);
                    }
                }

                // Record successful launch attempt
                var launchId = await RecordSuccessfulLaunch(game, process, executablePath, workingDirectory, arguments, profile.ProfileName);
                _processToLaunchId[process.Id] = launchId;

                // Start monitoring the process
                _processMonitorService.StartMonitoring(game, process);

                // Raise success event
                var launchArgs = new GameLaunchEventArgs(game, process, DateTime.Now);
                GameLaunched?.Invoke(this, launchArgs);

                return GameLaunchResult.Successful(process);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Common Windows errors (file not found, access denied, etc.)
                var errorMessage = $"Windows error launching game: {ex.Message}";
                if (ex.NativeErrorCode == 2)
                {
                    errorMessage = "Game executable file not found or cannot be accessed.";
                }
                else if (ex.NativeErrorCode == 5)
                {
                    errorMessage = "Access denied. The game might require administrator privileges.";
                }

                var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage, ex);
                GameLaunchFailed?.Invoke(this, failureArgs);
                return GameLaunchResult.Failed(errorMessage, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMessage = "Access denied. Check file permissions or run as administrator.";
                var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage, ex);
                GameLaunchFailed?.Invoke(this, failureArgs);
                return GameLaunchResult.Failed(errorMessage, ex);
            }
            catch (FileNotFoundException ex)
            {
                var errorMessage = $"Game file not found: {ex.FileName ?? "Unknown file"}";
                var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage, ex);
                GameLaunchFailed?.Invoke(this, failureArgs);
                return GameLaunchResult.Failed(errorMessage, ex);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Unexpected error launching game: {ex.Message}";
                var failureArgs = new GameLaunchFailedEventArgs(game, errorMessage, ex);
                GameLaunchFailed?.Invoke(this, failureArgs);
                return GameLaunchResult.Failed(errorMessage, ex);
            }
        }

        public string GetGameExecutablePath(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.InstallPath) || string.IsNullOrWhiteSpace(game.ExecutableName))
            {
                return string.Empty;
            }

            // Handle cases where ExecutableName might already be a full path
            if (Path.IsPathRooted(game.ExecutableName))
            {
                return game.ExecutableName;
            }

            // Combine install path with executable name
            return Path.Combine(game.InstallPath, game.ExecutableName);
        }

        public string GetGameWorkingDirectory(Game game)
        {
            // Use GameWorkingPath if specified, otherwise use InstallPath, otherwise use executable directory
            if (!string.IsNullOrWhiteSpace(game.GameWorkingPath) && Directory.Exists(game.GameWorkingPath))
            {
                return game.GameWorkingPath;
            }

            if (!string.IsNullOrWhiteSpace(game.InstallPath) && Directory.Exists(game.InstallPath))
            {
                return game.InstallPath;
            }

            // Last resort - use directory of executable
            var executablePath = GetGameExecutablePath(game);
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                var directory = Path.GetDirectoryName(executablePath);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    return directory;
                }
            }

            // Ultimate fallback
            return Environment.CurrentDirectory;
        }

        private async Task<int> RecordSuccessfulLaunch(Game game, Process process, string executablePath, string workingDirectory, string? arguments)
        {
            return await RecordSuccessfulLaunch(game, process, executablePath, workingDirectory, arguments, null);
        }

        private async Task<int> RecordSuccessfulLaunch(Game game, Process process, string executablePath, string workingDirectory, string? arguments, string? profileName)
        {
            var launchHistory = new LaunchHistory
            {
                GameId = game.GameId,
                GameName = game.Name,
                LaunchTime = DateTime.Now,
                Success = true,
                ProcessId = process.Id,
                ExecutablePath = executablePath,
                WorkingDirectory = workingDirectory,
                LaunchArguments = !string.IsNullOrWhiteSpace(profileName) ? $"{arguments} [Profile: {profileName}]" : arguments
            };

            return await _launchHistoryRepository.AddAsync(launchHistory);
        }

        private async Task RecordFailedLaunch(Game game, string errorMessage, string? executablePath = null, string? workingDirectory = null, string? arguments = null)
        {
            await RecordFailedLaunch(game, errorMessage, executablePath, workingDirectory, arguments, null);
        }

        private async Task RecordFailedLaunch(Game game, string errorMessage, string? executablePath = null, string? workingDirectory = null, string? arguments = null, string? profileName = null)
        {
            var launchHistory = new LaunchHistory
            {
                GameId = game.GameId,
                GameName = game.Name,
                LaunchTime = DateTime.Now,
                Success = false,
                ErrorMessage = !string.IsNullOrWhiteSpace(profileName) ? $"{errorMessage} [Profile: {profileName}]" : errorMessage,
                ExecutablePath = executablePath,
                WorkingDirectory = workingDirectory,
                LaunchArguments = arguments
            };

            await _launchHistoryRepository.AddAsync(launchHistory);
        }

        private async void OnGameStopped(object? sender, Game game)
        {
            try
            {
                var runningGames = await _launchHistoryRepository.GetRunningGamesAsync();
                var runningGame = runningGames.FirstOrDefault(g => g.GameId == game.GameId);
                
                if (runningGame != null)
                {
                    // Get performance stats from the monitor service
                    var peakMemory = _processMonitorService.MemoryUsage;
                    var avgCpu = _processMonitorService.CpuUsage;
                    
                    await _launchHistoryRepository.MarkCompletedAsync(
                        runningGame.LaunchId, 
                        DateTime.Now, 
                        peakMemory, 
                        avgCpu);
                        
                    // Remove from process tracking
                    if (runningGame.ProcessId.HasValue)
                    {
                        _processToLaunchId.Remove(runningGame.ProcessId.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating launch history on game stop: {ex.Message}");
            }
        }

    }
}
