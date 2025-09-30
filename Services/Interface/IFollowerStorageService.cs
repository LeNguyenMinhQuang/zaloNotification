using System.Threading.Tasks;
using System.Collections.Generic;

namespace SigmaNotificationBackend.Services
{
    public interface IFollowerStorageService
    {
        Task AddFollowerAsync(string userId);
        Task RemoveFollowerAsync(string userId);
        Task<List<string>> GetAllFollowerUserIdsAsync();
        Task<bool> IsFollowerAsync(string userId);
    }
}