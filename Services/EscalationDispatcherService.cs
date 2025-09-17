using System;
using System.Collections.Generic;
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

        var vn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vn);
        var today = now.Date;

        // Lấy tất cả messages trong ngày
        var messages = await dbContext.SVN_Messages
            .Where(m => m.create_at >= today && m.create_at < today.AddDays(1))
            .ToListAsync();

        // ===== 1) Escalate lên SUPERVISOR: chưa operator xem, chưa supervisor xem, quá 5'
        // var needSupervisor = messages
        //     .Where(m => string.IsNullOrEmpty(m.isOperatorSeen)
        //              && string.IsNullOrEmpty(m.isSupervisorSeen)
        //              && (now - m.create_at).TotalMinutes >= 5)
        //     .GroupBy(m => m.to_department);
        var needSupervisor = messages
        .Where(m => string.IsNullOrEmpty(m.isOperatorSeen)
                && string.IsNullOrEmpty(m.isSupervisorSeen)
                && (m.isSentToSupervisor == 0 || m.isSentToSupervisor == null)   // 🔥 thêm check cờ
                && (now - m.create_at).TotalMinutes >= 5)
        .GroupBy(m => m.to_department);

        foreach (var deptGroup in needSupervisor)
        {
            await EscalateDeptBatchAsync(
                zaloSendService,
                dbContext,
                deptGroup.Key,
                deptGroup.ToList(),
                roleDetail: "supervisor",
                extraNote: "⚠️ Không operator nào đã xem"
            );
        }

        // ===== 2) Escalate lên MANAGER: chưa supervisor xem, chưa manager xem, quá 30'
        // var needManager = messages
        //     .Where(m => string.IsNullOrEmpty(m.isSupervisorSeen)
        //              && string.IsNullOrEmpty(m.isManagerSeen)
        //              && (now - m.create_at).TotalMinutes >= 30)
        //     .GroupBy(m => m.to_department);
        var needManager = messages
        .Where(m => string.IsNullOrEmpty(m.isSupervisorSeen)
                && string.IsNullOrEmpty(m.isManagerSeen)
                && (m.isSentToManager == 0 || m.isSentToManager == null)        // 🔥 thêm check cờ
                && (now - m.create_at).TotalMinutes >= 30)
        .GroupBy(m => m.to_department);

        foreach (var deptGroup in needManager)
        {
            await EscalateDeptBatchAsync(
                zaloSendService,
                dbContext,
                deptGroup.Key,
                deptGroup.ToList(),
                roleDetail: "manager",
                extraNote: "⚠️ Không supervisor nào đã xem"
            );
        }
    }

    // Gửi 1 tin hợp nhất (giống operator) cho mọi follower cấp trên trong 1 phòng ban
    private async Task EscalateDeptBatchAsync(
        IZaloSendService zaloSendService,
        AppDbContext dbContext,
        int? departmentId,
        List<SVN_Messages> messagesForDept,
        string roleDetail,
        string extraNote)
    {
        if (departmentId == null || messagesForDept.Count == 0) return;

        var followers = await dbContext.Followers
            .Where(f => f.Role == departmentId
                     && ((f.RoleDetail ?? string.Empty).ToLower() == roleDetail))
            .ToListAsync();

        if (followers.Count == 0) return;

        // ✅ Truyền cả LIST để ZaloSendService gộp format giống operator
        var dynList = messagesForDept.Cast<dynamic>().ToList();
        var sendTime = messagesForDept.First().create_at;

        foreach (var follower in followers)
        {
            await zaloSendService.SendCombinedProductionMessageTemplateAsync(
                follower.UserId,
                dynList,
                sendTime,
                extraNote
            );
            await Task.Delay(50);
        }

        if (roleDetail == "supervisor")
        {
            foreach (var m in messagesForDept)
                m.isSentToSupervisor = 1;
        }
        else if (roleDetail == "manager")
        {
            foreach (var m in messagesForDept)
                m.isSentToManager = 1;
        }

        await dbContext.SaveChangesAsync();
    }
}



