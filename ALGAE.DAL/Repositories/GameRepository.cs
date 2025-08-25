using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using Algae.DAL;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly DatabaseContext _dbContext;

        public GameRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Game>> GetAllAsync()
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QueryAsync<Game>("SELECT * FROM Games");
        }

        public async Task<Game?> GetByIdAsync(int id)
        {
            using var connection = _dbContext.GetConnection();
            return await connection.QuerySingleOrDefaultAsync<Game>("SELECT * FROM Games WHERE GameId = @GameId", new { GameId = id });
        }

        public async Task AddAsync(Game game)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                INSERT INTO Games (
                    ShortName, Name, Description, GameImage, ThemeName, 
                    InstallPath, GameWorkingPath, ExecutableName, GameArgs, 
                    Version, Publisher
                ) VALUES (
                    @ShortName, @Name, @Description, @GameImage, @ThemeName,
                    @InstallPath, @GameWorkingPath, @ExecutableName, @GameArgs,
                    @Version, @Publisher
                )";
            await connection.ExecuteAsync(sql, game);
        }

        public async Task UpdateAsync(Game game)
        {
            using var connection = _dbContext.GetConnection();
            const string sql = @"
                UPDATE Games SET 
                    ShortName = @ShortName,
                    Name = @Name,
                    Description = @Description,
                    GameImage = @GameImage,
                    ThemeName = @ThemeName,
                    InstallPath = @InstallPath,
                    GameWorkingPath = @GameWorkingPath,
                    ExecutableName = @ExecutableName,
                    GameArgs = @GameArgs,
                    Version = @Version,
                    Publisher = @Publisher
                WHERE GameId = @GameId";
            await connection.ExecuteAsync(sql, game);
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = _dbContext.GetConnection();
            await connection.ExecuteAsync("DELETE FROM Games WHERE GameId = @GameId", new { GameId = id });
        }
    }
}
