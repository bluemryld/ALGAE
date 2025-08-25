using Dapper;
using Algae.DAL;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public class SearchPathRepository : ISearchPathRepository
    {
        private readonly DatabaseContext _dbContext;

        public SearchPathRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<SearchPath>> GetAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<SearchPath>("SELECT * FROM SearchPaths ORDER BY Path");
        }

        public async Task<SearchPath?> GetByIdAsync(int searchPathId)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<SearchPath>(
                "SELECT * FROM SearchPaths WHERE SearchPathId = @SearchPathId",
                new { SearchPathId = searchPathId });
        }

        public async Task<SearchPath?> GetByPathAsync(string path)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<SearchPath>(
                "SELECT * FROM SearchPaths WHERE Path = @Path",
                new { Path = path });
        }

        public async Task<bool> PathExistsAsync(string path)
        {
            using var connection = _dbContext.GetConnection();
            var count = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM SearchPaths WHERE Path = @Path",
                new { Path = path });
            return count > 0;
        }

        public async Task AddAsync(SearchPath searchPath)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO SearchPaths (Path) 
                VALUES (@Path)";
            await connection.ExecuteAsync(sql, searchPath);
        }

        public async Task AddAsync(string path)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO SearchPaths (Path) 
                VALUES (@Path)";
            await connection.ExecuteAsync(sql, new { Path = path });
        }

        public async Task UpdateAsync(SearchPath searchPath)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                UPDATE SearchPaths SET 
                    Path = @Path
                WHERE SearchPathId = @SearchPathId";
            await connection.ExecuteAsync(sql, searchPath);
        }

        public async Task DeleteAsync(int searchPathId)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM SearchPaths WHERE SearchPathId = @SearchPathId",
                new { SearchPathId = searchPathId });
        }

        public async Task DeleteByPathAsync(string path)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM SearchPaths WHERE Path = @Path",
                new { Path = path });
        }

        public async Task ClearAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM SearchPaths");
        }
    }
}
