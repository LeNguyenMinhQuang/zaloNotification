using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using SigmaNotificationBackend.Data;
using SigmaNotificationBackend.Models;
using SigmaNotificationBackend.Services;

public class EscalationDispatcherService : IJob
{
    private readonly ILogger<EscalationDispatcherService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public EscalationDispatcherService(
        ILogger<EscalationDispatcherService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var zaloSendService = scope.ServiceProvider.GetRequiredService<IZaloSendService>();

        var now = DateTime.UtcNow.AddHours(7);
        var today = now.Date;

        var messages = await dbContext.SVN_Messages
            .Where(m => m.create_at >= today && m.create_at < today.AddDays(1))
            .ToListAsync();

        foreach (var msg in messages)
        {
            try
            {
                var ageMinutes = (now - msg.create_at).TotalMinutes;

                if (string.IsNullOrEmpty(msg.isOperatorSeen) && ageMinutes >= 10 && string.IsNullOrEmpty(msg.isSupervisorSeen))
                {
                    await EscalateToRole(zaloSendService, dbContext, msg, "supervisor", "<không operator nào đã xem>");
                }

                if (string.IsNullOrEmpty(msg.isSupervisorSeen) && ageMinutes >= 20 && string.IsNullOrEmpty(msg.isManagerSeen))
                {
                    await EscalateToRole(zaloSendService, dbContext, msg, "manager", "<không supervisor nào đã xem>");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Escalation error for message {Id}", msg.id_message);
            }
        }
    }

    private async Task EscalateToRole(IZaloSendService zaloSendService, AppDbContext dbContext, SVN_Messages msg, string roleDetail, string extraNote)
    {
        var followers = await dbContext.Followers
            .Where(f => f.Role == msg.to_department && f.RoleDetail.ToLower() == roleDetail)
            .ToListAsync();

        foreach (var follower in followers)
        {
            await zaloSendService.SendCombinedProductionMessageTemplateAsync(
                follower.UserId,
                new List<dynamic> { msg },
                DateTime.UtcNow.AddHours(7),
                extraNote
            );
        }
    }
}
