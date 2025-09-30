using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using SigmaNotificationBackend.Models; // để dùng DailyTarget

public class ScheduledDashboardFetcher : IJob
{
    private readonly ILogger<ScheduledDashboardFetcher> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private Dictionary<string, string> _lastSnapshots = new();

    public ScheduledDashboardFetcher(
        ILogger<ScheduledDashboardFetcher> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();

        try
        {
            var dashboardService = scope.ServiceProvider.GetRequiredService<DashboardService>();
            string currentDate = DateTime.Now.ToString("yyyyMMdd");

            // 🔄 GIỜ TRẢ VỀ List<DailyTarget> (không dùng DTO nữa)
            var rows = await dashboardService.GetDashboardDataByDate(currentDate);

            if (rows != null && rows.Count > 0)
            {
                var changedRows = new List<DailyTarget>();

                foreach (var row in rows)
                {
                    var key = row.Operation ?? "unknown";
                    var snapshot = CreateSnapshot(row);

                    if (_lastSnapshots.TryGetValue(key, out var lastSnapshot) && lastSnapshot == snapshot)
                    {
                        continue;
                    }

                    _lastSnapshots[key] = snapshot;
                    changedRows.Add(row);
                }

                if (changedRows.Count > 0)
                {
                    // 🔄 Sinh cảnh báo trực tiếp từ DailyTarget (không đi qua DTO)
                    await dashboardService.GenerateAndSaveWarningsAsync(changedRows);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScheduledDashboardFetcher error");
        }
    }

    // 🔄 Snapshot từ DailyTarget để phát hiện thay đổi (giữ logic y hệt)
    private string CreateSnapshot(DailyTarget row)
    {
        return $"{row.Total_Qty}_{row.Current_UPH}_{row.Current_UPPH}_{row.MaxLabor}_{row.Total_NG_Qty}";
    }
}
