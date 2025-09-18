// using System.Net.Http.Headers;
// using System.Text.Json;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;

// namespace SigmaNotificationBackend.Services
// {
//     public interface IZaloSendService
//     {
//         Task SendMessageToUserAsync(string userId, string message);
//         Task SendDepartmentSelectionTemplateAsync(string userId);
//         Task HandleDepartmentSelectionAsync(string userId, string messageText);
//         Task SendCombinedProductionMessageTemplateAsync(string userId, List<dynamic> messages, DateTime sendTime, string? extraNote = null);
//         Task MarkMessageAsViewedByUserAsync(int messageId, string userId);
//     }

//     public class ZaloSendService : IZaloSendService
//     {
//         private readonly ITokenStorageService _tokenStorage;
//         private readonly ILogger<ZaloSendService> _logger;
//         private readonly AppDbContext _dbcontext;
//         private readonly HttpClient _httpClient;

//         private static readonly Dictionary<string, int> DepartmentRoles = new()
//         {
//             { "IT", 1 },
//             { "PMC", 2 },
//             { "PD", 3 },
//             { "Other", 4 },
//             { "QC", 5 }
//         };

//         public ZaloSendService(
//             ITokenStorageService tokenStorage,
//             ILogger<ZaloSendService> logger,
//             IHttpClientFactory httpClientFactory,
//             AppDbContext appDbContext
//             )
//         {
//             _tokenStorage = tokenStorage;
//             _logger = logger;
//             _httpClient = httpClientFactory.CreateClient();
//             _dbcontext = appDbContext;
//         }

//         public async Task SendMessageToUserAsync(string userId, string message)
//         {
//             var token = await _tokenStorage.GetCurrentAccessTokenAsync();
//             if (string.IsNullOrEmpty(token))
//             {
//                 return;
//             }

//             var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

//             var payload = new
//             {
//                 recipient = new { user_id = userId },
//                 message = new { text = message }
//             };

//             var payloadJson = JsonSerializer.Serialize(payload);

//             var request = new HttpRequestMessage(HttpMethod.Post, url)
//             {
//                 Content = JsonContent.Create(payload)
//             };
//             request.Headers.Add("access_token", token);

//             var response = await _httpClient.SendAsync(request);
//             var result = await response.Content.ReadAsStringAsync();

//             if (!response.IsSuccessStatusCode)
//             {
//                 _logger.LogError("Failed to send message: {Result}", result);

//             }
//             else
//             {
//                 _logger.LogInformation("Message sent successfully to user {UserId}", userId);
//             }
//         }


//         // public async Task SendCombinedProductionMessageTemplateAsync(string userId, List<dynamic> messages, DateTime sendTime)
//         // {
//         //     var token = await _tokenStorage.GetCurrentAccessTokenAsync();
//         //     if (string.IsNullOrEmpty(token)) return;

//         //     string GetShiftTimeLabel(DateTime time)
//         //     {
//         //         var vn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
//         //         var t = TimeZoneInfo.ConvertTimeFromUtc(time.ToUniversalTime(), vn);
//         //         var hour = t.Hour;
//         //         var minute = t.Minute;

//         //         if (hour == 10 && minute >= 10 && minute <= 20) return "08h00 - 10h10";  // Vào lúc 10:15 gửi → ca 08h00-10h10
//         //         if (hour == 13 && minute >= 0 && minute <= 10) return "11h00 - 13h00";   // Vào lúc 13:05 gửi → ca 11h00-13h00  
//         //         if (hour == 15 && minute >= 10 && minute <= 20) return "13h00 - 15h10";  // Vào lúc 15:15 gửi → ca 13h00-15h10
//         //         if (hour == 18 && minute >= 5 && minute <= 15) return "15h10 - 18h00";   // Vào lúc 18:10 gửi → ca 15h10-18h00
//         //         if (hour == 22 && minute >= 25 && minute <= 35) return "18h00 - 20h00";  // Vào lúc 22:39 gửi → ca 18h00-20h00

//         //         return "uknown";
//         //     }

//         //     string FormatContent(List<dynamic> groupedMessages)
//         //     {
//         //         var groupedByOperation = groupedMessages
//         //             .GroupBy(m => m.operation)
//         //             .ToList();

//         //         var sb = new System.Text.StringBuilder();

//         //         sb.AppendLine($"🔔 Time: {GetShiftTimeLabel(sendTime)}");


//         //         foreach (var operationGroup in groupedByOperation)
//         //         {
//         //             var op = operationGroup.Key?.ToString()?.Trim() ?? "Unknown";
//         //             sb.AppendLine(FormatOperationLine(op, 25));


