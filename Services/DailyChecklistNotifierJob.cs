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
using SigmaNotificationBackend.Data;     // 👈 thêm: để lấy DashboardDbContext
using SigmaNotificationBackend.Services; // IZaloSendService

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

        // private static readonly int[] TargetDepartments = new[] { 1, 3, 5 };
        private static readonly int[] TargetDepartments = new[] { 1 };

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
            var httpClient = httpFactory.CreateClient();

            var zaloSender = scope.ServiceProvider.GetRequiredService<IZaloSendService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dashDb = scope.ServiceProvider.GetRequiredService<DashboardDbContext>(); // 👈 thêm

            try
            {
                // 👇 LẤY DANH SÁCH OPERATION TRONG NGÀY HÔM NAY (yyyyMMdd) TỪ DailyTargets
                var today = DateTime.Now.ToString("yyyyMMdd");
                var operations = await dashDb.DailyTargets
                    .Where(t => t.Date_time != null
                             && t.Date_time.StartsWith(today)   // phòng khi Date_time có kèm giờ
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
                        return op;
                    })
                    .Distinct()
                    .ToList();

                if (operations.Count == 0)
                {
                    _logger.LogWarning("DailyApiFetcherJob: không tìm thấy Operation nào trong DailyTarget cho hôm nay ({Today}).", today);
                    return;
                }

                var sb = new StringBuilder();

                // 🔔 Header thời gian (giờ VN theo máy chủ)
                var now = DateTime.Now;
                sb.AppendLine($"🔔 Time: {now:HH:mm:ss dd/MM/yyyy}");

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

                        if (appended > 0) sb.AppendLine(); // dòng trống giữa các block
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

                // 3) Lưu vào SVN_Messages: 1 record cho mỗi phòng ban 1/3/5
                //    Đặt isSent=1 theo pattern dispatcher hiện tại (đã "coi như gửi xong").
                var vnTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTz);

                var saved = new List<SVN_Messages>();
                foreach (var dept in new[] { 1, 3, 5 })
                {
                    var m = new SVN_Messages
                    {
                        operation = "Checklist",
                        content = finalMessage,
                        type_message = "",
                        create_at = nowVn,
                        to_department = dept,
                        UserId = "",
                        isSent = 1, // theo pattern dispatcher hiện tại
                        isSentToSupervisor = 1,
                        isSentToManager = 1,
                        isOperatorSeen = "x",
                        isSupervisorSeen = "x",
                    };
                    db.SVN_Messages.Add(m);
                    saved.Add(m);
                }
                await db.SaveChangesAsync();

                var messageIds = saved.Select(x => x.id_message).ToList();

                // 4) Gửi message cho MANAGER của các phòng ban 1/3/5
                var managerIds = await db.Followers
                    .AsNoTracking()
                    // .Where(f => f.Role == 1 && (f.RoleDetail ?? "").Trim().ToLower() == "operator")
                    .Where(f =>
                        (f.Role == 1 || f.Role == 3 || f.Role == 5) &&
                        (f.RoleDetail ?? "").Trim().ToLower() == "manager")
                    .Select(f => f.UserId)
                    .Distinct()
                    .ToListAsync();

                if (!managerIds.Any())
                {
                    _logger.LogWarning("DailyApiFetcherJob: không tìm thấy manager ở các phòng ban {Depts}.", string.Join(",", TargetDepartments));
                    return;
                }

                _logger.LogInformation("DailyApiFetcherJob: gửi 1 tin hợp nhất cho {Count} managers (depts 1,3,5).", managerIds.Count);

                foreach (var uid in managerIds)
                {
                    try
                    {
                        // 👇 gửi kèm nút "Đã xem" với payload là list id_message
                        await zaloSender.SendAckTemplateAsync(uid, finalMessage, messageIds);
                        await Task.Delay(100);
                    }
                    catch (Exception exUser)
                    {
                        _logger.LogError(exUser, "Lỗi gửi tin cho manager {UserId}", uid);
                    }
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
            // Đếm số 🟢 và 🔴
            int green = CountOccurrences(checklistLine, "🟢");
            int red = CountOccurrences(checklistLine, "🔴");

            // Quy tắc:
            // - Không có 🔴  => Good job
            // - Có >=1 🔴     => Các bộ phận xin hãy hoàn thành checklist
            string verdict = red >= 1
                ? "Các bộ phận xin hãy hoàn thành checklist"
                : "Good job";

            var formatted = ReformatChecklist(checklistLine);

            var sb = new StringBuilder();
            sb.AppendLine($"===={operation}====");
            sb.AppendLine(formatted);
            sb.AppendLine();
            sb.Append("=> ").Append(verdict);
            sb.AppendLine();
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
                    // bỏ chữ "Checked" ở cuối
                    var text = trimmed.Replace("Checked", "").TrimEnd('-', ' ').Trim();
                    sb.AppendLine($"Check: {text}");
                }
                else if (trimmed.EndsWith("Confirmed"))
                {
                    // bỏ chữ "Confirmed" ở cuối
                    var text = trimmed.Replace("Confirmed", "").TrimEnd('-', ' ').Trim();
                    sb.AppendLine($"Confirm: {text}");
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
