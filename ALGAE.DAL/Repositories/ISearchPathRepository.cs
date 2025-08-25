using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Algae.DAL.Models;

namespace ALGAE.DAL.Repositories
{
    public interface ISearchPathRepository
    {
        Task<IEnumerable<SearchPath>> GetAllAsync();
        Task<SearchPath?> GetByIdAsync(int searchPathId);
        Task<SearchPath?> GetByPathAsync(string path);
        Task<bool> PathExistsAsync(string path);
        Task AddAsync(SearchPath searchPath);
        Task AddAsync(string path);
        Task UpdateAsync(SearchPath searchPath);
        Task DeleteAsync(int searchPathId);
        Task DeleteByPathAsync(string path);
        Task ClearAllAsync();
    }
}