//         //             foreach (var msg in operationGroup)
//         //             {
//         //                 string content = msg.content.ToString()
//         //                     .Replace("bg-danger", "❌")
//         //                     .Replace("bg-warning", "⚠️")
//         //                     .Replace("[", "")
//         //                     .Replace("]", "")
//         //                     .Replace(";", " -");

//         //                 sb.AppendLine(content.Trim());
//         //                 sb.AppendLine();
//         //             }
//         //         }

//         //         return sb.ToString().Trim();


//         //         string FormatOperationLine(string operation, int totalLength = 26)
//         //         {
//         //             operation = operation.Trim();
//         //             string leftEquals = new string('=', 3);
//         //             int remaining = totalLength - leftEquals.Length - operation.Length;

//         //             string rightEquals = remaining > 0 ? new string('=', remaining) : "";
//         //             return $"{leftEquals}{operation}{rightEquals}";
//         //         }
//         //     }

//         //     var messageText = FormatContent(messages);

//         //     var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

//         //     var messageId = messages.FirstOrDefault()?.id_message?.ToString() ?? "unknown";

//         //     var payload = new
//         //     {
//         //         recipient = new { user_id = userId },
//         //         message = new
//         //         {
//         //             text = messageText,
//         //             attachment = new
//         //             {
//         //                 type = "template",
//         //                 payload = new
//         //                 {
//         //                     template_type = "text",
//         //                     buttons = new[]
//         //         {
//         //             new { title = "Đã xem", type = "oa.query.show", payload = $"Đã xem tin nhắn: {messageId}" }
//         //         }
//         //                 }
//         //             }
//         //         }
//         //     };


//         //     var request = new HttpRequestMessage(HttpMethod.Post, url)
//         //     {
//         //         Content = JsonContent.Create(payload)
//         //     };

//         //     request.Headers.Add("access_token", token);
//         //     var response = await _httpClient.SendAsync(request);
//         //     var result = await response.Content.ReadAsStringAsync();

//         //     if (!response.IsSuccessStatusCode)
//         //     {
//         //         _logger.LogError("❌ Gửi tin nhắn thất bạn : {Result}", result);
//         //     }
//         //     else
//         //     {
//         //         _logger.LogInformation("✅ Gửi tin nhắn thành công tới {UserId}", userId);
//         //     }
//         // }

//         public async Task SendCombinedProductionMessageTemplateAsync(
//     string userId,
//     List<dynamic> messages,
//     DateTime sendTime,
//     string? extraNote = null) // 🔥 thêm tham số optional
//         {
//             var token = await _tokenStorage.GetCurrentAccessTokenAsync();
//             if (string.IsNullOrEmpty(token)) return;

//             string GetShiftTimeLabel(DateTime time)
//             {
//                 var vn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
//                 var t = TimeZoneInfo.ConvertTimeFromUtc(time.ToUniversalTime(), vn);
//                 var hour = t.Hour;
//                 var minute = t.Minute;

//                 if (hour == 10 && minute >= 10 && minute <= 20) return "08h00 - 10h10";
//                 if (hour == 13 && minute >= 0 && minute <= 10) return "11h00 - 13h00";
//                 if (hour == 15 && minute >= 10 && minute <= 20) return "13h00 - 15h10";
//                 if (hour == 18 && minute >= 5 && minute <= 15) return "15h10 - 18h00";
//                 if (hour == 22 && minute >= 25 && minute <= 35) return "18h00 - 20h00";

//                 return "unknown";
//             }

//             string FormatContent(List<dynamic> groupedMessages)
//             {
//                 var groupedByOperation = groupedMessages
//                     .GroupBy(m => m.operation)
//                     .ToList();

//                 var sb = new System.Text.StringBuilder();

//                 // 🔥 nếu có extraNote (dùng cho escalation) thì chèn vào đầu message
//                 if (!string.IsNullOrEmpty(extraNote))
//                 {
//                     sb.AppendLine(extraNote);
//                     sb.AppendLine("----------");
//                 }

//                 sb.AppendLine($"🔔 Time: {GetShiftTimeLabel(sendTime)}");

//                 foreach (var operationGroup in groupedByOperation)
//                 {
//                     var op = operationGroup.Key?.ToString()?.Trim() ?? "Unknown";
//                     sb.AppendLine(FormatOperationLine(op, 25));

