using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;

namespace ALGAE.Services
{
    /// <summary>
    /// Service for detecting and identifying games automatically
    /// </summary>
    public class GameDetectionService : IGameDetectionService
    {
        private readonly ISearchPathRepository _searchPathRepository;
        private readonly IGameSignatureRepository _gameSignatureRepository;
        private readonly ICompanionSignatureRepository _companionSignatureRepository;
        private readonly IGameRepository _gameRepository;
        private readonly ICompanionRepository _companionRepository;
        private readonly INotificationService _notificationService;

        // Common executable extensions for games
        private static readonly string[] GameExecutableExtensions = { ".exe", ".bat", ".cmd", ".sh" };

        // Common game directory names and patterns
        private static readonly string[] CommonGameDirectoryNames = 
        {
            "Games", "Program Files", "Program Files (x86)", "Steam", "steamapps", "common",
            "Epic Games", "Launcher", "GOG", "GOG Games", "Origin Games", "Ubisoft Game Launcher",
            "Battle.net", "Blizzard Entertainment", "Electronic Arts", "Rockstar Games",
            "Microsoft Games", "Windows Games"
        };

        // File patterns to exclude from scanning
        private static readonly string[] ExcludePatterns = 
        {
            "unins*.exe", "*uninstall*.exe", "*setup*.exe", "*install*.exe", 
            "*updater*.exe", "*patcher*.exe", "*launcher*.exe", "vcredist*.exe",
            "*redist*.exe", "directx*.exe", "dxsetup.exe", "*crash*.exe",
            "*error*.exe", "*report*.exe", "*debug*.exe", "*test*.exe"
        };

        public event EventHandler<GameDetectionProgressEventArgs>? ProgressUpdated;

        public GameDetectionService(
            ISearchPathRepository searchPathRepository,
            IGameSignatureRepository gameSignatureRepository,
            ICompanionSignatureRepository companionSignatureRepository,
            IGameRepository gameRepository,
            ICompanionRepository companionRepository,
            INotificationService notificationService)
        {
            _searchPathRepository = searchPathRepository;
            _gameSignatureRepository = gameSignatureRepository;
            _companionSignatureRepository = companionSignatureRepository;
            _gameRepository = gameRepository;
            _companionRepository = companionRepository;
            _notificationService = notificationService;
        }

        public async Task<GameDetectionResult> ScanForGamesAsync(CancellationToken cancellationToken = default)
        {
            var searchPaths = await _searchPathRepository.GetAllAsync();
            var directories = searchPaths.Select(sp => sp.Path).ToList();

            if (!directories.Any())
            {
                // If no search paths configured, add common game directories
                directories = await GetCommonGameDirectoriesAsync();
                foreach (var directory in directories.Where(Directory.Exists))
                {
                    await _searchPathRepository.AddAsync(directory);
                }
            }

            return await ScanDirectoriesAsync(directories, true, cancellationToken);
        }

        public async Task<GameDetectionResult> ScanDirectoryAsync(string directory, bool recursive = true, CancellationToken cancellationToken = default)
        {
            return await ScanDirectoriesAsync(new[] { directory }, recursive, cancellationToken);
        }

