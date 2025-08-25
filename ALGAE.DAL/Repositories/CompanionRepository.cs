using Dapper;
using Algae.DAL;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public class CompanionRepository : ICompanionRepository
    {
        private readonly DatabaseContext _dbContext;

        public CompanionRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Companion>> GetAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<Companion>("SELECT * FROM CompanionApps");
        }

        public async Task<Companion?> GetByIdAsync(int companionId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<Companion>(
                "SELECT * FROM CompanionApps WHERE CompanionId = @CompanionId", 
                new { CompanionId = companionId });
        }

        public async Task<IEnumerable<Companion>> GetByTypeAsync(string type)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<Companion>(
                "SELECT * FROM CompanionApps WHERE Type = @Type", 
                new { Type = type });
        }

        public async Task<IEnumerable<Companion>> GetByGameIdAsync(int gameId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<Companion>(
                "SELECT * FROM CompanionApps WHERE GameId = @GameId", 
                new { GameId = gameId });
        }

        public async Task<IEnumerable<Companion>> GetGlobalCompanionsAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<Companion>(
                "SELECT * FROM CompanionApps WHERE GameId IS NULL");
        }

        public async Task<IEnumerable<Companion>> GetForGameAsync(int gameId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<Companion>(
                "SELECT * FROM CompanionApps WHERE GameId = @GameId OR GameId IS NULL ORDER BY GameId DESC, Name", 
                new { GameId = gameId });
        }

        public async Task AddAsync(Companion companion)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO CompanionApps (
                    GameId, Name, Type, PathOrURL, LaunchHelper, Browser, OpenInNewWindow
                ) VALUES (
                    @GameId, @Name, @Type, @PathOrURL, @LaunchHelper, @Browser, @OpenInNewWindow
                )";
            await connection.ExecuteAsync(sql, companion);
        }

        public async Task UpdateAsync(Companion companion)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                UPDATE CompanionApps SET 
                    GameId = @GameId,
                    Name = @Name,
                    Type = @Type,
                    PathOrURL = @PathOrURL,
                    LaunchHelper = @LaunchHelper,
                    Browser = @Browser,
                    OpenInNewWindow = @OpenInNewWindow
                WHERE CompanionId = @CompanionId";
            await connection.ExecuteAsync(sql, companion);
        }

        public async Task DeleteAsync(int companionId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM CompanionApps WHERE CompanionId = @CompanionId", 
                new { CompanionId = companionId });
        }
    }
}