//                     foreach (var msg in operationGroup)
//                     {
//                         string content = msg.content.ToString()
//                             .Replace("bg-danger", "❌")
//                             .Replace("bg-warning", "⚠️")
//                             .Replace("[", "")
//                             .Replace("]", "")
//                             .Replace(";", " -");

//                         sb.AppendLine(content.Trim());
//                         sb.AppendLine();
//                     }
//                 }

//                 return sb.ToString().Trim();

//                 string FormatOperationLine(string operation, int totalLength = 26)
//                 {
//                     operation = operation.Trim();
//                     string leftEquals = new string('=', 3);
//                     int remaining = totalLength - leftEquals.Length - operation.Length;

//                     string rightEquals = remaining > 0 ? new string('=', remaining) : "";
//                     return $"{leftEquals}{operation}{rightEquals}";
//                 }
//             }

//             var messageText = FormatContent(messages);

//             var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

//             var messageId = messages.FirstOrDefault()?.id_message?.ToString() ?? "unknown";

//             var payload = new
//             {
//                 recipient = new { user_id = userId },
//                 message = new
//                 {
//                     text = messageText,
//                     attachment = new
//                     {
//                         type = "template",
//                         payload = new
//                         {
//                             template_type = "text",
//                             buttons = new[]
//                             {
//                         new { title = "Đã xem", type = "oa.query.show", payload = $"Đã xem tin nhắn: {messageId}" }
//                     }
//                         }
//                     }
//                 }
//             };

//             var request = new HttpRequestMessage(HttpMethod.Post, url)
//             {
//                 Content = JsonContent.Create(payload)
//             };

//             request.Headers.Add("access_token", token);
//             var response = await _httpClient.SendAsync(request);
//             var result = await response.Content.ReadAsStringAsync();

//             if (!response.IsSuccessStatusCode)
//             {
//                 _logger.LogError("❌ Gửi tin nhắn thất bại : {Result}", result);
//             }
//             else
//             {
//                 _logger.LogInformation("✅ Gửi tin nhắn thành công tới {UserId}", userId);
//             }
//         }



//         public async Task SendDepartmentSelectionTemplateAsync(string userId)
//         {
//             var token = await _tokenStorage.GetCurrentAccessTokenAsync();
//             if (token == null || string.IsNullOrEmpty(token))
//             {
//                 return;
//             }

//             var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

//             var payloadJson = @"
//             {
//                 ""recipient"": {
//                     ""user_id"": """ + userId + @"""
//                 },
//                 ""message"": {
//                     ""text"": ""Bạn ở bộ phận nào?"",
//                     ""attachment"": {
//                         ""type"": ""template"",
//                         ""payload"": {
//                             ""template_type"": ""text"",
//                             ""buttons"": [
//                                 { ""title"": ""QC"", ""type"": ""oa.query.show"", ""payload"": ""department: QC"" },
//                                 { ""title"": ""PD"", ""type"": ""oa.query.show"", ""payload"": ""department: PD"" },
//                                 { ""title"": ""PMC"", ""type"": ""oa.query.show"", ""payload"": ""department: PMC"" },
//                                 { ""title"": ""IT"", ""type"": ""oa.query.show"", ""payload"": ""department: IT"" },
//                                 { ""title"": ""Other"", ""type"": ""oa.query.show"", ""payload"": ""department: Other"" }
//                             ]
//                         }
//                     }
//                 }
//             }";

//             var request = new HttpRequestMessage(HttpMethod.Post, url)
//             {
//                 Content = new StringContent(payloadJson, System.Text.Encoding.UTF8, "application/json")
//             };

//             request.Headers.Add("access_token", token);

//             var response = await _httpClient.SendAsync(request);
//             var result = await response.Content.ReadAsStringAsync();

//             if (!response.IsSuccessStatusCode)
//             {
//                 _logger.LogError("Lỗi gửi tin nhắn mẫu chọn phòng ban: {Result}", result);
//             }
//         }



//         public async Task HandleDepartmentSelectionAsync(string userId, string messageText)
//         {
//             if (!messageText.StartsWith("department:", StringComparison.OrdinalIgnoreCase))
//                 return;

//             var parts = messageText.Split(':', 2);
//             if (parts.Length != 2)
//                 return;

//             var department = parts[1].Trim().ToUpper();

//             if (!DepartmentRoles.TryGetValue(department, out var role))
//                 return;

//             var follower = await _dbcontext.Followers.FirstOrDefaultAsync(f => f.UserId == userId);

