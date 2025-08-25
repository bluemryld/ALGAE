using Dapper;
using Algae.DAL;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public class GameSignatureRepository : IGameSignatureRepository
    {
        private readonly DatabaseContext _dbContext;

        public GameSignatureRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<GameSignature>> GetAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<GameSignature>("SELECT * FROM GameSignatures");
        }

        public async Task<GameSignature?> GetByIdAsync(int gameSignatureId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<GameSignature>(
                "SELECT * FROM GameSignatures WHERE GameSignatureId = @GameSignatureId",
                new { GameSignatureId = gameSignatureId });
        }

        public async Task<IEnumerable<GameSignature>> GetByExecutableNameAsync(string executableName)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<GameSignature>(
                "SELECT * FROM GameSignatures WHERE ExecutableName = @ExecutableName",
                new { ExecutableName = executableName });
        }

        public async Task<IEnumerable<GameSignature>> GetByPublisherAsync(string publisher)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<GameSignature>(
                "SELECT * FROM GameSignatures WHERE Publisher = @Publisher",
                new { Publisher = publisher });
        }

        public async Task<IEnumerable<GameSignature>> SearchByNameAsync(string name)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<GameSignature>(
                "SELECT * FROM GameSignatures WHERE Name LIKE @Name OR ShortName LIKE @Name",
                new { Name = $"%{name}%" });
        }

        public async Task<IEnumerable<GameSignature>> GetMatchingGamesAsync(string name, string? version, string? publisher)
        {
            using var connection = _dbContext.GetConnection();
            var sql = "SELECT * FROM GameSignatures WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(name))
            {
                sql += " AND (MatchName = 0 OR (Name = @Name OR ShortName = @Name))";
                parameters.Add("Name", name);
            }

            if (!string.IsNullOrEmpty(version))
            {
                sql += " AND (MatchVersion = 0 OR Version = @Version)";
                parameters.Add("Version", version);
            }

            if (!string.IsNullOrEmpty(publisher))
            {
                sql += " AND (MatchPublisher = 0 OR Publisher = @Publisher)";
                parameters.Add("Publisher", publisher);
            }

            return await connection.QueryAsync<GameSignature>(sql, parameters);
        }

        public async Task AddAsync(GameSignature gameSignature)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO GameSignatures (
                    ShortName, Name, Description, GameImage, ThemeName, ExecutableName, 
                    GameArgs, Version, Publisher, MetaName, MatchName, MatchVersion, MatchPublisher
                ) VALUES (
                    @ShortName, @Name, @Description, @GameImage, @ThemeName, @ExecutableName,
                    @GameArgs, @Version, @Publisher, @MetaName, @MatchName, @MatchVersion, @MatchPublisher
                )";
            await connection.ExecuteAsync(sql, gameSignature);
        }

        public async Task UpdateAsync(GameSignature gameSignature)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                UPDATE GameSignatures SET 
                    ShortName = @ShortName,
                    Name = @Name,
                    Description = @Description,
                    GameImage = @GameImage,
                    ThemeName = @ThemeName,
                    ExecutableName = @ExecutableName,
                    GameArgs = @GameArgs,
                    Version = @Version,
                    Publisher = @Publisher,
                    MetaName = @MetaName,
                    MatchName = @MatchName,
                    MatchVersion = @MatchVersion,
                    MatchPublisher = @MatchPublisher
                WHERE GameSignatureId = @GameSignatureId";
            await connection.ExecuteAsync(sql, gameSignature);
        }

        public async Task DeleteAsync(int gameSignatureId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM GameSignatures WHERE GameSignatureId = @GameSignatureId",
                new { GameSignatureId = gameSignatureId });
        }
    }
}
