// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Quartz;
// using SigmaNotificationBackend.Data;
// using SigmaNotificationBackend.Models;
// using SigmaNotificationBackend.Services;

// public class EscalationDispatcherService : IJob
// {
//     private readonly ILogger<EscalationDispatcherService> _logger;
//     private readonly IServiceScopeFactory _scopeFactory;

//     public EscalationDispatcherService(
//         ILogger<EscalationDispatcherService> logger,
//         IServiceScopeFactory scopeFactory)
//     {
//         _logger = logger;
//         _scopeFactory = scopeFactory;
//     }

//     public async Task Execute(IJobExecutionContext context)
//     {
//         using var scope = _scopeFactory.CreateScope();
//         var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//         var zaloSendService = scope.ServiceProvider.GetRequiredService<IZaloSendService>();

//         var now = DateTime.UtcNow.AddHours(7);
//         var today = now.Date;

//         var messages = await dbContext.SVN_Messages
//             .Where(m => m.create_at >= today && m.create_at < today.AddDays(1))
//             .ToListAsync();

//         foreach (var msg in messages)
//         {
//             try
//             {
//                 var ageMinutes = (now - msg.create_at).TotalMinutes;

//                 // if (string.IsNullOrEmpty(msg.isOperatorSeen) && ageMinutes >= 10 && string.IsNullOrEmpty(msg.isSupervisorSeen))
//                 // {
//                 //     await EscalateToRole(zaloSendService, dbContext, msg, "supervisor", "<không operator nào đã xem>");
//                 // }

//                 // if (string.IsNullOrEmpty(msg.isSupervisorSeen) && ageMinutes >= 20 && string.IsNullOrEmpty(msg.isManagerSeen))
//                 // {
//                 //     await EscalateToRole(zaloSendService, dbContext, msg, "manager", "<không supervisor nào đã xem>");
//                 // }

//                 if (string.IsNullOrEmpty(msg.isOperatorSeen) && ageMinutes >= 1 && string.IsNullOrEmpty(msg.isSupervisorSeen))
//                 {
//                     await EscalateToRole(zaloSendService, dbContext, msg, "supervisor", "<không operator nào đã xem>");
//                 }

//                 if (string.IsNullOrEmpty(msg.isSupervisorSeen) && ageMinutes >= 29 && string.IsNullOrEmpty(msg.isManagerSeen))
//                 {
//                     await EscalateToRole(zaloSendService, dbContext, msg, "manager", "<không supervisor nào đã xem>");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Escalation error for message {Id}", msg.id_message);
//             }
//         }
//     }

//     private async Task EscalateToRole(IZaloSendService zaloSendService, AppDbContext dbContext, SVN_Messages msg, string roleDetail, string extraNote)
//     {
//         var followers = await dbContext.Followers
//             // .Where(f => f.Role == msg.to_department && f.RoleDetail.ToLower() == roleDetail)
//             .Where(f => f.Role == msg.to_department
//          && (f.RoleDetail ?? string.Empty).ToLower() == roleDetail)
//             .ToListAsync();

//         foreach (var follower in followers)
//         {
//             await zaloSendService.SendCombinedProductionMessageTemplateAsync(
//                 follower.UserId,
//                 new List<dynamic> { msg },
//                 DateTime.UtcNow.AddHours(7),
//                 extraNote
//             );
//         }
//     }
// }

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
        var needSupervisor = messages
            .Where(m => string.IsNullOrEmpty(m.isOperatorSeen)
                     && string.IsNullOrEmpty(m.isSupervisorSeen)
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
        var needManager = messages
            .Where(m => string.IsNullOrEmpty(m.isSupervisorSeen)
                     && string.IsNullOrEmpty(m.isManagerSeen)
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
    }
}

