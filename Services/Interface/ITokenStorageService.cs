using System;
using System.Threading.Tasks;
using SigmaNotificationBackend.Models; // Đảm bảo namespace này đúng

namespace SigmaNotificationBackend.Services
{
    public interface ITokenStorageService
    {
        Task SaveTokensAsync(string accessToken, string refreshToken, long expiresInSeconds);
        Task<ZaloToken?> GetStoredTokensAsync();
        Task<string?> GetCurrentAccessTokenAsync();
    }

}