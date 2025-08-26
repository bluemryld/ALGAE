using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public interface ILaunchHistoryRepository
    {
        /// <summary>
        /// Gets all launch history records
        /// </summary>
        Task<IEnumerable<LaunchHistory>> GetAllAsync();

        /// <summary>
        /// Gets launch history record by ID
        /// </summary>
        Task<LaunchHistory?> GetByIdAsync(int launchId);

        /// <summary>
        /// Gets launch history for a specific game
        /// </summary>
        Task<IEnumerable<LaunchHistory>> GetByGameIdAsync(int gameId);

        /// <summary>
        /// Gets recent launch history records
        /// </summary>
        /// <param name="count">Number of recent records to retrieve</param>
        Task<IEnumerable<LaunchHistory>> GetRecentAsync(int count = 50);

        /// <summary>
        /// Gets launch history within a date range
        /// </summary>
        Task<IEnumerable<LaunchHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets successful launches only
        /// </summary>
        Task<IEnumerable<LaunchHistory>> GetSuccessfulLaunchesAsync();

        /// <summary>
        /// Gets failed launches only
        /// </summary>
        Task<IEnumerable<LaunchHistory>> GetFailedLaunchesAsync();

        /// <summary>
        /// Gets currently running games
        /// </summary>
        Task<IEnumerable<LaunchHistory>> GetRunningGamesAsync();

        /// <summary>
        /// Adds a new launch history record
        /// </summary>
        Task<int> AddAsync(LaunchHistory launchHistory);

        /// <summary>
        /// Updates an existing launch history record
        /// </summary>
        Task UpdateAsync(LaunchHistory launchHistory);

        /// <summary>
        /// Marks a launch as completed
        /// </summary>
        Task MarkCompletedAsync(int launchId, DateTime endTime, string? peakMemoryUsage = null, double? averageCpuUsage = null);

        /// <summary>
        /// Deletes launch history record
        /// </summary>
        Task DeleteAsync(int launchId);

        /// <summary>
        /// Deletes old launch history records
        /// </summary>
        /// <param name="olderThanDays">Delete records older than this many days</param>
        Task DeleteOldRecordsAsync(int olderThanDays);

        /// <summary>
        /// Gets launch statistics for a game
        /// </summary>
        Task<LaunchStatistics> GetLaunchStatisticsAsync(int gameId);

        /// <summary>
        /// Gets overall launch statistics
        /// </summary>
        Task<LaunchStatistics> GetOverallStatisticsAsync();
    }

    /// <summary>
    /// Launch statistics for games
    /// </summary>
    public class LaunchStatistics
    {
        public int TotalLaunches { get; set; }
        public int SuccessfulLaunches { get; set; }
        public int FailedLaunches { get; set; }
        public double SuccessRate => TotalLaunches > 0 ? (double)SuccessfulLaunches / TotalLaunches * 100 : 0;
        public TimeSpan TotalPlayTime { get; set; }
        public TimeSpan AveragePlayTime { get; set; }
        public DateTime? LastLaunched { get; set; }
        public DateTime? FirstLaunched { get; set; }
    }
}
