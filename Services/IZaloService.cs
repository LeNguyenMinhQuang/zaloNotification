using System;
using System.Threading.Tasks;
using SigmaNotificationBackend.Models;

namespace SigmaNotificationBackend.Services
{
    public interface IZaloService
    {

        // Lấy access token hiện tại, nếu sắp hết hạn sẽ tự động làm mới
        // Task<string?> GetCurrentAccessTokenAsync();

        // // Hàm để làm mới access_token và refresh_token
        // // currentRefreshToken có thể là null, service sẽ tự tìm từ DB
        // Task RefreshTokensAsync(string? currentRefreshToken = null);


        // // Thêm phương thức để gửi tin nhắn text
        // Task<bool> SendTextMessageAsync(string recipientId, string messageText);


    }
}