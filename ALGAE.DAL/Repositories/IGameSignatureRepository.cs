using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public interface IGameSignatureRepository
    {
        Task<IEnumerable<GameSignature>> GetAllAsync();
        Task<GameSignature?> GetByIdAsync(int gameSignatureId);
        Task<IEnumerable<GameSignature>> GetByExecutableNameAsync(string executableName);
        Task<IEnumerable<GameSignature>> GetByPublisherAsync(string publisher);
        Task<IEnumerable<GameSignature>> SearchByNameAsync(string name);
        Task<IEnumerable<GameSignature>> GetMatchingGamesAsync(string name, string? version, string? publisher);
        Task AddAsync(GameSignature gameSignature);
        Task UpdateAsync(GameSignature gameSignature);
        Task DeleteAsync(int gameSignatureId);
    }
}
