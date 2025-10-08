// Đường dẫn: SigmaNotificationBackend/Models/ZaloTokenResponse.cs
using System.Text.Json.Serialization;

namespace SigmaNotificationBackend.Models
{
    public class ZaloTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        // Dạng chuỗi vì có thể là "90000", nên cần parse về long nếu sử dụng số
        [JsonPropertyName("expires_in")]
        public string? ExpiresInRaw { get; set; }

        // Chuyển đổi thủ công sang long khi cần
        [JsonIgnore]
        public long ExpiresIn
        {
            get
            {
                return long.TryParse(ExpiresInRaw, out var value) ? value : 0;
            }
        }


        [JsonPropertyName("error")]
        public int Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
