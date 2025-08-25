using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public interface IProfilesRepository
    {
        Task<IEnumerable<Profile>> GetAllAsync();
        Task<IEnumerable<Profile>> GetAllByGameIdAsync(int gameId);
        Task<Profile?> GetByIdAsync(int profileId);
        Task AddAsync(Profile profile);
        Task UpdateAsync(Profile profile);
        Task DeleteAsync(int profileId);
    }
}
