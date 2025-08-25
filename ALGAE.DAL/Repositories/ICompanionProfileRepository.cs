using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public interface ICompanionProfileRepository
    {
        Task<IEnumerable<CompanionProfile>> GetAllAsync();
        Task<CompanionProfile?> GetByIdAsync(int companionProfileId);
        Task<IEnumerable<CompanionProfile>> GetByProfileIdAsync(int profileId);
        Task<IEnumerable<CompanionProfile>> GetByCompanionIdAsync(int companionId);
        Task<IEnumerable<Companion>> GetCompanionsByProfileIdAsync(int profileId);
        Task<IEnumerable<Profile>> GetProfilesByCompanionIdAsync(int companionId);
        Task AddAsync(CompanionProfile companionProfile);
        Task AddAsync(int profileId, int companionId, bool isEnabled = true);
        Task UpdateEnabledStatusAsync(int profileId, int companionId, bool isEnabled);
        Task<IEnumerable<Companion>> GetEnabledCompanionsByProfileIdAsync(int profileId);
        Task DeleteAsync(int companionProfileId);
        Task DeleteByProfileAndCompanionAsync(int profileId, int companionId);
        Task DeleteAllByProfileIdAsync(int profileId);
        Task DeleteAllByCompanionIdAsync(int companionId);
    }
}