        public async Task<GameDetectionResult> ScanDirectoriesAsync(IEnumerable<string> directories, bool recursive = true, CancellationToken cancellationToken = default)
        {
            var result = new GameDetectionResult();
            var stopwatch = Stopwatch.StartNew();
            var detectedGames = new List<DetectedGame>();
            var existingGames = new HashSet<string>();

            try
            {
                // Get existing games to avoid duplicates
                var allExistingGames = await _gameRepository.GetAllAsync();
                foreach (var game in allExistingGames)
                {
                    existingGames.Add(game.InstallPath.ToLowerInvariant());
                }

                // Get all game signatures for matching
                var gameSignatures = (await _gameSignatureRepository.GetAllAsync()).ToList();

                var totalFiles = 0;
                var scannedFiles = 0;

                // First pass: count files for progress tracking
                foreach (var directory in directories.Where(Directory.Exists))
                {
                    try
                    {
                        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                        var files = Directory.GetFiles(directory, "*.*", searchOption)
                            .Where(IsExecutableFile)
                            .Where(f => !ShouldExcludeFile(f));
                        totalFiles += files.Count();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        result.Errors.Add($"Access denied to directory: {directory}");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error accessing directory {directory}: {ex.Message}");
                    }
                }

                // Second pass: scan for games
                foreach (var directory in directories.Where(Directory.Exists))
                {
                    result.ScannedPaths.Add(directory);

                    try
                    {
                        OnProgressUpdated($"Scanning {directory}...", scannedFiles, totalFiles, detectedGames.Count, directory);

                        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                        var executableFiles = Directory.GetFiles(directory, "*.*", searchOption)
                            .Where(IsExecutableFile)
                            .Where(f => !ShouldExcludeFile(f));

                        // Process files in chunks to improve cancellation responsiveness
                        var executableList = executableFiles.ToList();
                        const int chunkSize = 50; // Process 50 files at a time
                        
                        for (int i = 0; i < executableList.Count; i += chunkSize)
                        {
                            // Check for cancellation before each chunk
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            var chunk = executableList.Skip(i).Take(chunkSize);
                            
                            await Task.Run(() =>
                            {
                                foreach (var executablePath in chunk)
                                {
                                    // Check for cancellation more frequently
                                    cancellationToken.ThrowIfCancellationRequested();
                                    
                                    scannedFiles++;

                                    try
                                    {
                                        var detectedGame = IdentifyExecutableSync(executablePath, gameSignatures, existingGames);
                                        if (detectedGame != null)
                                        {
                                            detectedGames.Add(detectedGame);
                                            OnProgressUpdated($"Found: {detectedGame.Name}", scannedFiles, totalFiles, detectedGames.Count, executablePath);
                                        }

                                        // Update progress more frequently
                                        if (scannedFiles % 5 == 0)
                                        {
                                            OnProgressUpdated($"Scanning... ({scannedFiles}/{totalFiles})", scannedFiles, totalFiles, detectedGames.Count, executablePath);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error identifying executable {executablePath}: {ex.Message}");
                                    }
                                }
                            }, cancellationToken);
                            
                            // Small delay between chunks to allow UI updates
                            await Task.Delay(1, cancellationToken);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        result.Errors.Add($"Access denied to directory: {directory}");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error scanning directory {directory}: {ex.Message}");
                    }
                }

                result.DetectedGames = detectedGames;
                result.TotalFilesScanned = scannedFiles;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Unexpected error during scanning: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.ScanDuration = stopwatch.Elapsed;
                OnProgressUpdated("Scan completed", result.TotalFilesScanned, result.TotalFilesScanned, result.DetectedGames.Count, string.Empty);
            }

            return result;
        }

        public async Task<DetectedGame?> IdentifyGameAsync(string executablePath)
        {
            var gameSignatures = (await _gameSignatureRepository.GetAllAsync()).ToList();
            var existingGames = new HashSet<string>();

            var allExistingGames = await _gameRepository.GetAllAsync();
            foreach (var game in allExistingGames)
            {
                existingGames.Add(game.InstallPath.ToLowerInvariant());
            }

            return await IdentifyExecutableAsync(executablePath, gameSignatures, existingGames);
        }
        
        /// <summary>
        /// Synchronous version of IdentifyExecutableAsync for better performance in loops
        /// </summary>
        private DetectedGame? IdentifyExecutableSync(string executablePath, List<GameSignature> gameSignatures, HashSet<string> existingGames)
        {
            if (!File.Exists(executablePath) || !IsExecutableFile(executablePath) || ShouldExcludeFile(executablePath))
                return null;

            var fileName = Path.GetFileName(executablePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(executablePath);
            var directory = Path.GetDirectoryName(executablePath) ?? string.Empty;

            // Check if game already exists
            bool alreadyExists = existingGames.Contains(executablePath.ToLowerInvariant()) ||
                                existingGames.Contains(directory.ToLowerInvariant());

            FileVersionInfo? fileInfo = null;
            try
            {
                fileInfo = FileVersionInfo.GetVersionInfo(executablePath);
            }
            catch
            {
                // Ignore version info errors
            }

            var detectedGame = new DetectedGame
            {
                InstallPath = executablePath,
                GameWorkingPath = directory,
                ExecutableName = fileName,
                AlreadyExists = alreadyExists
            };

            // Try to match against known game signatures
            var bestMatch = FindBestGameSignatureMatchSync(fileName, fileInfo, gameSignatures);
            if (bestMatch != null)
            {
                MapGameSignatureToDetectedGame(detectedGame, bestMatch.Value.signature);
                detectedGame.MatchedSignature = bestMatch.Value.signature;
                detectedGame.ConfidenceScore = bestMatch.Value.confidence;
                detectedGame.DetectionReasons.Add($"Matched signature: {bestMatch.Value.signature.Name}");
            }
            else
            {
                // Try heuristic detection
                ApplyHeuristicDetectionSync(detectedGame, fileName, fileInfo, directory);
            }

            // Only return if we have enough information
            if (string.IsNullOrEmpty(detectedGame.Name) || detectedGame.ConfidenceScore < 0.3f)
                return null;

            // Note: Companions are not detected in sync method for performance
            // They will be detected in the async workflow instead

            return detectedGame;
        }

        private async Task<DetectedGame?> IdentifyExecutableAsync(string executablePath, List<GameSignature> gameSignatures, HashSet<string> existingGames)
        {
            if (!File.Exists(executablePath) || !IsExecutableFile(executablePath) || ShouldExcludeFile(executablePath))
                return null;

            var fileName = Path.GetFileName(executablePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(executablePath);
            var directory = Path.GetDirectoryName(executablePath) ?? string.Empty;

            // Check if game already exists
            bool alreadyExists = existingGames.Contains(executablePath.ToLowerInvariant()) ||
                                existingGames.Contains(directory.ToLowerInvariant());

            FileVersionInfo? fileInfo = null;
            try
            {
                fileInfo = FileVersionInfo.GetVersionInfo(executablePath);
            }
            catch
            {
                // Ignore version info errors
            }

            var detectedGame = new DetectedGame
            {
                InstallPath = executablePath,
                GameWorkingPath = directory,
                ExecutableName = fileName,
                AlreadyExists = alreadyExists
            };

            // Try to match against known game signatures
            var bestMatch = await FindBestGameSignatureMatch(fileName, fileInfo, gameSignatures);
            if (bestMatch != null)
            {
                MapGameSignatureToDetectedGame(detectedGame, bestMatch.Value.signature);
                detectedGame.MatchedSignature = bestMatch.Value.signature;
                detectedGame.ConfidenceScore = bestMatch.Value.confidence;
                detectedGame.DetectionReasons.Add($"Matched signature: {bestMatch.Value.signature.Name}");
            }
            else
            {
                // Try heuristic detection
                await ApplyHeuristicDetection(detectedGame, fileName, fileInfo, directory);
            }

            // Only return if we have enough information
            if (string.IsNullOrEmpty(detectedGame.Name) || detectedGame.ConfidenceScore < 0.3f)
                return null;

            // Detect companions for this game
            detectedGame.DetectedCompanions = await DetectCompanionsForGameAsync(detectedGame, directory);

            return detectedGame;
        }

        private async Task<(GameSignature? signature, float confidence)?> FindBestGameSignatureMatch(
            string fileName, FileVersionInfo? fileInfo, List<GameSignature> gameSignatures)
        {
            var bestMatch = (signature: (GameSignature?)null, confidence: 0f);

            foreach (var signature in gameSignatures)
            {
                float confidence = CalculateSignatureMatchConfidence(signature, fileName, fileInfo);
                
                if (confidence > bestMatch.confidence && confidence > 0.5f)
                {
                    bestMatch = (signature, confidence);
                }
            }

            return bestMatch.signature != null ? bestMatch : null;
        }
        
        /// <summary>
        /// Synchronous version of FindBestGameSignatureMatch for better performance in loops
        /// </summary>
        private (GameSignature signature, float confidence)? FindBestGameSignatureMatchSync(
            string fileName, FileVersionInfo? fileInfo, List<GameSignature> gameSignatures)
        {
            var bestMatch = (signature: (GameSignature?)null, confidence: 0f);

            foreach (var signature in gameSignatures)
            {
                float confidence = CalculateSignatureMatchConfidence(signature, fileName, fileInfo);
                
                if (confidence > bestMatch.confidence && confidence > 0.5f)
                {
                    bestMatch = (signature, confidence);
                }
            }

            return bestMatch.signature != null ? bestMatch : null;
        }

        private float CalculateSignatureMatchConfidence(GameSignature signature, string fileName, FileVersionInfo? fileInfo)
        {
            float confidence = 0f;
            int totalChecks = 0;

            // Check executable name match (highest weight - most reliable)
            if (signature.MatchName && !string.IsNullOrEmpty(signature.ExecutableName))
            {
                totalChecks++;
                if (string.Equals(signature.ExecutableName, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    confidence += 0.7f; // Increased from 0.6f since version is removed
                }
                else if (fileName.Contains(signature.ExecutableName, StringComparison.OrdinalIgnoreCase))
                {
                    confidence += 0.35f; // Increased from 0.3f
                }
            }

            // Check publisher match (medium weight - fairly reliable)
            if (signature.MatchPublisher && !string.IsNullOrEmpty(signature.Publisher) && fileInfo != null)
            {
                totalChecks++;
                if (!string.IsNullOrEmpty(fileInfo.CompanyName) &&
                    fileInfo.CompanyName.Contains(signature.Publisher, StringComparison.OrdinalIgnoreCase))
                {
                    confidence += 0.3f; // Increased from 0.25f to compensate for removed version
                }
            }

            // NOTE: Version matching removed - versions change too frequently with updates/patches
            // This prevents false negatives when games are updated to newer versions

            // Check product name match (medium weight - good secondary confirmation)
            if (!string.IsNullOrEmpty(signature.MetaName) && fileInfo != null)
            {
                if (!string.IsNullOrEmpty(fileInfo.ProductName) &&
                    fileInfo.ProductName.Contains(signature.MetaName, StringComparison.OrdinalIgnoreCase))
                {
                    confidence += 0.25f; // Increased from 0.2f
                }
            }

            return totalChecks > 0 ? confidence : 0f;
        }

        private void MapGameSignatureToDetectedGame(DetectedGame detectedGame, GameSignature signature)
        {
            detectedGame.Name = signature.Name;
            detectedGame.ShortName = signature.ShortName;
            detectedGame.Description = signature.Description;
            detectedGame.Publisher = signature.Publisher;
            detectedGame.Version = signature.Version;
            detectedGame.GameArgs = signature.GameArgs;
            detectedGame.GameImage = signature.GameImage;
            detectedGame.ThemeName = signature.ThemeName;
        }

        private async Task ApplyHeuristicDetection(DetectedGame detectedGame, string fileName, FileVersionInfo? fileInfo, string directory)
        {
            var reasons = new List<string>();
            float confidence = 0f;

            // Extract name from filename
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            detectedGame.Name = CleanGameName(baseName);
            detectedGame.ShortName = GenerateShortName(detectedGame.Name);
            reasons.Add("Name extracted from filename");
            confidence += 0.3f;

            // Extract information from file version info
            if (fileInfo != null)
            {
                if (!string.IsNullOrEmpty(fileInfo.ProductName))
                {
                    detectedGame.Name = CleanGameName(fileInfo.ProductName);
                    reasons.Add("Name from file version info");
                    confidence += 0.4f;
                }

                if (!string.IsNullOrEmpty(fileInfo.CompanyName))
                {
                    detectedGame.Publisher = fileInfo.CompanyName;
                    reasons.Add("Publisher from file version info");
                    confidence += 0.1f;
                }

                if (!string.IsNullOrEmpty(fileInfo.ProductVersion))
                {
                    detectedGame.Version = fileInfo.ProductVersion;
                    reasons.Add("Version from file version info");
                    confidence += 0.1f;
                }

                if (!string.IsNullOrEmpty(fileInfo.FileDescription))
                {
                    detectedGame.Description = fileInfo.FileDescription;
                    reasons.Add("Description from file version info");
                    confidence += 0.1f;
                }
            }

            // Analyze directory structure for additional clues
            var directoryName = Path.GetFileName(directory);
            if (!string.IsNullOrEmpty(directoryName) && IsLikelyGameDirectory(directoryName))
            {
                confidence += 0.2f;
                reasons.Add("Located in game-like directory");
            }

            detectedGame.DetectionReasons.AddRange(reasons);
            detectedGame.ConfidenceScore = Math.Min(confidence, 1.0f);
        }
        
        /// <summary>
        /// Synchronous version of ApplyHeuristicDetection for better performance in loops
        /// </summary>
        private void ApplyHeuristicDetectionSync(DetectedGame detectedGame, string fileName, FileVersionInfo? fileInfo, string directory)
        {
            var reasons = new List<string>();
            float confidence = 0f;

            // Extract name from filename
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            detectedGame.Name = CleanGameName(baseName);
            detectedGame.ShortName = GenerateShortName(detectedGame.Name);
            reasons.Add("Name extracted from filename");
            confidence += 0.3f;

            // Extract information from file version info
            if (fileInfo != null)
            {
                if (!string.IsNullOrEmpty(fileInfo.ProductName))
                {
                    detectedGame.Name = CleanGameName(fileInfo.ProductName);
                    reasons.Add("Name from file version info");
                    confidence += 0.4f;
                }

                if (!string.IsNullOrEmpty(fileInfo.CompanyName))
                {
                    detectedGame.Publisher = fileInfo.CompanyName;
                    reasons.Add("Publisher from file version info");
                    confidence += 0.1f;
                }

                if (!string.IsNullOrEmpty(fileInfo.ProductVersion))
                {
                    detectedGame.Version = fileInfo.ProductVersion;
                    reasons.Add("Version from file version info");
                    confidence += 0.1f;
                }

                if (!string.IsNullOrEmpty(fileInfo.FileDescription))
                {
                    detectedGame.Description = fileInfo.FileDescription;
                    reasons.Add("Description from file version info");
                    confidence += 0.1f;
                }
            }

            // Analyze directory structure for additional clues
            var directoryName = Path.GetFileName(directory);
            if (!string.IsNullOrEmpty(directoryName) && IsLikelyGameDirectory(directoryName))
            {
                confidence += 0.2f;
                reasons.Add("Located in game-like directory");
            }

            detectedGame.DetectionReasons.AddRange(reasons);
            detectedGame.ConfidenceScore = Math.Min(confidence, 1.0f);
        }

        private static string CleanGameName(string name)
        {
            // Remove common suffixes and prefixes
            var cleanName = name;
            var patterns = new[]
            {
                @"\s*\(.*\)$",           // Remove parentheses content at end
                @"\s*\[.*\]$",           // Remove bracket content at end
                @"\s*-\s*\d+(\.\d+)*$",  // Remove version numbers
                @"^Game\s+",             // Remove "Game " prefix
                @"\s+Game$",             // Remove " Game" suffix
            };

            foreach (var pattern in patterns)
            {
                cleanName = Regex.Replace(cleanName, pattern, "", RegexOptions.IgnoreCase).Trim();
            }

            return string.IsNullOrWhiteSpace(cleanName) ? name : cleanName;
        }

        private static string GenerateShortName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Take first letters of each word, max 10 chars
            var words = name.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1)
            {
                return words[0].Length <= 10 ? words[0] : words[0].Substring(0, 10);
            }

            var shortName = string.Join("", words.Select(w => w[0]));
            return shortName.Length <= 10 ? shortName : shortName.Substring(0, 10);
        }

        private static bool IsLikelyGameDirectory(string directoryName)
        {
            var lowerName = directoryName.ToLowerInvariant();
            
            // Check for common game directory patterns
            var gamePatterns = new[]
            {
                "game", "games", "play", "steam", "gog", "epic", "origin",
                "launcher", "client", "studio", "entertainment", "software"
            };

            return gamePatterns.Any(pattern => lowerName.Contains(pattern));
        }

        private static bool IsExecutableFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return GameExecutableExtensions.Contains(extension);
        }

        private static bool ShouldExcludeFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLowerInvariant();
            
            return ExcludePatterns.Any(pattern => 
            {
                var regex = new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$", RegexOptions.IgnoreCase);
                return regex.IsMatch(fileName);
            });
        }

        public async Task<List<string>> GetCommonGameDirectoriesAsync()
        {
            var directories = new List<string>();

            try
            {
                // Program Files directories
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                if (!string.IsNullOrEmpty(programFiles))
                    directories.Add(programFiles);
                
                if (!string.IsNullOrEmpty(programFilesX86))
                    directories.Add(programFilesX86);

                // Steam directories
                var steamPaths = new[]
                {
                    Path.Combine(programFiles, "Steam", "steamapps", "common"),
                    Path.Combine(programFilesX86, "Steam", "steamapps", "common"),
                    @"C:\Steam\steamapps\common",
                    @"D:\Steam\steamapps\common",
                    @"E:\Steam\steamapps\common"
                };
                directories.AddRange(steamPaths);

                // Epic Games
                directories.Add(Path.Combine(programFiles, "Epic Games"));

                // GOG
                directories.Add(Path.Combine(programFilesX86, "GOG Galaxy", "Games"));
                directories.Add(@"C:\GOG Games");

                // Origin
                directories.Add(Path.Combine(programFilesX86, "Origin Games"));

                // Battle.net
                directories.Add(Path.Combine(programFiles, "Battle.net"));
                directories.Add(Path.Combine(programFilesX86, "Battle.net"));

                // Microsoft Store games
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localAppData))
                {
                    directories.Add(Path.Combine(localAppData, "Microsoft", "WindowsApps"));
                }

                // Custom drives
                for (char drive = 'C'; drive <= 'Z'; drive++)
                {
                    var drivePath = $@"{drive}:\Games";
                    directories.Add(drivePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting common game directories: {ex.Message}");
            }

            return directories.Where(Directory.Exists).ToList();
        }

        public async Task AddSearchPathAsync(string path)
        {
            if (!await _searchPathRepository.PathExistsAsync(path))
            {
                await _searchPathRepository.AddAsync(path);
            }
        }

        public async Task RemoveSearchPathAsync(string path)
        {
            await _searchPathRepository.DeleteByPathAsync(path);
        }

        public async Task<List<SearchPath>> GetSearchPathsAsync()
        {
            var paths = await _searchPathRepository.GetAllAsync();
            return paths.ToList();
        }

        public async Task<List<DetectedGame>> ValidateDetectedGamesAsync(List<DetectedGame> detectedGames)
        {
            var validGames = new List<DetectedGame>();

            foreach (var game in detectedGames)
            {
                // Validate that the executable still exists and is accessible
                if (File.Exists(game.InstallPath))
                {
                    try
                    {
                        // Try to get file info to ensure it's accessible
                        var fileInfo = new FileInfo(game.InstallPath);
                        if (fileInfo.Length > 0) // Basic validation
                        {
                            validGames.Add(game);
                        }
                    }
                    catch
                    {
                        // Skip games we can't access
                    }
                }
            }

            return validGames;
        }

        public async Task<int> AddDetectedGamesToLibraryAsync(List<DetectedGame> detectedGames)
        {
            int addedCount = 0;

            foreach (var detectedGame in detectedGames.Where(g => !g.AlreadyExists))
            {
                try
                {
                    var game = new Game
                    {
                        Name = detectedGame.Name,
                        ShortName = detectedGame.ShortName,
                        Description = detectedGame.Description,
                        Publisher = detectedGame.Publisher,
                        Version = detectedGame.Version,
                        InstallPath = detectedGame.InstallPath,
                        GameWorkingPath = detectedGame.GameWorkingPath,
                        ExecutableName = detectedGame.ExecutableName,
                        GameArgs = detectedGame.GameArgs,
                        GameImage = detectedGame.GameImage,
                        ThemeName = detectedGame.ThemeName
                    };

                    await _gameRepository.AddAsync(game);
                    addedCount++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error adding detected game {detectedGame.Name}: {ex.Message}");
                }
            }

            return addedCount;
        }

        private void OnProgressUpdated(string status, int filesScanned, int totalFiles, int gamesFound, string currentPath)
        {
            ProgressUpdated?.Invoke(this, new GameDetectionProgressEventArgs
            {
                Status = status,
                FilesScanned = filesScanned,
                TotalFiles = totalFiles,
                GamesFound = gamesFound,
                CurrentPath = currentPath
            });
        }

        /// <summary>
        /// Detects companion applications for a detected game
        /// </summary>
        private async Task<List<DetectedCompanion>> DetectCompanionsForGameAsync(DetectedGame detectedGame, string gameDirectory)
        {
            var detectedCompanions = new List<DetectedCompanion>();
            
            if (detectedGame.MatchedSignature == null)
                return detectedCompanions;

            try
            {
                // Get companion signatures for this game
                var companionSignatures = await _companionSignatureRepository.GetByGameSignatureIdAsync(detectedGame.MatchedSignature.GameSignatureId);
                
                if (!companionSignatures.Any())
                    return detectedCompanions;

                // Get existing companions to check for duplicates
                var existingCompanions = await _companionRepository.GetAllAsync();
                var existingPaths = new HashSet<string>(existingCompanions.Select(c => c.PathOrURL.ToLowerInvariant()));

                // Search for companions in the game directory and common locations
                var searchPaths = new List<string> { gameDirectory };
                
                // Add parent directory to search (common for companion apps)
                var parentDir = Directory.GetParent(gameDirectory)?.FullName;
                if (!string.IsNullOrEmpty(parentDir))
                    searchPaths.Add(parentDir);

                foreach (var searchPath in searchPaths.Where(Directory.Exists))
                {
                    var executables = Directory.GetFiles(searchPath, "*.exe", SearchOption.TopDirectoryOnly)
                                               .Where(f => !ShouldExcludeFile(f));

                    foreach (var executablePath in executables)
                    {
                        foreach (var signature in companionSignatures)
                        {
                            var detectedCompanion = await TryMatchCompanionSignature(executablePath, signature, existingPaths);
                            if (detectedCompanion != null)
                            {
                                detectedCompanions.Add(detectedCompanion);
                                break; // Don't match the same executable to multiple signatures
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error detecting companions for game {detectedGame.Name}: {ex.Message}");
            }

            return detectedCompanions;
        }

        /// <summary>
        /// Attempts to match an executable to a companion signature
        /// </summary>
        private async Task<DetectedCompanion?> TryMatchCompanionSignature(string executablePath, CompanionSignature signature, HashSet<string> existingPaths)
        {
            var fileName = Path.GetFileName(executablePath);
            
            // Check executable name match
            if (!string.Equals(fileName, signature.ExecutableName, StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(executablePath);
                float confidence = CalculateCompanionSignatureMatchConfidence(signature, fileName, fileInfo);
                
                if (confidence < 0.5f)
                    return null;

                var detectedCompanion = new DetectedCompanion
                {
                    Name = signature.Name,
                    Description = signature.Description,
                    Publisher = signature.Publisher,
                    Version = signature.Version,
                    ExecutablePath = executablePath,
                    CompanionArgs = signature.CompanionArgs,
                    MatchedSignature = signature,
                    ConfidenceScore = confidence,
                    AlreadyExists = existingPaths.Contains(executablePath.ToLowerInvariant()),
                    Type = "Application"
                };

                // Add detection reasons
                detectedCompanion.DetectionReasons.Add($"Matched companion signature: {signature.Name}");
                detectedCompanion.DetectionReasons.Add($"Executable name: {fileName}");
                
                if (fileInfo.FileDescription != null && signature.MatchName && 
                    fileInfo.FileDescription.Contains(signature.MetaName ?? signature.Name, StringComparison.OrdinalIgnoreCase))
                {
                    detectedCompanion.DetectionReasons.Add($"File description match: {fileInfo.FileDescription}");
                    confidence += 0.2f;
                }

                if (fileInfo.CompanyName != null && signature.MatchPublisher &&
                    fileInfo.CompanyName.Contains(signature.Publisher ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    detectedCompanion.DetectionReasons.Add($"Publisher match: {fileInfo.CompanyName}");
                    confidence += 0.1f;
                }

                detectedCompanion.ConfidenceScore = Math.Min(confidence, 1.0f);
                
                return detectedCompanion;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error analyzing companion executable {executablePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculates confidence score for companion signature match
        /// </summary>
        private float CalculateCompanionSignatureMatchConfidence(CompanionSignature signature, string fileName, FileVersionInfo? fileInfo)
        {
            float confidence = 0f;

            // Exact executable name match
            if (string.Equals(fileName, signature.ExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                confidence += 0.6f; // Base confidence for name match
            }

            if (fileInfo != null)
            {
                // File description match
                if (signature.MatchName && !string.IsNullOrEmpty(signature.MetaName) && 
                    fileInfo.FileDescription?.Contains(signature.MetaName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    confidence += 0.3f;
                }

                // Company/Publisher match  
                if (signature.MatchPublisher && !string.IsNullOrEmpty(signature.Publisher) &&
                    fileInfo.CompanyName?.Contains(signature.Publisher, StringComparison.OrdinalIgnoreCase) == true)
                {
                    confidence += 0.2f;
                }

                // Version match
                if (signature.MatchVersion && !string.IsNullOrEmpty(signature.Version) &&
                    fileInfo.ProductVersion?.Contains(signature.Version, StringComparison.OrdinalIgnoreCase) == true)
                {
                    confidence += 0.1f;
                }
            }

            return Math.Min(confidence, 1.0f);
        }
    }
}
