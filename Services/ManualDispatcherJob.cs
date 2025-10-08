using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using SigmaNotificationBackend.Services;

namespace SigmaNotificationBackend.Jobs
{
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

            _logger.LogInformation("ManualDispatcherJob started at {time}", DateTime.Now);

            await fetcher.Execute(context);     // đọc SVN_daily_target → ghi SVN_Messages
            await dispatcher.Execute(context);  // gửi operator ngay

            _logger.LogInformation("ManualDispatcherJob finished at {time}", DateTime.Now);
        }
    }
}
