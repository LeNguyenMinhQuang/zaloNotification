using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SigmaNotificationBackend.Services
{
    public interface IZaloSendService
    {
        Task SendMessageToUserAsync(string userId, string message);
        Task SendDepartmentSelectionTemplateAsync(string userId);
        Task HandleDepartmentSelectionAsync(string userId, string messageText);
        Task SendCombinedProductionMessageTemplateAsync(string userId, List<dynamic> messages, DateTime sendTime);
        Task MarkMessageAsViewedByUserAsync(int messageId, string userId);
    }

    public class ZaloSendService : IZaloSendService
    {
        private readonly ITokenStorageService _tokenStorage;
        private readonly ILogger<ZaloSendService> _logger;
        private readonly AppDbContext _dbcontext;
        private readonly HttpClient _httpClient;

        private static readonly Dictionary<string, int> DepartmentRoles = new()
        {
            { "IT", 1 },
            { "PMC", 2 },
            { "PD", 3 },
            { "Other", 4 },
            { "QC", 5 }
        };

        public ZaloSendService(
            ITokenStorageService tokenStorage,
            ILogger<ZaloSendService> logger,
            IHttpClientFactory httpClientFactory,
            AppDbContext appDbContext
            )
        {
            _tokenStorage = tokenStorage;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _dbcontext = appDbContext;
        }

        public async Task SendMessageToUserAsync(string userId, string message)
        {
            var token = await _tokenStorage.GetCurrentAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

            var payload = new
            {
                recipient = new { user_id = userId },
                message = new { text = message }
            };

            var payloadJson = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("access_token", token);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to send message: {Result}", result);

            }
            else
            {
                _logger.LogInformation("Message sent successfully to user {UserId}", userId);
            }
        }


        public async Task SendCombinedProductionMessageTemplateAsync(string userId, List<dynamic> messages, DateTime sendTime)
        {
            var token = await _tokenStorage.GetCurrentAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return;

            string GetShiftTimeLabel(DateTime time)
            {
                var vn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var t = TimeZoneInfo.ConvertTimeFromUtc(time.ToUniversalTime(), vn);
                var hour = t.Hour;
                var minute = t.Minute;

                if (hour == 10 && minute >= 10 && minute <= 20) return "08h00 - 10h10";  // Vào lúc 10:15 gửi → ca 08h00-10h10
                if (hour == 13 && minute >= 0 && minute <= 10) return "11h00 - 13h00";   // Vào lúc 13:05 gửi → ca 11h00-13h00  
                if (hour == 15 && minute >= 10 && minute <= 20) return "13h00 - 15h10";  // Vào lúc 15:15 gửi → ca 13h00-15h10
                if (hour == 18 && minute >= 5 && minute <= 15) return "15h10 - 18h00";   // Vào lúc 18:10 gửi → ca 15h10-18h00
                if (hour == 22 && minute >= 25 && minute <= 35) return "18h00 - 20h00";  // Vào lúc 22:39 gửi → ca 18h00-20h00

                return "uknown";
            }

            string FormatContent(List<dynamic> groupedMessages)
            {
                var groupedByOperation = groupedMessages
                    .GroupBy(m => m.operation)
                    .ToList();

                var sb = new System.Text.StringBuilder();

                sb.AppendLine($"🔔 Time: {GetShiftTimeLabel(sendTime)}");


                foreach (var operationGroup in groupedByOperation)
                {
                    var op = operationGroup.Key?.ToString()?.Trim() ?? "Unknown";
                    sb.AppendLine(FormatOperationLine(op, 25));


                    foreach (var msg in operationGroup)
                    {
                        string content = msg.content.ToString()
                            .Replace("bg-danger", "❌")
                            .Replace("bg-warning", "⚠️")
                            .Replace("[", "")
                            .Replace("]", "")
                            .Replace(";", " -");

                        sb.AppendLine(content.Trim());
                        sb.AppendLine();
                    }
                }

                return sb.ToString().Trim();


                string FormatOperationLine(string operation, int totalLength = 26)
                {
                    operation = operation.Trim();
                    string leftEquals = new string('=', 3);
                    int remaining = totalLength - leftEquals.Length - operation.Length;

                    string rightEquals = remaining > 0 ? new string('=', remaining) : "";
                    return $"{leftEquals}{operation}{rightEquals}";
                }
            }

            var messageText = FormatContent(messages);

            var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

            var messageId = messages.FirstOrDefault()?.id_message?.ToString() ?? "unknown";

            var payload = new
            {
                recipient = new { user_id = userId },
                message = new
                {
                    text = messageText,
                    attachment = new
                    {
                        type = "template",
                        payload = new
                        {
                            template_type = "text",
                            buttons = new[]
                {
                    new { title = "Đã xem", type = "oa.query.show", payload = $"Đã xem tin nhắn: {messageId}" }
                }
                        }
                    }
                }
            };


            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Add("access_token", token);
            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Gửi tin nhắn thất bạn : {Result}", result);
            }
            else
            {
                _logger.LogInformation("✅ Gửi tin nhắn thành công tới {UserId}", userId);
            }
        }


        public async Task SendDepartmentSelectionTemplateAsync(string userId)
        {
            var token = await _tokenStorage.GetCurrentAccessTokenAsync();
            if (token == null || string.IsNullOrEmpty(token))
            {
                return;
            }

            var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

            var payloadJson = @"
            {
                ""recipient"": {
                    ""user_id"": """ + userId + @"""
                },
                ""message"": {
                    ""text"": ""Bạn ở bộ phận nào?"",
                    ""attachment"": {
                        ""type"": ""template"",
                        ""payload"": {
                            ""template_type"": ""text"",
                            ""buttons"": [
                                { ""title"": ""QC"", ""type"": ""oa.query.show"", ""payload"": ""department: QC"" },
                                { ""title"": ""PD"", ""type"": ""oa.query.show"", ""payload"": ""department: PD"" },
                                { ""title"": ""PMC"", ""type"": ""oa.query.show"", ""payload"": ""department: PMC"" },
                                { ""title"": ""IT"", ""type"": ""oa.query.show"", ""payload"": ""department: IT"" },
                                { ""title"": ""Other"", ""type"": ""oa.query.show"", ""payload"": ""department: Other"" }
                            ]
                        }
                    }
                }
            }";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json")
            };

            request.Headers.Add("access_token", token);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Lỗi gửi tin nhắn mẫu chọn phòng ban: {Result}", result);
            }
        }



        public async Task HandleDepartmentSelectionAsync(string userId, string messageText)
        {
            if (!messageText.StartsWith("department:", StringComparison.OrdinalIgnoreCase))
                return;

            var parts = messageText.Split(':', 2);
            if (parts.Length != 2)
                return;

            var department = parts[1].Trim().ToUpper();

            if (!DepartmentRoles.TryGetValue(department, out var role))
                return;

            var follower = await _dbcontext.Followers.FirstOrDefaultAsync(f => f.UserId == userId);

            if (follower != null)
            {
                if (follower.Role != 0)
                {
                    await SendMessageToUserAsync(userId, $"Bạn đã chọn phòng ban rồi 🔒. Vui lòng liên hệ quản trị viên nếu muốn thay đổi.");
                    return;
                }
                follower.Role = role;
                await _dbcontext.SaveChangesAsync();
                _logger.LogInformation("✅ Gán role {Role} cho user {UserId} thành công", role, userId);
            }
        }


        public async Task MarkMessageAsViewedByUserAsync(int messageId, string userId)
        {
            var message = await _dbcontext.SVN_Messages.FirstOrDefaultAsync(m => m.id_message == messageId);
            if (message != null)
            {
                var currentUserIds = message.UserId?.Split(';', StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>();

                if (!currentUserIds.Contains(userId))
                {
                    currentUserIds.Add(userId);
                    message.UserId = string.Join("; ", currentUserIds);
                    await _dbcontext.SaveChangesAsync();
                }
            }
        }
    }
}