//             if (follower != null)
//             {
//                 if (follower.Role != 0)
//                 {
//                     await SendMessageToUserAsync(userId, $"Bạn đã chọn phòng ban rồi 🔒. Vui lòng liên hệ quản trị viên nếu muốn thay đổi.");
//                     return;
//                 }
//                 follower.Role = role;
//                 await _dbcontext.SaveChangesAsync();
//                 _logger.LogInformation("✅ Gán role {Role} cho user {UserId} thành công", role, userId);
//             }
//         }


//         // public async Task MarkMessageAsViewedByUserAsync(int messageId, string userId)
//         // {
//         //     var message = await _dbcontext.SVN_Messages.FirstOrDefaultAsync(m => m.id_message == messageId);
//         //     if (message != null)
//         //     {
//         //         var currentUserIds = message.UserId?.Split(';', StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>();

//         //         if (!currentUserIds.Contains(userId))
//         //         {
//         //             currentUserIds.Add(userId);
//         //             message.UserId = string.Join("; ", currentUserIds);
//         //             await _dbcontext.SaveChangesAsync();
//         //         }
//         //     }
//         // }

//         public async Task MarkMessageAsViewedByUserAsync(int messageId, string userId)
//         {
//             var message = await _dbcontext.SVN_Messages.FindAsync(messageId);
//             if (message == null) return;

//             // 1) Append userId vào danh sách UserId (giữ nguyên logic cũ)
//             if (string.IsNullOrEmpty(message.UserId))
//                 message.UserId = userId;
//             else if (!message.UserId.Split(';').Contains(userId))
//                 message.UserId += ";" + userId;

//             // 🔥 2) Lấy thông tin follower để biết RoleDetail
//             var follower = await _dbcontext.Followers.FirstOrDefaultAsync(f => f.UserId == userId);
//             if (follower != null && !string.IsNullOrEmpty(follower.RoleDetail))
//             {
//                 // Chuẩn hóa roleDetail về lowercase để tránh lỗi
//                 var roleDetail = follower.RoleDetail.Trim().ToLower();

//                 // 🔥 3) Gán vào cột tương ứng nếu chưa có ai ở cấp đó xem
//                 if (roleDetail == "operator" && string.IsNullOrEmpty(message.isOperatorSeen))
//                 {
//                     message.isOperatorSeen = userId;
//                 }
//                 else if (roleDetail == "supervisor" && string.IsNullOrEmpty(message.isSupervisorSeen))
//                 {
//                     message.isSupervisorSeen = userId;
//                 }
//                 else if (roleDetail == "manager" && string.IsNullOrEmpty(message.isManagerSeen))
//                 {
//                     message.isManagerSeen = userId;
//                 }
//             }

//             // 🔥 4) Lưu DB
//             await _dbcontext.SaveChangesAsync();
//         }
//     }
// }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SigmaNotificationBackend.Data;          // 👈 thêm
using SigmaNotificationBackend.Models;        // 👈 thêm

namespace SigmaNotificationBackend.Services
{
    public interface IZaloSendService
    {
        Task SendMessageToUserAsync(string userId, string message);
        Task SendDepartmentSelectionTemplateAsync(string userId);
        Task HandleDepartmentSelectionAsync(string userId, string messageText);

        // Giữ nguyên chữ ký cũ (dùng khi gửi tin gộp)
        Task SendCombinedProductionMessageTemplateAsync(string userId, List<dynamic> messages, DateTime sendTime, string? extraNote = null);

        // ✅ NEW: update nhiều messageId cùng lúc khi người dùng bấm “Đã xem”
        Task MarkMessagesAsViewedByUserAsync(IEnumerable<int> messageIds, string userId);

        // Back-compat: nếu chỗ nào trong code gọi single-id vẫn hoạt động
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
            if (string.IsNullOrEmpty(token)) return;

            var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

            var payload = new
            {
                recipient = new { user_id = userId },
                message = new { text = message }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("access_token", token);

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _logger.LogError("Failed to send message: {Result}", result);
            else
                _logger.LogInformation("Message sent successfully to user {UserId}", userId);
        }

