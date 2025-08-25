using Dapper;
using Algae.DAL;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public class ProfilesRepository : IProfilesRepository
    {
        private readonly DatabaseContext _dbContext;

        public ProfilesRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Profile>> GetAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<Profile>("SELECT * FROM Profiles");
        }

        public async Task<IEnumerable<Profile>> GetAllByGameIdAsync(int gameId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<Profile>("SELECT * FROM Profiles WHERE GameId = @GameId", new { GameId = gameId });
        }

        public async Task<Profile?> GetByIdAsync(int profileId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<Profile>("SELECT * FROM Profiles WHERE ProfileId = @ProfileId", new { ProfileId = profileId });
        }

        public async Task AddAsync(Profile profile)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO Profiles (GameId, ProfileName, CommandLineArgs) 
                VALUES (@GameId, @ProfileName, @CommandLineArgs)";
            await connection.ExecuteAsync(sql, profile);
        }

        public async Task UpdateAsync(Profile profile)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                UPDATE Profiles SET 
                    GameId = @GameId,
                    ProfileName = @ProfileName,
                    CommandLineArgs = @CommandLineArgs
                WHERE ProfileId = @ProfileId";
            await connection.ExecuteAsync(sql, profile);
        }

        public async Task DeleteAsync(int profileId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM Profiles WHERE ProfileId = @ProfileId", new { ProfileId = profileId });
        }
    }
}
