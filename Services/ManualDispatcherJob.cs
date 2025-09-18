using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using SigmaNotificationBackend.Services;

namespace SigmaNotificationBackend.Jobs
{
    /// <summary>
    /// Job thủ công: mỗi lần chạy sẽ
    /// 1) Gọi ScheduledDashboardFetcher để lấy dữ liệu SVN_daily_target và sinh SVN_Messages
    /// 2) Gọi ProductionMessageDispatcherService để gửi ngay cho operator
    /// </summary>
    public class ManualDispatcherJob : IJob
    {
        private readonly ILogger<ManualDispatcherJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ManualDispatcherJob(
            ILogger<ManualDispatcherJob> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var fetcher = scope.ServiceProvider.GetRequiredService<ScheduledDashboardFetcher>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<ProductionMessageDispatcherService>();

            try
            {
                _logger.LogInformation("ManualDispatcherJob started at {time}", DateTime.Now);

                // 1) Lấy dữ liệu mới từ SVN_Daily_Target → ghi vào SVN_Messages
                await fetcher.Execute(null);

                // 2) Gửi tin nhắn ngay cho operator
                await dispatcher.Execute(null);

                _logger.LogInformation("ManualDispatcherJob completed successfully at {time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ManualDispatcherJob failed");
            }
        }
    }
}
