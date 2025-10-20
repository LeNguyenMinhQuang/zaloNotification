    // Jobs/DailyApiFetcherJob.cs
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Quartz;
    using SigmaNotificationBackend.Data;      // DashboardDbContext, AppDbContext
    using SigmaNotificationBackend.Services;  // IZaloSendService
    using SigmaNotificationBackend.Models;    // SVN_Messages

    // Khớp response API bạn cung cấp
    public record ExternalApiResponse(
        bool ok,
        string? message,
        int numOfRow,
        object? content,
        int errorNumber,
        int userID,
        int odooUserID,
        string? dataType
    );

    namespace SigmaNotificationBackend.Jobs
    {
        public class DailyApiFetcherJob : IJob
        {
            private readonly ILogger<DailyApiFetcherJob> _logger;
            private readonly IServiceScopeFactory _scopeFactory;

            private const string DefaultApiUrl =
                "http://10.10.99.10:8100/api/APIServices/GetDataByDateAndOperation";

            public DailyApiFetcherJob(ILogger<DailyApiFetcherJob> logger, IServiceScopeFactory scopeFactory)
            {
                _logger = logger;
                _scopeFactory = scopeFactory;
            }

            public async Task Execute(IJobExecutionContext context)
            {
                using var scope = _scopeFactory.CreateScope();
                var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient  = httpFactory.CreateClient();

                var zaloSender = scope.ServiceProvider.GetRequiredService<IZaloSendService>();
                var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dashDb = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();

            try
            {
                // 1) LẤY DANH SÁCH OPERATION HÔM NAY từ DailyTargets (yyyyMMdd)
                var today = DateTime.Now.ToString("yyyyMMdd");
                var operations = await dashDb.SVN_Targets
                    .Where(t => t.Date_time != null
                            && t.Date_time.StartsWith(today)
                            && t.Operation != null)
                    .Select(t => t.Operation!)
                    .Distinct()
                    .ToListAsync();

                operations = operations
                    .Select(op =>
                    {
                        if (op.Equals("Injection_POP", StringComparison.OrdinalIgnoreCase))
                            return "Injection";
                        if (op.Contains("walter", StringComparison.OrdinalIgnoreCase))
                            return "Walter";
                        if (op.Contains("toast", StringComparison.OrdinalIgnoreCase))
                            return "Toast";
                        return op;
                    })
                    .Distinct()
                    .ToList();

                if (operations.Count == 0)
                {
                    _logger.LogWarning("DailyApiFetcherJob: không tìm thấy Operation nào trong DailyTarget cho hôm nay ({Today}).", today);
                    return;
                }

                // 2) Gọi API cho từng operation và gộp thành 1 message
                var sb = new StringBuilder();
                // sb.AppendLine($"🔔 Time: {DateTime.Now:HH:mm:ss dd/MM/yyyy}");

                int appended = 0;
                foreach (var op in operations)
                {
                    try
                    {
                        var url = $"{DefaultApiUrl}?operation={Uri.EscapeDataString(op)}";
                        _logger.LogInformation("DailyApiFetcherJob: gọi API {Url}", url);

                        var resp = await httpClient.GetFromJsonAsync<ExternalApiResponse>(url);

                        if (resp?.ok != true || string.IsNullOrWhiteSpace(resp.message))
                        {
                            _logger.LogWarning("DailyApiFetcherJob: API không hợp lệ hoặc message rỗng (Op={Op})", op);
                            continue;
                        }

                        var block = BuildOperationBlock(op, resp.message.Trim());

                        if (appended > 0) sb.AppendLine();
                        sb.Append(block);
                        appended++;
                    }
                    catch (Exception exOp)
                    {
                        _logger.LogError(exOp, "DailyApiFetcherJob: lỗi khi gọi API (Op={Op})", op);
                    }
                }

                if (appended == 0)
                {
                    _logger.LogWarning("DailyApiFetcherJob: không có message hợp lệ để gửi.");
                    return;
                }

                var finalMessage = sb.ToString();

                // 3) LƯU vào SVN_Messages: tạo 1 record cho mỗi dept 1,3,5
                var vnTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTz);
                var created = new List<SVN_Messages>();

                foreach (var dept in new[] { 2 })
                {
                    var msg = new SVN_Messages
                    {
                        operation = "Checklist",
                        content = finalMessage,
                        type_message = "",
                        create_at = nowVn,
                        to_department = dept,
                        UserId = "",
                        isSent = 1,
                        isSentToSupervisor = 1,
                        isSentToManager = 1
                    };
                    db.SVN_Messages.Add(msg);
                    created.Add(msg); // giữ reference
                }
                await db.SaveChangesAsync();

                

                // var checklistMsgs = await db.SVN_Messages
                // .Where(m => m.operation == "Checklist"
                //         && m.content == finalMessage
                //         && m.create_at == nowVn)   // đúng mốc thời gian vừa insert
                // .OrderBy(m => m.id_message)
                // .ToListAsync();

                // 4) GỬI cho Role=1 & RoleDetail="operator"
                var operatorIds = await db.Followers
                        .AsNoTracking()
                        .Where(f =>
                            (f.RoleDetail ?? "").Trim().ToLower() == "manager")
                        // .Where(f =>
                        //     (f.UserId ?? "").Trim().ToLower() == "2117908013271116630")
                        .Select(f => f.UserId)
                        .Distinct()
                        .ToListAsync();

                _logger.LogInformation("DailyApiFetcherJob: tìm thấy {Count} operator vao luc" + nowVn, operatorIds.Count);

                if (!operatorIds.Any())
                {
                    _logger.LogWarning("DailyApiFetcherJob: không tìm thấy operator Role=1 để gửi tin.");
                    return;
                }

                // foreach (var uid in operatorIds)
                // {
                //     try
                //     {
                //         await zaloSender.SendMessageToUserAsync(uid, finalMessage);
                //         await Task.Delay(100); // tránh rate limit
                //     }
                //     catch (Exception exUser)
                //     {
                //         _logger.LogError(exUser, "Lỗi gửi tin cho operator {UserId}", uid);
                //     }
                // }
                var dynList = created.Cast<dynamic>().ToList();
                foreach (var uid in operatorIds)
                {
                    await zaloSender.SendCombinedProductionMessageTemplateAsync(uid, dynList, nowVn);
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DailyApiFetcherJob: lỗi tổng thể khi gộp & gửi message.");
            }
            }

            // ===== Helpers =====

            private static string BuildOperationBlock(string operation, string checklistLine)
            {
                // Đếm số 🔴 để ra verdict
                int red = CountOccurrences(checklistLine, "🔴");
                string verdict = red >= 1
                    ? "Các bộ phận xin hãy hoàn thành checklist"
                    : "Ok";

                var formatted = ReformatChecklist(checklistLine);

                var sb = new StringBuilder();
                sb.AppendLine($"===={operation}====");
                sb.AppendLine(formatted);
                sb.AppendLine();
                sb.Append("=> ").Append(verdict);
                sb.AppendLine();
                return sb.ToString();
            }

            private static int CountOccurrences(string text, string token)
            {
                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return 0;
                int count = 0, idx = 0;
                while (true)
                {
                    idx = text.IndexOf(token, idx, StringComparison.Ordinal);
                    if (idx == -1) break;
                    count++;
                    idx += token.Length;
                }
                return count;
            }

            private static string ReformatChecklist(string rawMessage)
            {
                // Bỏ prefix "Checklist status: "
                var content = rawMessage.Replace("Checklist status:", "").Trim();

                // Tách theo dấu |
                var parts = content.Split('|', StringSplitOptions.RemoveEmptyEntries);

                var sb = new StringBuilder();

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (trimmed.EndsWith("Checked"))
                    {
                        var text = trimmed.Replace("Checked", "").TrimEnd('-', ' ').Trim();
                        sb.AppendLine($"Checked: {text}");
                    }
                    else if (trimmed.EndsWith("Confirmed"))
                    {
                        var text = trimmed.Replace("Confirmed", "").TrimEnd('-', ' ').Trim();
                        sb.AppendLine($"Confirmed: {text}");
                    }
                    else
                    {
                        // fallback giữ nguyên nếu format lạ
                        sb.AppendLine(trimmed);
                    }
                }

                return sb.ToString().Trim();
            }
        }
    }