using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public interface IGameRepository
    {
        Task<IEnumerable<Game>> GetAllAsync();
        Task<Game?> GetByIdAsync(int id);
        Task AddAsync(Game game);
        Task UpdateAsync(Game game);
        Task DeleteAsync(int id);
    }
}
