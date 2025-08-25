using Algae.DAL.Models;

namespace ALGAE.Services
{
    /// <summary>
    /// Service for launching and managing companion applications
    /// </summary>
    public interface ICompanionLaunchService
    {
        /// <summary>
        /// Launch all enabled companions for a given profile
        /// </summary>
        /// <param name="profileId">The profile ID to launch companions for</param>
        /// <returns>List of successfully started companion processes</returns>
        Task<IEnumerable<CompanionProcess>> LaunchCompanionsForProfileAsync(int profileId);

        /// <summary>
        /// Launch a specific companion
        /// </summary>
        /// <param name="companion">The companion to launch</param>
        /// <returns>The companion process information if successful, null otherwise</returns>
        Task<CompanionProcess?> LaunchCompanionAsync(Companion companion);

        /// <summary>
        /// Stop a running companion process
        /// </summary>
        /// <param name="companionProcess">The companion process to stop</param>
        /// <returns>True if successfully stopped, false otherwise</returns>
        Task<bool> StopCompanionAsync(CompanionProcess companionProcess);

        /// <summary>
        /// Stop all companions for a specific profile
        /// </summary>
        /// <param name="profileId">The profile ID to stop companions for</param>
        /// <returns>Number of companions stopped</returns>
        Task<int> StopAllCompanionsForProfileAsync(int profileId);

        /// <summary>
        /// Get all running companion processes
        /// </summary>
        /// <returns>Collection of running companion processes</returns>
        IEnumerable<CompanionProcess> GetRunningCompanions();

        /// <summary>
        /// Check if a companion is currently running
        /// </summary>
        /// <param name="companionId">The companion ID to check</param>
        /// <returns>True if the companion is running, false otherwise</returns>
        bool IsCompanionRunning(int companionId);
    }

    /// <summary>
    /// Represents a running companion process
    /// </summary>
    public class CompanionProcess
    {
        public int CompanionId { get; set; }
        public string CompanionName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public DateTime StartTime { get; set; }
        public int ProfileId { get; set; }
        public System.Diagnostics.Process? Process { get; set; }
    }
}
