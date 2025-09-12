using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Quartz;


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
            var dtos = await dashboardService.GetDashboardDataByDate(currentDate);

            if (dtos != null && dtos.Count > 0)
            {
                var changedDtos = new List<DashBoardSummaryDto>();

                foreach (var dto in dtos)
                {
                    var key = dto.Operation ?? "unknown";
                    var snapshot = CreateSnapshot(dto);

                    if (_lastSnapshots.TryGetValue(key, out var lastSnapshot) && lastSnapshot == snapshot)
                    {
                        continue;
                    }

                    _lastSnapshots[key] = snapshot;
                    changedDtos.Add(dto);
                }

                if (changedDtos.Count > 0)
                {
                    await dashboardService.GenerateAndSaveWarningsAsync(changedDtos);
                }
                else
                {
                }
            }
            else
            {

            }
        }
        catch (Exception ex)
        {
        }
    }

    private string CreateSnapshot(DashBoardSummaryDto dto)
    {
        return $"{dto.Total_Qty}_{dto.Current_UPH}_{dto.Current_UPPH}_{dto.MaxLabor}_{dto.Total_NG_Qty}";
    }
}
