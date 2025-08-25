using Dapper;
using Algae.DAL;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public class CompanionSignatureRepository : ICompanionSignatureRepository
    {
        private readonly DatabaseContext _dbContext;

        public CompanionSignatureRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<CompanionSignature>> GetAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<CompanionSignature>("SELECT * FROM CompanionSignatures");
        }

        public async Task<CompanionSignature?> GetByIdAsync(int companionSignatureId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<CompanionSignature>(
                "SELECT * FROM CompanionSignatures WHERE CompanionSignatureId = @CompanionSignatureId",
                new { CompanionSignatureId = companionSignatureId });
        }

        public async Task<IEnumerable<CompanionSignature>> GetByGameSignatureIdAsync(int gameSignatureId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<CompanionSignature>(
                "SELECT * FROM CompanionSignatures WHERE GameSignatureId = @GameSignatureId",
                new { GameSignatureId = gameSignatureId });
        }

        public async Task<IEnumerable<CompanionSignature>> GetByExecutableNameAsync(string executableName)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<CompanionSignature>(
                "SELECT * FROM CompanionSignatures WHERE ExecutableName = @ExecutableName",
                new { ExecutableName = executableName });
        }

        public async Task<IEnumerable<CompanionSignature>> GetByPublisherAsync(string publisher)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<CompanionSignature>(
                "SELECT * FROM CompanionSignatures WHERE Publisher = @Publisher",
                new { Publisher = publisher });
        }

        public async Task<IEnumerable<CompanionSignature>> SearchByNameAsync(string name)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<CompanionSignature>(
                "SELECT * FROM CompanionSignatures WHERE Name LIKE @Name",
                new { Name = $"%{name}%" });
        }

        public async Task<IEnumerable<CompanionSignature>> GetMatchingCompanionsAsync(string name, string? version, string? publisher)
        {
            using var connection = _dbContext.GetConnection();
            var sql = "SELECT * FROM CompanionSignatures WHERE 1=1";
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(name))
            {
                sql += " AND (MatchName = 0 OR Name = @Name)";
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

            return await connection.QueryAsync<CompanionSignature>(sql, parameters);
        }

        public async Task AddAsync(CompanionSignature companionSignature)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO CompanionSignatures (
                    GameSignatureId, Name, Description, ExecutableName, CompanionArgs, 
                    Version, Publisher, MetaName, MatchName, MatchVersion, MatchPublisher
                ) VALUES (
                    @GameSignatureId, @Name, @Description, @ExecutableName, @CompanionArgs,
                    @Version, @Publisher, @MetaName, @MatchName, @MatchVersion, @MatchPublisher
                )";
            await connection.ExecuteAsync(sql, companionSignature);
        }

        public async Task UpdateAsync(CompanionSignature companionSignature)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                UPDATE CompanionSignatures SET 
                    GameSignatureId = @GameSignatureId,
                    Name = @Name,
                    Description = @Description,
                    ExecutableName = @ExecutableName,
                    CompanionArgs = @CompanionArgs,
                    Version = @Version,
                    Publisher = @Publisher,
                    MetaName = @MetaName,
                    MatchName = @MatchName,
                    MatchVersion = @MatchVersion,
                    MatchPublisher = @MatchPublisher
                WHERE CompanionSignatureId = @CompanionSignatureId";
            await connection.ExecuteAsync(sql, companionSignature);
        }

        public async Task DeleteAsync(int companionSignatureId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM CompanionSignatures WHERE CompanionSignatureId = @CompanionSignatureId",
                new { CompanionSignatureId = companionSignatureId });
        }

        public async Task DeleteAllByGameSignatureIdAsync(int gameSignatureId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM CompanionSignatures WHERE GameSignatureId = @GameSignatureId",
                new { GameSignatureId = gameSignatureId });
        }
    }
}
