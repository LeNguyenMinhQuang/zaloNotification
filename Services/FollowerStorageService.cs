using Microsoft.EntityFrameworkCore; // Nếu dùng EF Core
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SigmaNotificationBackend.Models;
using System.Text.Json;

namespace SigmaNotificationBackend.Services
{
    public class FollowerStorageService : IFollowerStorageService
    {
        private readonly AppDbContext _context;

        private readonly IZaloSendService _zaloSendService;

        private readonly HttpClient _httpClient;

        private readonly ITokenStorageService _tokenStorageService;

        public FollowerStorageService(AppDbContext context, IZaloSendService zaloSendService, HttpClient httpClient, ITokenStorageService tokenStorageService)
        {
            _context = context;
            _zaloSendService = zaloSendService;
            _httpClient = httpClient;
            _tokenStorageService = tokenStorageService;
        }



        public async Task AddFollowerAsync(string userId)
        {
            if (!await _context.Followers.AnyAsync(f => f.UserId == userId))
            {
                var follower = new Follower
                {
                    UserId = userId,
                    FollowDate = DateTime.UtcNow,
                    Name = "",
                    Avatar = ""
                };


                var (name, avatar) = await GetZaloUserInfoAsync(userId);
                if (!string.IsNullOrEmpty(name)) follower.Name = name;
                if (!string.IsNullOrEmpty(avatar)) follower.Avatar = avatar;

                _context.Followers.Add(follower);
                await _context.SaveChangesAsync();

                Console.WriteLine($"📝 Lưu follower: {userId} | Name: {follower.Name} | Avatar: {follower.Avatar}");

                await _zaloSendService.SendDepartmentSelectionTemplateAsync(userId);
            }
        }



        public async Task RemoveFollowerAsync(string userId)
        {
            var follower = await _context.Followers.FirstOrDefaultAsync(f => f.UserId == userId);
            if (follower != null)
            {
                _context.Followers.Remove(follower);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Đã xóa follower: {userId}");
            }
            else
            {
                Console.WriteLine($"Không tìm thấy follower: {userId}");
            }
        }

        public async Task<List<string>> GetAllFollowerUserIdsAsync()
        {
            return await _context.Followers.Select(f => f.UserId).ToListAsync();
        }

        public async Task<bool> IsFollowerAsync(string userId)
        {
            return await _context.Followers.AnyAsync(f => f.UserId == userId);
        }



        private async Task<(string? Name, string? Avatar)> GetZaloUserInfoAsync(string userId)
        {
            try
            {
                string accessToken = await _tokenStorageService.GetCurrentAccessTokenAsync();

                var url = $"https://openapi.zalo.me/v3.0/oa/user/detail?data={{\"user_id\":\"{userId}\"}}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("access_token", accessToken);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📦 Zalo Response:\n{content}");

                if (response.IsSuccessStatusCode)
                {
                    using var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;

                    var error = root.GetProperty("error").GetInt32();
                    if (error == 0 && root.TryGetProperty("data", out var data))
                    {
                        string? name = null;
                        string? avatar = null;

                        if (data.TryGetProperty("display_name", out var nameProp))
                            name = nameProp.GetString();

                        if (data.TryGetProperty("avatar", out var avatarProp))
                            avatar = avatarProp.GetString();

                        return (name, avatar);
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }

            return (null, null);
        }

    }

}