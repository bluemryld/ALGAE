using Dapper;
using Algae.DAL;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public class CompanionProfileRepository : ICompanionProfileRepository
    {
        private readonly DatabaseContext _dbContext;

        public CompanionProfileRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<CompanionProfile>> GetAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<CompanionProfile>("SELECT * FROM CompanionProfiles");
        }

        public async Task<CompanionProfile?> GetByIdAsync(int companionProfileId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<CompanionProfile>(
                "SELECT * FROM CompanionProfiles WHERE CompanionProfileId = @CompanionProfileId",
                new { CompanionProfileId = companionProfileId });
        }

        public async Task<IEnumerable<CompanionProfile>> GetByProfileIdAsync(int profileId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<CompanionProfile>(
                "SELECT * FROM CompanionProfiles WHERE ProfileId = @ProfileId",
                new { ProfileId = profileId });
        }

        public async Task<IEnumerable<CompanionProfile>> GetByCompanionIdAsync(int companionId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<CompanionProfile>(
                "SELECT * FROM CompanionProfiles WHERE CompanionId = @CompanionId",
                new { CompanionId = companionId });
        }

        public async Task<IEnumerable<Companion>> GetCompanionsByProfileIdAsync(int profileId)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                SELECT c.* FROM CompanionApps c
                INNER JOIN CompanionProfiles cp ON c.CompanionId = cp.CompanionId
                WHERE cp.ProfileId = @ProfileId";
            return await connection.QueryAsync<Companion>(sql, new { ProfileId = profileId });
        }

        public async Task<IEnumerable<Profile>> GetProfilesByCompanionIdAsync(int companionId)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                SELECT p.* FROM Profiles p
                INNER JOIN CompanionProfiles cp ON p.ProfileId = cp.ProfileId
                WHERE cp.CompanionId = @CompanionId";
            return await connection.QueryAsync<Profile>(sql, new { CompanionId = companionId });
        }

        public async Task AddAsync(CompanionProfile companionProfile)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO CompanionProfiles (ProfileId, CompanionId, IsEnabled) 
                VALUES (@ProfileId, @CompanionId, @IsEnabled)";
            await connection.ExecuteAsync(sql, companionProfile);
        }

        public async Task AddAsync(int profileId, int companionId, bool isEnabled = true)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO CompanionProfiles (ProfileId, CompanionId, IsEnabled) 
                VALUES (@ProfileId, @CompanionId, @IsEnabled)";
            await connection.ExecuteAsync(sql, new { ProfileId = profileId, CompanionId = companionId, IsEnabled = isEnabled });
        }

        public async Task DeleteAsync(int companionProfileId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync(
                "DELETE FROM CompanionProfiles WHERE CompanionProfileId = @CompanionProfileId",
                new { CompanionProfileId = companionProfileId });
        }

        public async Task DeleteByProfileAndCompanionAsync(int profileId, int companionId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync(
                "DELETE FROM CompanionProfiles WHERE ProfileId = @ProfileId AND CompanionId = @CompanionId",
                new { ProfileId = profileId, CompanionId = companionId });
        }

        public async Task DeleteAllByProfileIdAsync(int profileId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync(
                "DELETE FROM CompanionProfiles WHERE ProfileId = @ProfileId",
                new { ProfileId = profileId });
        }

        public async Task DeleteAllByCompanionIdAsync(int companionId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync(
                "DELETE FROM CompanionProfiles WHERE CompanionId = @CompanionId",
                new { CompanionId = companionId });
        }

        public async Task UpdateEnabledStatusAsync(int profileId, int companionId, bool isEnabled)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync(
                "UPDATE CompanionProfiles SET IsEnabled = @IsEnabled WHERE ProfileId = @ProfileId AND CompanionId = @CompanionId",
                new { IsEnabled = isEnabled, ProfileId = profileId, CompanionId = companionId });
        }

        public async Task<IEnumerable<Companion>> GetEnabledCompanionsByProfileIdAsync(int profileId)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                SELECT c.* FROM CompanionApps c
                INNER JOIN CompanionProfiles cp ON c.CompanionId = cp.CompanionId
                WHERE cp.ProfileId = @ProfileId AND cp.IsEnabled = 1";
            return await connection.QueryAsync<Companion>(sql, new { ProfileId = profileId });
        }
    }
}