        public async Task SendCombinedProductionMessageTemplateAsync(
            string userId,
            List<dynamic> messages,
            DateTime sendTime,
            string? extraNote = null)
        {
            var token = await _tokenStorage.GetCurrentAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return;

            string GetShiftTimeLabel(DateTime time)
            {
                var vn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var t = TimeZoneInfo.ConvertTimeFromUtc(time.ToUniversalTime(), vn);
                var hour = t.Hour;
                var minute = t.Minute;

                if (hour == 10 && minute >= 10 && minute <= 20) return "08h00 - 10h10";
                if (hour == 13 && minute >= 0 && minute <= 10) return "11h00 - 13h00";
                if (hour == 15 && minute >= 10 && minute <= 20) return "13h00 - 15h10";
                if (hour == 18 && minute >= 5 && minute <= 15) return "15h10 - 18h00";
                if (hour == 22 && minute >= 25 && minute <= 35) return "18h00 - 20h00";
                return "unknown";
            }

            string FormatContent(List<dynamic> groupedMessages)
            {
                var groupedByOperation = groupedMessages.GroupBy(m => m.operation).ToList();
                var sb = new System.Text.StringBuilder();

                if (!string.IsNullOrEmpty(extraNote))
                {
                    sb.AppendLine(extraNote);
                    sb.AppendLine("----------");
                }

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

            // ✅ Lấy toàn bộ id_message trong nhóm → payload nút “Đã xem” chứa danh sách id
            var idList = messages
                .Select(m =>
                {
                    try { return (int?)m.id_message; }
                    catch { return null; }
                })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var idPayload = idList.Count > 0 ? string.Join(",", idList) : "unknown";

            var url = "https://openapi.zalo.me/v3.0/oa/message/cs";
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
                                new { title = "Đã xem", type = "oa.query.show", payload = $"Đã xem tin nhắn: {idPayload}" }
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
                _logger.LogError("❌ Gửi tin nhắn thất bại : {Result}", result);
            else
                _logger.LogInformation("✅ Gửi tin nhắn thành công tới {UserId}", userId);
        }

        public async Task SendDepartmentSelectionTemplateAsync(string userId)
        {
            var token = await _tokenStorage.GetCurrentAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return;

            var url = "https://openapi.zalo.me/v3.0/oa/message/cs";

            var payloadJson = @"
            {
                ""recipient"": { ""user_id"": """ + userId + @""" },
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
                _logger.LogError("Lỗi gửi tin nhắn mẫu chọn phòng ban: {Result}", result);
        }

        public async Task HandleDepartmentSelectionAsync(string userId, string messageText)
        {
            if (!messageText.StartsWith("department:", StringComparison.OrdinalIgnoreCase))
                return;

            var parts = messageText.Split(':', 2);
            if (parts.Length != 2) return;

            var department = parts[1].Trim().ToUpper();
            if (!DepartmentRoles.TryGetValue(department, out var role)) return;

            var follower = await _dbcontext.Followers.FirstOrDefaultAsync(f => f.UserId == userId);
            if (follower != null)
            {
                if (follower.Role != 0)
                {
                    await SendMessageToUserAsync(userId, "Bạn đã chọn phòng ban rồi 🔒. Vui lòng liên hệ quản trị viên nếu muốn thay đổi.");
                    return;
                }
                follower.Role = role;
                await _dbcontext.SaveChangesAsync();
                _logger.LogInformation("✅ Gán role {Role} cho user {UserId} thành công", role, userId);
            }
        }

        // ✅ NEW: update hàng loạt theo danh sách ID
        public async Task MarkMessagesAsViewedByUserAsync(IEnumerable<int> messageIds, string userId)
        {
            var ids = messageIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0) return;

            var follower = await _dbcontext.Followers.AsNoTracking().FirstOrDefaultAsync(f => f.UserId == userId);
            var role = (follower?.RoleDetail ?? "").Trim().ToLower();

            var msgs = await _dbcontext.SVN_Messages.Where(m => ids.Contains(m.id_message)).ToListAsync();

            foreach (var msg in msgs)
            {
                // append UserId
                if (string.IsNullOrWhiteSpace(msg.UserId))
                    msg.UserId = userId;
                else if (!msg.UserId.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Contains(userId))
                    msg.UserId = msg.UserId + ";" + userId;

                // set cờ đã xem theo cấp (nếu trống)
                if (role == "operator" && string.IsNullOrWhiteSpace(msg.isOperatorSeen))
                    msg.isOperatorSeen = userId;
                else if (role == "supervisor" && string.IsNullOrWhiteSpace(msg.isSupervisorSeen))
                    msg.isSupervisorSeen = userId;
                else if (role == "manager" && string.IsNullOrWhiteSpace(msg.isManagerSeen))
                    msg.isManagerSeen = userId;
            }

            await _dbcontext.SaveChangesAsync();
        }

        // Back-compat: hàm single-id gọi sang bulk
        public async Task MarkMessageAsViewedByUserAsync(int messageId, string userId)
            => await MarkMessagesAsViewedByUserAsync(new[] { messageId }, userId);
    }
}
