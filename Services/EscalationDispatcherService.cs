// using System;
// using System.Collections.Generic;
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

//         var vn = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
//         var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vn);
//         var today = now.Date;

//         // lấy tham số từ trigger
//         var role = context.MergedJobDataMap.GetString("Role")?.ToLower();

//         var messages = await dbContext.SVN_Messages
//             .Where(m => m.create_at >= today && m.create_at < today.AddDays(1))
//             .ToListAsync();

//         if (role == "supervisor")
//         {
//             var needSupervisor = messages
//                 .Where(m => m.isSent == 1
//                          && string.IsNullOrEmpty(m.isOperatorSeen)
//                          && string.IsNullOrEmpty(m.isSupervisorSeen)
//                          && (m.isSentToSupervisor == 0 || m.isSentToSupervisor == null))
//                 .GroupBy(m => m.to_department);

//             foreach (var deptGroup in needSupervisor)
//             {
//                 await EscalateDeptBatchAsync(zaloSendService, dbContext,
//                     deptGroup.Key, deptGroup.ToList(),
//                     roleDetail: "supervisor",
//                     extraNote: "⚠️ Không operator nào đã xem");
//             }
//         }
//         else if (role == "manager")
//         {
//             var needManager = messages
//                 .Where(m => m.isSent == 1
//                          && (m.isOperatorSeen == null || m.isOperatorSeen.Trim() == "")
//                          && string.IsNullOrEmpty(m.isSupervisorSeen)
//                          && string.IsNullOrEmpty(m.isManagerSeen)
//                          && (m.isSentToManager == 0 || m.isSentToManager == null))
//                 .GroupBy(m => m.to_department);

//             foreach (var deptGroup in needManager)
//             {
//                 await EscalateDeptBatchAsync(zaloSendService, dbContext,
//                     deptGroup.Key, deptGroup.ToList(),
//                     roleDetail: "manager",
//                     extraNote: "⚠️ Không supervisor nào đã xem");
//             }
//         }
//     }


//     // Gửi 1 tin hợp nhất (giống operator) cho mọi follower cấp trên trong 1 phòng ban
//     private async Task EscalateDeptBatchAsync(
//         IZaloSendService zaloSendService,
//         AppDbContext dbContext,
//         int? departmentId,
//         List<SVN_Messages> messagesForDept,
//         string roleDetail,
//         string extraNote)
//     {
//         if (departmentId == null || messagesForDept.Count == 0) return;

//         var followers = await dbContext.Followers
//             .Where(f => f.Role == departmentId
//                      && ((f.RoleDetail ?? string.Empty).ToLower() == roleDetail))
//             .ToListAsync();

//         if (followers.Count == 0) return;

//         // ✅ Truyền cả LIST để ZaloSendService gộp format giống operator
//         // var dynList = messagesForDept.Cast<dynamic>().ToList();
//         // var sendTime = messagesForDept.First().create_at;

//         List<dynamic> dynList;
//         var sendTime = messagesForDept.First().create_at;

//         if (departmentId == 5) // QC: chỉ giữ Defect
//         {
//             var filtered = new List<dynamic>();
//             foreach (var m in messagesForDept)
//             {
//                 var defectOnly = FilterDefectOnly(m.content);
//                 if (!string.IsNullOrWhiteSpace(defectOnly))
//                 {
//                     filtered.Add(new
//                     {
//                         id_message = m.id_message,
//                         operation = m.operation,
//                         content = defectOnly,
//                         type_message = m.type_message,
//                         create_at = m.create_at,
//                         to_department = m.to_department,
//                         UserId = m.UserId,
//                         isSent = m.isSent
//                     });
//                 }
//             }

//             if (filtered.Count == 0) return;   // không còn gì để gửi cho QC
//             dynList = filtered.Cast<dynamic>().ToList();
//         }
//         else
//         {
//             dynList = messagesForDept.Cast<dynamic>().ToList();
//         }

//         foreach (var follower in followers)
//         {
//             await zaloSendService.SendCombinedProductionMessageTemplateAsync(
//                 follower.UserId,
//                 dynList,
//                 sendTime,
//                 extraNote
//             );
//             await Task.Delay(50);
//         }

//         if (roleDetail == "supervisor")
//         {
//             foreach (var m in messagesForDept)
//                 m.isSentToSupervisor = 1;
//         }
//         else if (roleDetail == "manager")
//         {
//             foreach (var m in messagesForDept)
//                 m.isSentToManager = 1;
//         }

//         await dbContext.SaveChangesAsync();
//     }

//     private static string FilterDefectOnly(string? content)
//     {
//         if (string.IsNullOrWhiteSpace(content)) return string.Empty;

