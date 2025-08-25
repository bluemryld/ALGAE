using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Algae.DAL.Models;
using ALGAE.DAL.Repositories;

namespace ALGAE.Services
{
    /// <summary>
    /// Service for launching and managing companion applications
    /// </summary>
    public class CompanionLaunchService : ICompanionLaunchService
    {
        private readonly ICompanionProfileRepository _companionProfileRepository;
        private readonly INotificationService _notificationService;
        private readonly ConcurrentDictionary<int, CompanionProcess> _runningCompanions = new();

        public CompanionLaunchService(
            ICompanionProfileRepository companionProfileRepository,
            INotificationService notificationService)
        {
            _companionProfileRepository = companionProfileRepository;
            _notificationService = notificationService;
        }

        public async Task<IEnumerable<CompanionProcess>> LaunchCompanionsForProfileAsync(int profileId)
        {
            var launchedCompanions = new List<CompanionProcess>();
            
            try
            {
                // Get all enabled companions for this profile
                var enabledCompanions = await _companionProfileRepository.GetEnabledCompanionsByProfileIdAsync(profileId);
                
                Debug.WriteLine($"CompanionLaunchService: Found {enabledCompanions.Count()} enabled companions for profile {profileId}");
                
                foreach (var companion in enabledCompanions)
                {
                    try
                    {
                        var companionProcess = await LaunchCompanionAsync(companion);
                        if (companionProcess != null)
                        {
                            companionProcess.ProfileId = profileId;
                            launchedCompanions.Add(companionProcess);
                            Debug.WriteLine($"CompanionLaunchService: Successfully launched companion {companion.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"CompanionLaunchService: Failed to launch companion {companion.Name}: {ex.Message}");
                        _notificationService.ShowWarning($"Failed to start companion '{companion.Name}': {ex.Message}");
                    }
                }

                if (launchedCompanions.Any())
                {
                    _notificationService.ShowSuccess($"Successfully launched {launchedCompanions.Count} companion(s)");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CompanionLaunchService: Error launching companions for profile {profileId}: {ex.Message}");
                _notificationService.ShowError($"Error launching companions: {ex.Message}");
            }

            return launchedCompanions;
        }

        public async Task<CompanionProcess?> LaunchCompanionAsync(Companion companion)
        {
            try
            {
                // Check if companion is already running
                if (IsCompanionRunning(companion.CompanionId))
                {
                    Debug.WriteLine($"CompanionLaunchService: Companion {companion.Name} is already running");
                    return _runningCompanions.Values.FirstOrDefault(c => c.CompanionId == companion.CompanionId);
                }

                Process? process = null;
                
                switch (companion.Type.ToLowerInvariant())
                {
                    case "application":
                        process = await LaunchApplicationAsync(companion);
                        break;
                    case "website":
                        process = await LaunchWebsiteAsync(companion);
                        break;
                    case "steam":
                        process = await LaunchSteamGameAsync(companion);
                        break;
                    default:
                        // Try to launch as application by default
                        process = await LaunchApplicationAsync(companion);
                        break;
                }

                if (process != null)
                {
                    var companionProcess = new CompanionProcess
                    {
                        CompanionId = companion.CompanionId,
                        CompanionName = companion.Name,
                        ProcessId = process.Id,
                        StartTime = process.StartTime,
                        Process = process
                    };

                    _runningCompanions.TryAdd(companion.CompanionId, companionProcess);
                    
                    // Monitor process exit
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, e) => _runningCompanions.TryRemove(companion.CompanionId, out _);

                    return companionProcess;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CompanionLaunchService: Error launching companion {companion.Name}: {ex.Message}");
                throw;
            }

            return null;
        }

        private async Task<Process?> LaunchApplicationAsync(Companion companion)
        {
            if (string.IsNullOrEmpty(companion.PathOrURL))
            {
                throw new InvalidOperationException($"No path specified for companion '{companion.Name}'");
            }

            // Check if the file exists
            if (!File.Exists(companion.PathOrURL))
            {
                throw new FileNotFoundException($"Companion executable not found: {companion.PathOrURL}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = companion.PathOrURL,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(companion.PathOrURL)
            };

            return Process.Start(startInfo);
        }

        private async Task<Process?> LaunchWebsiteAsync(Companion companion)
        {
            if (string.IsNullOrEmpty(companion.PathOrURL))
            {
                throw new InvalidOperationException($"No URL specified for companion '{companion.Name}'");
            }

            var startInfo = new ProcessStartInfo();
            
            if (!string.IsNullOrEmpty(companion.Browser))
            {
                // Use specific browser
                startInfo.FileName = companion.Browser;
                startInfo.Arguments = companion.OpenInNewWindow ? $"--new-window \"{companion.PathOrURL}\"" : $"\"{companion.PathOrURL}\"";
            }
            else
            {
                // Use default browser
                startInfo.FileName = companion.PathOrURL;
                startInfo.UseShellExecute = true;
            }

            return Process.Start(startInfo);
        }

        private async Task<Process?> LaunchSteamGameAsync(Companion companion)
        {
            if (string.IsNullOrEmpty(companion.PathOrURL))
            {
                throw new InvalidOperationException($"No Steam URL/ID specified for companion '{companion.Name}'");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "steam",
                Arguments = $"steam://run/{companion.PathOrURL}",
                UseShellExecute = true
            };

            return Process.Start(startInfo);
        }

        public async Task<bool> StopCompanionAsync(CompanionProcess companionProcess)
        {
            try
            {
                if (companionProcess.Process != null && !companionProcess.Process.HasExited)
                {
                    companionProcess.Process.Kill();
                    _runningCompanions.TryRemove(companionProcess.CompanionId, out _);
                    Debug.WriteLine($"CompanionLaunchService: Stopped companion {companionProcess.CompanionName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CompanionLaunchService: Error stopping companion {companionProcess.CompanionName}: {ex.Message}");
                _notificationService.ShowWarning($"Error stopping companion '{companionProcess.CompanionName}': {ex.Message}");
            }

            return false;
        }

        public async Task<int> StopAllCompanionsForProfileAsync(int profileId)
        {
            var stoppedCount = 0;
            var companionsToStop = _runningCompanions.Values
                .Where(c => c.ProfileId == profileId)
                .ToList();

            foreach (var companion in companionsToStop)
            {
                if (await StopCompanionAsync(companion))
                {
                    stoppedCount++;
                }
            }

            if (stoppedCount > 0)
            {
                _notificationService.ShowInformation($"Stopped {stoppedCount} companion(s)");
            }

            return stoppedCount;
        }

        public IEnumerable<CompanionProcess> GetRunningCompanions()
        {
            // Clean up any dead processes
            var deadCompanions = _runningCompanions.Values
                .Where(c => c.Process?.HasExited == true)
                .ToList();

            foreach (var deadCompanion in deadCompanions)
            {
                _runningCompanions.TryRemove(deadCompanion.CompanionId, out _);
            }

            return _runningCompanions.Values.ToList();
        }

        public bool IsCompanionRunning(int companionId)
        {
            if (_runningCompanions.TryGetValue(companionId, out var companion))
            {
                if (companion.Process?.HasExited == false)
                {
                    return true;
                }
                else
                {
                    // Clean up dead process
                    _runningCompanions.TryRemove(companionId, out _);
                }
            }

            return false;
        }
    }
}
