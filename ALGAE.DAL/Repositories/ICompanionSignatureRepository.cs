using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public interface ICompanionSignatureRepository
    {
        Task<IEnumerable<CompanionSignature>> GetAllAsync();
        Task<CompanionSignature?> GetByIdAsync(int companionSignatureId);
        Task<IEnumerable<CompanionSignature>> GetByGameSignatureIdAsync(int gameSignatureId);
        Task<IEnumerable<CompanionSignature>> GetByExecutableNameAsync(string executableName);
        Task<IEnumerable<CompanionSignature>> GetByPublisherAsync(string publisher);
        Task<IEnumerable<CompanionSignature>> SearchByNameAsync(string name);
        Task<IEnumerable<CompanionSignature>> GetMatchingCompanionsAsync(string name, string? version, string? publisher);
        Task AddAsync(CompanionSignature companionSignature);
        Task UpdateAsync(CompanionSignature companionSignature);
        Task DeleteAsync(int companionSignatureId);
        Task DeleteAllByGameSignatureIdAsync(int gameSignatureId);
    }
}