//         var parts = content.Split(';', StringSplitOptions.RemoveEmptyEntries)
//                            .Select(p => p.Trim());

//         var defectParts = parts.Where(p =>
//             p.Contains("defect", StringComparison.OrdinalIgnoreCase));

//         return string.Join(" ; ", defectParts).Trim();
//     }
// }


// Services/EscalationDispatcherService.cs
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

        // Lấy tham số từ trigger
        var role = context.MergedJobDataMap.GetString("Role")?.ToLower();

        var messages = await dbContext.SVN_Messages
            .Where(m => m.create_at >= today && m.create_at < today.AddDays(1))
            .ToListAsync();

        if (role == "supervisor")
        {
            var needSupervisor = messages
                .Where(m => m.isSent == 1
                         && string.IsNullOrEmpty(m.isOperatorSeen)
                         && string.IsNullOrEmpty(m.isSupervisorSeen)
                         && (m.isSentToSupervisor == 0 || m.isSentToSupervisor == null))
                .GroupBy(m => m.to_department);

            foreach (var deptGroup in needSupervisor)
            {
                await EscalateDeptBatchAsync(zaloSendService, dbContext,
                    deptGroup.Key, deptGroup.ToList(),
                    roleDetail: "supervisor",
                    extraNote: "⚠️ Không operator nào đã xem");
            }
        }
        else if (role == "manager")
        {
            var needManager = messages
                .Where(m => m.isSent == 1
                         // (nếu bạn muốn chặn khi operator đã xem, thêm điều kiện rỗng cho isOperatorSeen ở đây)
                         && string.IsNullOrEmpty(m.isOperatorSeen)
                         && string.IsNullOrEmpty(m.isSupervisorSeen)
                         && string.IsNullOrEmpty(m.isManagerSeen)
                         && (m.isSentToManager == 0 || m.isSentToManager == null))
                .GroupBy(m => m.to_department);

            foreach (var deptGroup in needManager)
            {
                await EscalateDeptBatchAsync(zaloSendService, dbContext,
                    deptGroup.Key, deptGroup.ToList(),
                    roleDetail: "manager",
                    extraNote: "⚠️ Không supervisor nào đã xem");
            }
        }
        else
        {
            _logger.LogWarning("EscalationDispatcherService được gọi nhưng thiếu Role trong JobDataMap.");
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

        // ======= CHANGED: chốt gửi escalate (set cờ) NGAY LẬP TỨC =======
        if (roleDetail == "supervisor")
        {
            foreach (var m in messagesForDept)
                m.isSentToSupervisor = 1; // CHANGED
        }
        else if (roleDetail == "manager")
        {
            foreach (var m in messagesForDept)
                m.isSentToManager = 1; // CHANGED
        }
        await dbContext.SaveChangesAsync(); // CHANGED: lưu flag ngay
        // ================================================================

        // Tìm followers theo phòng ban + cấp (nếu không có thì dừng — flags đã set rồi)
        var followers = await dbContext.Followers
            .Where(f => f.Role == departmentId
                     && ((f.RoleDetail ?? string.Empty).Trim().ToLower() == roleDetail))
            .ToListAsync();

        if (followers.Count == 0) return;

        // Áp dụng filter cho dept=5 (QC) giống dispatcher
        var filtered = FilterMessagesForRole(messagesForDept.Cast<object>().ToList(), departmentId.Value);
        if (filtered.Count == 0) return;

        var dynList = filtered.Cast<dynamic>().ToList();
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

    // --- Helpers giống ProductionMessageDispatcherService ---
    private List<object> FilterMessagesForRole(List<object> messages, int role)
    {
        var filteredMessages = new List<object>();

        foreach (var message in messages)
        {
            dynamic msg = message;
            string originalContent = msg.content?.ToString() ?? "";

            if (role == 5) // QC dept
            {
                string defectOnlyContent = FilterDefectOnly(originalContent);
                if (!string.IsNullOrEmpty(defectOnlyContent))
                {
                    var filteredMessage = new
                    {
                        id_message = msg.id_message,
                        operation = msg.operation,
                        content = defectOnlyContent,
                        type_message = msg.type_message,
                        create_at = msg.create_at,
                        to_department = msg.to_department,
                        UserId = msg.UserId,
                        isSent = msg.isSent
                    };
                    filteredMessages.Add(filteredMessage);
                }
            }
            else
            {
                filteredMessages.Add(message);
            }
        }

        return filteredMessages;
    }

    private static string FilterDefectOnly(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        var parts = content.Split(';', StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim());

        var defectParts = parts.Where(p =>
            p.Contains("defect", StringComparison.OrdinalIgnoreCase));

        return string.Join(" ; ", defectParts).Trim();
    }
}
