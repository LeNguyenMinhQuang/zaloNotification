using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SigmaNotificationBackend.Models;
using System;
using System.Threading.Tasks;

namespace SigmaNotificationBackend.Services
{
    public class TokenStorageService : ITokenStorageService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TokenStorageService> _logger;

        private readonly HttpClient _httpClient;

        private readonly ZaloOAuthSettings _zaloOAuthSettings;

        public TokenStorageService(AppDbContext dbContext,
                                   ILogger<TokenStorageService> logger,
                                   IHttpClientFactory httpClientFactory,
                                   IOptions<ZaloOAuthSettings> zaloOAuthSettings)
        {
            _dbContext = dbContext;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _zaloOAuthSettings = zaloOAuthSettings.Value;
        }


        /// Lưu hoặc cập nhật token Zalo vào database.
        public async Task SaveTokensAsync(string accessToken, string refreshToken, long expiresInSeconds)
        {
            var expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);

            var existingToken = await _dbContext.ZaloTokens.FirstOrDefaultAsync();

            if (existingToken == null)
            {
                var newToken = new ZaloToken
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.ZaloTokens.Add(newToken);
            }
            else
            {
                existingToken.AccessToken = accessToken;
                existingToken.RefreshToken = refreshToken;
                existingToken.ExpiresAt = expiresAt;
                existingToken.UpdatedAt = DateTime.UtcNow;
                _dbContext.ZaloTokens.Update(existingToken);
            }

            await _dbContext.SaveChangesAsync();
        }


        public async Task<string?> GetCurrentAccessTokenAsync()
        {
            var token = await GetStoredTokensAsync();
            if (token == null)
            {
                _logger.LogWarning("❌ Không tìm thấy Zalo token trong database.");
                return null;
            }

            var timeRemaining = token.ExpiresAt - DateTime.UtcNow;
            if (timeRemaining.TotalMinutes < 5)
            {
                _logger.LogInformation("🔁 Token sắp hết hạn, thực hiện làm mới...");
                await RefreshTokensAsync(token.RefreshToken);
                token = await GetStoredTokensAsync();
            }

            return token?.AccessToken;
        }

        public async Task RefreshTokensAsync(string? refreshToken = null)
        {
            refreshToken ??= (await GetStoredTokensAsync())?.RefreshToken;

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("❌ Không có refresh token để làm mới access token.");
                return;
            }

            var requestUrl = "https://oauth.zaloapp.com/v4/oa/access_token";
            var requestBody = new
            {
                app_id = _zaloOAuthSettings.AppId,
                grant_type = "refresh_token",
                refresh_token = refreshToken,
                code = ""
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(requestUrl, requestBody);
                var tokenResponse = await response.Content.ReadFromJsonAsync<ZaloTokenResponse>();

                if (!response.IsSuccessStatusCode || tokenResponse == null || tokenResponse.Error != 0)
                {
                    _logger.LogError("❌ Làm mới token thất bại. StatusCode={StatusCode}, Error={Error}, Message={Message}",
                        response.StatusCode, tokenResponse?.Error, tokenResponse?.Message);
                    return;
                }

                await SaveTokensAsync(
                    tokenResponse.AccessToken!,
                    tokenResponse.RefreshToken!,
                    tokenResponse.ExpiresIn
                );

                _logger.LogInformation("✅ Làm mới access token thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi gửi yêu cầu làm mới token.");
            }
        }


        // Lấy token Zalo hiện tại từ database.
        public async Task<ZaloToken?> GetStoredTokensAsync()
        {
            return await _dbContext.ZaloTokens.FirstOrDefaultAsync();
        }


    }
}
