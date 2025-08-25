using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public interface ICompanionRepository
    {
        Task<IEnumerable<Companion>> GetAllAsync();
        Task<Companion?> GetByIdAsync(int companionId);
        Task<IEnumerable<Companion>> GetByTypeAsync(string type);
        Task<IEnumerable<Companion>> GetByGameIdAsync(int gameId);
        Task<IEnumerable<Companion>> GetGlobalCompanionsAsync(); // GameId is NULL
        Task<IEnumerable<Companion>> GetForGameAsync(int gameId); // Game-specific + Global companions
        Task AddAsync(Companion companion);
        Task UpdateAsync(Companion companion);
        Task DeleteAsync(int companionId);
    }
}
