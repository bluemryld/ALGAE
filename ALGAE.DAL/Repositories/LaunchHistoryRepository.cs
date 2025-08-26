using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Algae.DAL;
using Algae.DAL.Models;
using Dapper;

namespace ALGAE.DAL.Repositories
{
    public class LaunchHistoryRepository : ILaunchHistoryRepository
    {
        private readonly DatabaseContext _dbContext;

        public LaunchHistoryRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<LaunchHistory>> GetAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<LaunchHistory>("SELECT * FROM LaunchHistory ORDER BY LaunchTime DESC");
        }

        public async Task<LaunchHistory?> GetByIdAsync(int launchId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<LaunchHistory>(
                "SELECT * FROM LaunchHistory WHERE LaunchId = @LaunchId", 
                new { LaunchId = launchId });
        }

        public async Task<IEnumerable<LaunchHistory>> GetByGameIdAsync(int gameId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<LaunchHistory>(
                "SELECT * FROM LaunchHistory WHERE GameId = @GameId ORDER BY LaunchTime DESC", 
                new { GameId = gameId });
        }

        public async Task<IEnumerable<LaunchHistory>> GetRecentAsync(int count = 50)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<LaunchHistory>(
                "SELECT * FROM LaunchHistory ORDER BY LaunchTime DESC LIMIT @Count", 
                new { Count = count });
        }

        public async Task<IEnumerable<LaunchHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<LaunchHistory>(
                "SELECT * FROM LaunchHistory WHERE LaunchTime BETWEEN @StartDate AND @EndDate ORDER BY LaunchTime DESC",
                new { StartDate = startDate, EndDate = endDate });
        }

        public async Task<IEnumerable<LaunchHistory>> GetSuccessfulLaunchesAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<LaunchHistory>(
                "SELECT * FROM LaunchHistory WHERE Success = 1 ORDER BY LaunchTime DESC");
        }

        public async Task<IEnumerable<LaunchHistory>> GetFailedLaunchesAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<LaunchHistory>(
                "SELECT * FROM LaunchHistory WHERE Success = 0 ORDER BY LaunchTime DESC");
        }

        public async Task<IEnumerable<LaunchHistory>> GetRunningGamesAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<LaunchHistory>(
                "SELECT * FROM LaunchHistory WHERE Success = 1 AND EndTime IS NULL ORDER BY LaunchTime DESC");
        }

        public async Task<int> AddAsync(LaunchHistory launchHistory)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO LaunchHistory (
                    GameId, GameName, LaunchTime, EndTime, Success, ErrorMessage,
                    ProcessId, ExecutablePath, WorkingDirectory, LaunchArguments,
                    PeakMemoryUsage, AverageCpuUsage
                ) VALUES (
                    @GameId, @GameName, @LaunchTime, @EndTime, @Success, @ErrorMessage,
                    @ProcessId, @ExecutablePath, @WorkingDirectory, @LaunchArguments,
                    @PeakMemoryUsage, @AverageCpuUsage
                );
                SELECT last_insert_rowid();";
                
            var result = await connection.QuerySingleAsync<int>(sql, launchHistory);
            launchHistory.LaunchId = result;
            return result;
        }

        public async Task UpdateAsync(LaunchHistory launchHistory)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                UPDATE LaunchHistory SET 
                    GameId = @GameId,
                    GameName = @GameName,
                    LaunchTime = @LaunchTime,
                    EndTime = @EndTime,
                    Success = @Success,
                    ErrorMessage = @ErrorMessage,
                    ProcessId = @ProcessId,
                    ExecutablePath = @ExecutablePath,
                    WorkingDirectory = @WorkingDirectory,
                    LaunchArguments = @LaunchArguments,
                    PeakMemoryUsage = @PeakMemoryUsage,
                    AverageCpuUsage = @AverageCpuUsage
                WHERE LaunchId = @LaunchId";
                
            await connection.ExecuteAsync(sql, launchHistory);
        }

        public async Task MarkCompletedAsync(int launchId, DateTime endTime, string? peakMemoryUsage = null, double? averageCpuUsage = null)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                UPDATE LaunchHistory SET 
                    EndTime = @EndTime,
                    PeakMemoryUsage = COALESCE(@PeakMemoryUsage, PeakMemoryUsage),
                    AverageCpuUsage = COALESCE(@AverageCpuUsage, AverageCpuUsage)
                WHERE LaunchId = @LaunchId";
                
            await connection.ExecuteAsync(sql, new { 
                LaunchId = launchId, 
                EndTime = endTime,
                PeakMemoryUsage = peakMemoryUsage,
                AverageCpuUsage = averageCpuUsage
            });
        }

        public async Task DeleteAsync(int launchId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM LaunchHistory WHERE LaunchId = @LaunchId", 
                new { LaunchId = launchId });
        }

        public async Task DeleteOldRecordsAsync(int olderThanDays)
        {
            using var connection = _dbContext.GetConnection();
            var cutoffDate = DateTime.Now.AddDays(-olderThanDays);
            await connection.ExecuteAsync("DELETE FROM LaunchHistory WHERE LaunchTime < @CutoffDate", 
                new { CutoffDate = cutoffDate });
        }

        public async Task<LaunchStatistics> GetLaunchStatisticsAsync(int gameId)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                SELECT 
                    COUNT(*) as TotalLaunches,
                    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) as SuccessfulLaunches,
                    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) as FailedLaunches,
                    MIN(LaunchTime) as FirstLaunched,
                    MAX(LaunchTime) as LastLaunched
                FROM LaunchHistory 
                WHERE GameId = @GameId";

            var basicStats = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { GameId = gameId });

            // Calculate play time statistics
            const string playTimeSql = @"
                SELECT 
                    SUM(JULIANDAY(EndTime) - JULIANDAY(LaunchTime)) * 24 * 60 * 60 as TotalPlayTimeSeconds,
                    AVG(JULIANDAY(EndTime) - JULIANDAY(LaunchTime)) * 24 * 60 * 60 as AveragePlayTimeSeconds
                FROM LaunchHistory 
                WHERE GameId = @GameId AND EndTime IS NOT NULL AND Success = 1";

            var playTimeStats = await connection.QuerySingleOrDefaultAsync<dynamic>(playTimeSql, new { GameId = gameId });

            return new LaunchStatistics
            {
                TotalLaunches = basicStats?.TotalLaunches ?? 0,
                SuccessfulLaunches = basicStats?.SuccessfulLaunches ?? 0,
                FailedLaunches = basicStats?.FailedLaunches ?? 0,
                FirstLaunched = basicStats?.FirstLaunched,
                LastLaunched = basicStats?.LastLaunched,
                TotalPlayTime = TimeSpan.FromSeconds(playTimeStats?.TotalPlayTimeSeconds ?? 0),
                AveragePlayTime = TimeSpan.FromSeconds(playTimeStats?.AveragePlayTimeSeconds ?? 0)
            };
        }

        public async Task<LaunchStatistics> GetOverallStatisticsAsync()
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                SELECT 
                    COUNT(*) as TotalLaunches,
                    SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) as SuccessfulLaunches,
                    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) as FailedLaunches,
                    MIN(LaunchTime) as FirstLaunched,
                    MAX(LaunchTime) as LastLaunched
                FROM LaunchHistory";

            var basicStats = await connection.QuerySingleOrDefaultAsync<dynamic>(sql);

            // Calculate play time statistics
            const string playTimeSql = @"
                SELECT 
                    SUM(JULIANDAY(EndTime) - JULIANDAY(LaunchTime)) * 24 * 60 * 60 as TotalPlayTimeSeconds,
                    AVG(JULIANDAY(EndTime) - JULIANDAY(LaunchTime)) * 24 * 60 * 60 as AveragePlayTimeSeconds
                FROM LaunchHistory 
                WHERE EndTime IS NOT NULL AND Success = 1";

            var playTimeStats = await connection.QuerySingleOrDefaultAsync<dynamic>(playTimeSql);

            return new LaunchStatistics
            {
                TotalLaunches = basicStats?.TotalLaunches ?? 0,
                SuccessfulLaunches = basicStats?.SuccessfulLaunches ?? 0,
                FailedLaunches = basicStats?.FailedLaunches ?? 0,
                FirstLaunched = basicStats?.FirstLaunched,
                LastLaunched = basicStats?.LastLaunched,
                TotalPlayTime = TimeSpan.FromSeconds(playTimeStats?.TotalPlayTimeSeconds ?? 0),
                AveragePlayTime = TimeSpan.FromSeconds(playTimeStats?.AveragePlayTimeSeconds ?? 0)
            };
        }
    }
}
