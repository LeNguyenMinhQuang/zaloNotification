// File này dùng để định nghĩa cấu trúc cài đặt Zalo OAuth mà bạn sẽ đọc từ appsettings.json.

namespace SigmaNotificationBackend.Models
{
    public class ZaloOAuthSettings
    {
        public string AppId { get; set; } = string.Empty; // Sẽ đọc từ appsettings.json
        public string SecretKey { get; set; } = string.Empty; // Sẽ đọc từ appsettings.json
        public string RedirectUri { get; set; } = string.Empty; // Sẽ đọc từ appsettings.json
    }
}
