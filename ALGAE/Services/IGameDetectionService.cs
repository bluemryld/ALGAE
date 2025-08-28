using Algae.DAL.Models;

namespace ALGAE.Services
{
    /// <summary>
    /// Result of a game detection scan operation
    /// </summary>
    public class GameDetectionResult
    {
        public List<DetectedGame> DetectedGames { get; set; } = new();
        public List<string> ScannedPaths { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan ScanDuration { get; set; }
        public int TotalFilesScanned { get; set; }
        public bool IsSuccessful => Errors.Count == 0;
    }

    /// <summary>
    /// Represents a game detected during scanning
    /// </summary>
    public class DetectedGame
    {
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Publisher { get; set; }
        public string? Version { get; set; }
        public string InstallPath { get; set; } = string.Empty;
        public string? GameWorkingPath { get; set; }
        public string? ExecutableName { get; set; }
        public string? GameArgs { get; set; }
        public string? GameImage { get; set; }
        public string? ThemeName { get; set; }
        public GameSignature? MatchedSignature { get; set; }
        public float ConfidenceScore { get; set; }
        public List<string> DetectionReasons { get; set; } = new();
        public bool AlreadyExists { get; set; }
    }

    /// <summary>
    /// Service for detecting and identifying games automatically
    /// </summary>
    public interface IGameDetectionService
    {
        /// <summary>
        /// Scans all configured search paths for games
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Detection results</returns>
        Task<GameDetectionResult> ScanForGamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans specific directory for games
        /// </summary>
        /// <param name="directory">Directory path to scan</param>
        /// <param name="recursive">Whether to scan subdirectories</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Detection results</returns>
        Task<GameDetectionResult> ScanDirectoryAsync(string directory, bool recursive = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans multiple directories for games
        /// </summary>
        /// <param name="directories">List of directories to scan</param>
        /// <param name="recursive">Whether to scan subdirectories</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Detection results</returns>
        Task<GameDetectionResult> ScanDirectoriesAsync(IEnumerable<string> directories, bool recursive = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to identify a specific executable as a known game
        /// </summary>
        /// <param name="executablePath">Path to the executable</param>
        /// <returns>Detected game information if found, null otherwise</returns>
        Task<DetectedGame?> IdentifyGameAsync(string executablePath);

        /// <summary>
        /// Gets all common game directories on the system
        /// </summary>
        /// <returns>List of common game installation directories</returns>
        Task<List<string>> GetCommonGameDirectoriesAsync();

        /// <summary>
        /// Adds a new search path to the configuration
        /// </summary>
        /// <param name="path">Path to add</param>
        Task AddSearchPathAsync(string path);

        /// <summary>
        /// Removes a search path from the configuration
        /// </summary>
        /// <param name="path">Path to remove</param>
        Task RemoveSearchPathAsync(string path);

        /// <summary>
        /// Gets all configured search paths
        /// </summary>
        /// <returns>List of search paths</returns>
        Task<List<SearchPath>> GetSearchPathsAsync();

        /// <summary>
        /// Validates that detected games can be added to the library
        /// </summary>
        /// <param name="detectedGames">Games to validate</param>
        /// <returns>Validation results</returns>
        Task<List<DetectedGame>> ValidateDetectedGamesAsync(List<DetectedGame> detectedGames);

        /// <summary>
        /// Adds detected games to the game library
        /// </summary>
        /// <param name="detectedGames">Games to add</param>
        /// <returns>Number of games successfully added</returns>
        Task<int> AddDetectedGamesToLibraryAsync(List<DetectedGame> detectedGames);

        /// <summary>
        /// Event raised when game detection progress updates
        /// </summary>
        event EventHandler<GameDetectionProgressEventArgs>? ProgressUpdated;
    }

    /// <summary>
    /// Event arguments for game detection progress updates
    /// </summary>
    public class GameDetectionProgressEventArgs : EventArgs
    {
        public string CurrentPath { get; set; } = string.Empty;
        public int FilesScanned { get; set; }
        public int TotalFiles { get; set; }
        public int GamesFound { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
