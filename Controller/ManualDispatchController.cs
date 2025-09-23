// using System;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
// using Quartz;

// namespace SigmaNotificationBackend.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class ManualDispatchController : ControllerBase
//     {
//         private readonly ISchedulerFactory _schedulerFactory;
//         private readonly ILogger<ManualDispatchController> _logger;

//         public ManualDispatchController(
//             ISchedulerFactory schedulerFactory,
//             ILogger<ManualDispatchController> logger)
//         {
//             _schedulerFactory = schedulerFactory;
//             _logger = logger;
//         }

//         [HttpGet("trigger")]
//         public async Task<IActionResult> Trigger()
//         {
//             var scheduler = await _schedulerFactory.GetScheduler();

//             // 1) chạy fetch + operator ngay
//             await scheduler.TriggerJob(new JobKey("ManualDispatcherJob"));

//             // 2) schedule escalation one-off: +2' supervisor
//             var supJob = JobBuilder.Create<EscalationDispatcherService>()
//                 .WithIdentity($"ManualEscalationSupervisor-{Guid.NewGuid()}")
//                 .UsingJobData("Role", "supervisor")
//                 .Build();

//             var supTrigger = TriggerBuilder.Create()
//                 .StartAt(DateBuilder.FutureDate(9, IntervalUnit.Minute)) // test 2'
//                 .Build();

//             await scheduler.ScheduleJob(supJob, supTrigger);

//             // 3) schedule escalation one-off: +3' manager
//             var mgrJob = JobBuilder.Create<EscalationDispatcherService>()
//                 .WithIdentity($"ManualEscalationManager-{Guid.NewGuid()}")
//                 .UsingJobData("Role", "manager")
//                 .Build();

//             var mgrTrigger = TriggerBuilder.Create()
//                 .StartAt(DateBuilder.FutureDate(19, IntervalUnit.Minute)) // test 3'
//                 .Build();

//             await scheduler.ScheduleJob(mgrJob, mgrTrigger);

//             _logger.LogInformation("Manual API called at {time}: operator sent, escalation scheduled (+2'/+3').",
//                 DateTime.Now);

//             return Ok(new { ok = true });
//         }
//     }
// }


using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Quartz;

namespace SigmaNotificationBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManualDispatchController : ControllerBase
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<ManualDispatchController> _logger;
        private readonly IMemoryCache _cache;

        // Điều chỉnh số phút cooldown tại đây
        private const int COOLDOWN_MINUTES = 9;

        // Key cache toàn cục cho cooldown
        private const string COOL_KEY = "manual_dispatch_cooldown_global";

        // Gate để tránh race-condition giữa nhiều request đồng thời
        private static readonly SemaphoreSlim _gate = new(1, 1);

        public ManualDispatchController(
            ISchedulerFactory schedulerFactory,
            ILogger<ManualDispatchController> logger,
            IMemoryCache cache)
        {
            _schedulerFactory = schedulerFactory;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet("trigger")]
        public async Task<IActionResult> Trigger()
        {
            // Thời điểm kết thúc cooldown (UTC) để trả về cho client
            DateTimeOffset? cooldownUntil = null;

            await _gate.WaitAsync();
            try
            {
                // Nếu đang trong cooldown => 429 Too Many Requests
                if (_cache.TryGetValue<DateTimeOffset>(COOL_KEY, out var until))
                {
                    cooldownUntil = until;
                    Response.Headers["Retry-After"] = Math.Max(1, (int)(until - DateTimeOffset.UtcNow).TotalSeconds).ToString();
                    return StatusCode(429, new
                    {
                        ok = false,
                        message = $"Manual trigger đang trong cooldown. Thử lại sau {COOLDOWN_MINUTES} phút.",
                        cooldownUntil = until.ToLocalTime().ToString("HH:mm:ss dd/MM/yyyy")
                    });
                }

                // Đặt cooldown mới (absolute TTL 5’)
                cooldownUntil = DateTimeOffset.UtcNow.AddMinutes(COOLDOWN_MINUTES);
                _cache.Set(COOL_KEY, cooldownUntil.Value,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = cooldownUntil
                    });
            }
            finally
            {
                _gate.Release();
            }

            // Ngoài critical section: chạy logic dispatch như cũ
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();

                // 1) chạy fetch + operator ngay
                await scheduler.TriggerJob(new JobKey("ManualDispatcherJob"));

                // 2) schedule escalation one-off: +9' supervisor
                var supJob = JobBuilder.Create<EscalationDispatcherService>()
                    .WithIdentity($"ManualEscalationSupervisor-{Guid.NewGuid()}")
                    .UsingJobData("Role", "supervisor")
                    .Build();

                var supTrigger = TriggerBuilder.Create()
                    .StartAt(DateBuilder.FutureDate(9, IntervalUnit.Minute))
                    .Build();

                await scheduler.ScheduleJob(supJob, supTrigger);

                // 3) schedule escalation one-off: +19' manager
                var mgrJob = JobBuilder.Create<EscalationDispatcherService>()
                    .WithIdentity($"ManualEscalationManager-{Guid.NewGuid()}")
                    .UsingJobData("Role", "manager")
                    .Build();

                var mgrTrigger = TriggerBuilder.Create()
                    .StartAt(DateBuilder.FutureDate(19, IntervalUnit.Minute))
                    .Build();

                await scheduler.ScheduleJob(mgrJob, mgrTrigger);

                _logger.LogInformation("Manual API accepted at {time}: operator sent, escalation scheduled (+9'/+19').",
                    DateTime.Now);

                return Ok(new
                {
                    ok = true,
                    message = $"Manual trigger accepted. Cooldown đến {cooldownUntil.Value.ToLocalTime():HH:mm:ss dd/MM/yyyy}.",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual trigger failed. Clearing cooldown key.");
                // Nếu có lỗi, bỏ cooldown để gọi lại được
                _cache.Remove(COOL_KEY);

                return StatusCode(500, new
                {
                    ok = false,
                    message = "Có lỗi khi thực thi manual trigger.",
                    error = ex.Message
                });
            }
        }
    }
}
