// Đường dẫn: SigmaNotificationBackend/Controllers/ZaloController.cs
using System;                    // 👈 thêm
using System.Collections.Generic;
using System.Linq;               // 👈 thêm
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SigmaNotificationBackend.Models;
using SigmaNotificationBackend.Services;

namespace SigmaNotificationBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZaloController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ZaloOAuthSettings _settings;
        private readonly ILogger<ZaloController> _logger;
        private readonly ITokenStorageService _tokenStorageService;
        private readonly IZaloSendService _zaloSendService;
        private readonly IFollowerStorageService _followerStorageService;

        public ZaloController(HttpClient httpClient,
                              IConfiguration configuration,
                              ILogger<ZaloController> logger,
                              ITokenStorageService tokenStorageService,
                              IFollowerStorageService followerStorageService,
                              IZaloSendService zaloSendService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _tokenStorageService = tokenStorageService;
            _followerStorageService = followerStorageService;
            _zaloSendService = zaloSendService;

            _settings = new ZaloOAuthSettings();
            configuration.GetSection("ZaloOAuth").Bind(_settings);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> ZaloCallback([FromQuery] string code, [FromQuery] string oa_id, [FromQuery] int? error, [FromQuery] string? message)
        {
            if (error.HasValue && error != 0) return BadRequest($"Error from Zalo: {message ?? "Unknown error from Zalo authorization"}");
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(oa_id)) return BadRequest("Zalo callback is missing required parameters (code or oa_id).");

            ZaloTokenResponse? tokenResponse = null;
            try
            {
                tokenResponse = await ExchangeCodeForTokens(code);

                if (tokenResponse != null && tokenResponse.Error == 0)
                {
                    if (!string.IsNullOrEmpty(tokenResponse.AccessToken) && !string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    {
                        await _tokenStorageService.SaveTokensAsync(
                            tokenResponse.AccessToken,
                            tokenResponse.RefreshToken,
                            (int)Math.Min(tokenResponse.ExpiresIn, int.MaxValue));
                        return Ok("Cấp quyền Zalo thành công! Token đã được lưu vào database.");
                    }
                    else
                    {
                        return StatusCode(500, "Thất bại khi lưu token vào database. Vui lòng thử lại sau.");
                    }
                }
                else
                {
                    var errorMessage = tokenResponse?.Message ?? "Unknown error during token exchange.";
                    return StatusCode(500, $"Failed to exchange Zalo code for tokens: {errorMessage}");
                }
            }
            catch
            {
                return StatusCode(500, "An internal server error occurred during Zalo authorization.");
            }
        }

        private async Task<ZaloTokenResponse?> ExchangeCodeForTokens(string authCode)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("app_id", _settings.AppId),
                new KeyValuePair<string, string>("code", authCode),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.zaloapp.com/v4/oa/access_token")
            {
                Content = content
            };
            request.Headers.Add("secret_key", _settings.SecretKey);

            HttpResponseMessage? response = null;

            try
            {
                response = await _httpClient.SendAsync(request);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var tokenResponse = JsonSerializer.Deserialize<ZaloTokenResponse>(jsonResponse);
                if (!response.IsSuccessStatusCode || tokenResponse?.Error != 0) return tokenResponse;

                return tokenResponse;
            }
            catch
            {
                return new ZaloTokenResponse { Error = -1, Message = "Unexpected error during token exchange" };
            }
        }

        // Webhook OA
        [HttpPost("webhook")]
        public async Task<IActionResult> ZaloWebhook([FromBody] ZaloWebhookEvent webhookEvent)
        {
            _logger.LogInformation($"Received Zalo webhook event: {JsonSerializer.Serialize(webhookEvent)}");
            if (webhookEvent == null) return BadRequest("Invalid webhook event.");

            switch (webhookEvent.EventName)
            {
                case "follow":
                    {
                        var userId = webhookEvent.Follower?.Id;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            _logger.LogInformation("✅ Người quan tâm mới {UserId}", userId);
                            await _followerStorageService.AddFollowerAsync(userId);
                        }
                        break;
                    }

                case "unfollow":
                    {
                        var userId = webhookEvent.Follower?.Id;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            _logger.LogInformation("Người dùng đã bỏ quan tâm! User ID: {UserId}", userId);
                            await _followerStorageService.RemoveFollowerAsync(userId);
                        }
                        break;
                    }

                case "user_send_text":
                    {
                        var senderUserId = webhookEvent.Sender?.Id;
                        var receivedMessage = webhookEvent.Message?.Text;

                        if (!string.IsNullOrEmpty(senderUserId) && !string.IsNullOrEmpty(receivedMessage))
                        {
                            // Chọn phòng ban
                            if (receivedMessage.StartsWith("department:", StringComparison.OrdinalIgnoreCase))
                            {
                                await _zaloSendService.HandleDepartmentSelectionAsync(senderUserId, receivedMessage);
                            }
                            // ✅ Đánh dấu đã xem: hỗ trợ DANH SÁCH ID (vd: "Đã xem tin nhắn: 1,5,9")
                            else if (receivedMessage.StartsWith("Đã xem tin nhắn:", StringComparison.OrdinalIgnoreCase))
                            {
                                var idsText = receivedMessage.Substring(receivedMessage.IndexOf(':') + 1).Trim();
                                var idList = idsText
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => int.TryParse(s.Trim(), out var id) ? (int?)id : null)
                                    .Where(id => id.HasValue)
                                    .Select(id => id!.Value)
                                    .ToList();

                                if (idList.Count > 0)
                                {
                                    await _zaloSendService.MarkMessagesAsViewedByUserAsync(idList, senderUserId);
                                }
                            }
                        }
                        break;
                    }

                default:
                    _logger.LogInformation("Unhandled Zalo webhook event: {Event}", webhookEvent.EventName);
                    break;
            }
            return Ok();
        }
    }
}
