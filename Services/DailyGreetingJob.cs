using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using SigmaNotificationBackend.Services;
using SigmaNotificationBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace SigmaNotificationBackend.Jobs
{
    public class DailyGreetingJob : IJob
    {
        private readonly ILogger<DailyGreetingJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DailyGreetingJob(ILogger<DailyGreetingJob> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var zaloSender = scope.ServiceProvider.GetRequiredService<IZaloSendService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); // dùng để lấy danh sách follower

            try
            {
                // Lấy giờ VN hiện tại để in vào message / làm log
                var vnTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTz);

                // Lấy danh sách userId (theo ví dụ trong project: RoleDetail == "manager")
                var recipientIds = await db.Followers
                    .Where(f => new[] { "manager", "supervisor", "operator" }
                    .Contains((f.RoleDetail ?? "").Trim().ToLower()))
                    .Select(f => f.UserId)
                    .Distinct()
                    .ToListAsync();

                if (recipientIds == null || !recipientIds.Any())
                {
                    _logger.LogWarning("DailyGreetingJob: không tìm thấy người nhận để gửi (RoleDetail=manager).");
                    return;
                }

                // Tạo message in-memory (không lưu DB), đặt id_message = 0 cho mỗi item
                var content = "Chào buổi sáng, chúc một ngày làm việc tốt lành. Hãy nhấn vào nút đã xem nhé!";
                var dynList = new List<dynamic>
                {
                    new {
                        id_message = 0,
                        operation = "Greeting",
                        content = content
                    }
                };

                // Gửi cho từng user
                foreach (var uid in recipientIds)
                {
                    try
                    {
                        await zaloSender.SendCombinedProductionMessageTemplateAsync(uid, dynList, nowVn);
                        await Task.Delay(100); // tránh rate limit nhẹ
                    }
                    catch (Exception exSend)
                    {
                        _logger.LogError(exSend, "DailyGreetingJob: lỗi gửi tới {UserId}", uid);
                    }
                }

                _logger.LogInformation("DailyGreetingJob: đã gửi greeting tới {Count} user lúc {TimeVN}", recipientIds.Count, nowVn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DailyGreetingJob: lỗi tổng thể khi thực hiện job.");
            }
        }
    }
}
