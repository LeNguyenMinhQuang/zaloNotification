using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SigmaNotificationBackend.Data;
using SigmaNotificationBackend.Services;

namespace SigmaNotificationBackend.Jobs
{

    public class ProductionMessageDispatcherService : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProductionMessageDispatcherService> _logger;



        public ProductionMessageDispatcherService(
            IServiceProvider serviceProvider,
            ILogger<ProductionMessageDispatcherService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var zaloSender = scope.ServiceProvider.GetRequiredService<IZaloSendService>();

                var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);

                var messagesToSend = await db.SVN_Messages
                    .Where(m => m.create_at.Date == nowVn.Date
                                && m.create_at <= nowVn
                    // && (m.isSent == 0 || m.isSent == null))
                                && m.isSent == 0)
                    .ToListAsync();

                var distinctMessages = messagesToSend
                    .GroupBy(m => new { m.content, m.to_department, m.operation })
                    .Select(g => g.OrderBy(m => m.create_at).First())
                    .OrderBy(m => m.create_at)
                    .ToList();

                _logger.LogInformation("Tìm thấy {Count} tin nhắn có thể gửi vào lúc {Time}",
                    distinctMessages.Count, nowVn.ToString("HH:mm"));

                var messagesByDepartment = distinctMessages
                    .GroupBy(m => m.to_department)
                    .ToList();

                foreach (var departmentGroup in messagesByDepartment)
                {
                    int role = departmentGroup.Key;
                    var messagesInGroup = departmentGroup.ToList();

                    // var userIds = await db.Followers
                    //     .Where(f => f.Role == role)
                    //     .Select(f => f.UserId)
                    //     .ToListAsync();

                    var userIds = await db.Followers
                    .Where(f => f.Role == role
                            && (f.RoleDetail ?? string.Empty).ToLower() == "operator") // 🔥 CHỈ gửi operator
                    .Select(f => f.UserId)
                    .ToListAsync();

                    if (!userIds.Any())
                    {
                        _logger.LogWarning("Không tìm thấy User nào với vai trò {Role}", role);
                        continue;
                    }

                    _logger.LogInformation("Đang gửi tin nhắn tới {UserCount} người dùng có vai trò {Role}",
                        userIds.Count, role);

                    foreach (var userId in userIds)
                    {
                        try
                        {
                            var filteredMessages = FilterMessagesForRole(messagesInGroup.Cast<object>().ToList(), role);

                            await zaloSender.SendCombinedProductionMessageTemplateAsync(
                            userId,
                            filteredMessages.Cast<dynamic>().ToList(), // ← Đúng
                            messagesInGroup.First().create_at
                            );

                            await Task.Delay(100);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi gửi tin nhắn tới người dùng có UserId là : {UserId}", userId);
                        }
                    }

                    foreach (var msg in messagesInGroup)
                    {
                        msg.isSent = 1;
                    }
                }

                if (distinctMessages.Any())
                {
                    await db.SaveChangesAsync();
                    _logger.LogInformation("Đã đánh dấu {Count} messages là đã gửi", distinctMessages.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong File ProductionMessageDispatcherService");
            }
        }


        private List<object> FilterMessagesForRole(List<object> messages, int role)
        {
            var filteredMessages = new List<object>();

            foreach (var message in messages)
            {

                dynamic msg = message;
                string originalContent = msg.content?.ToString() ?? "";

                if (role == 5) // QC role
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


        private string FilterDefectOnly(string content)
        {
            if (string.IsNullOrEmpty(content))
                return "";


            var parts = content.Split(';', StringSplitOptions.RemoveEmptyEntries);


            var defectParts = parts
                .Where(part => part.Trim().ToLower().Contains("defect"))
                .Select(part => part.Trim())
                .ToList();

            return string.Join(" ; ", defectParts);
        }
    }

}

