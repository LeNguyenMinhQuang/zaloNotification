using System.Text.Json.Serialization;

namespace SigmaNotificationBackend.Models
{
    public class ZaloWebhookEvent
    {
        [JsonPropertyName("app_id")]
        public string? AppId { get; set; }

        [JsonPropertyName("oa_id")]
        public string? OaId { get; set; }

        // Trường user_id_by_app ở cấp root (có thể có cho một số loại event như 'follow')
        [JsonPropertyName("user_id_by_app")]
        public string? UserIdByApp { get; set; }

        [JsonPropertyName("event_name")]
        public string? EventName { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("follower")]
        public FollowerWebhookInfo? Follower { get; set; }

        [JsonPropertyName("sender")]
        public ZaloWebhookParticipant? Sender { get; set; }

        [JsonPropertyName("recipient")]
        public ZaloWebhookParticipant? Recipient { get; set; }

        [JsonPropertyName("message")]
        public ZaloWebhookMessage? Message { get; set; }
    }

    public class FollowerWebhookInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; } // Đây thường là user_id_by_app
    }

    public class ZaloWebhookParticipant
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class ZaloWebhookMessage
    {
        [JsonPropertyName("msg_id")]
        public string? MsgId { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // Ví dụ: "text", "image", "link"
    }

    public class ZaloApiResponse
    {
        [JsonPropertyName("error")]
        public int Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

}