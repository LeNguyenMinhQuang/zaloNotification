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
        private const int COOLDOWN_MINUTES = 19;

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

        // [HttpGet("trigger")]
        // public async Task<IActionResult> Trigger()
        // {
        //     // ========== ⛔ Off-hours guard: cấm gọi 20:00–08:15 và 12:10–13:10 (giờ VN) ==========
        //     var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        //     var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        //     var nightStart = new TimeSpan(20, 0, 0);   // 20:00
        //     var nightEnd = new TimeSpan(8, 5, 0);   // 08:15 (hôm sau)

        //     var lunchStart = new TimeSpan(11, 30, 0);  // 12:10
        //     var lunchEnd = new TimeSpan(13, 10, 0);  // 13:10

        //     bool inNight = nowLocal.TimeOfDay >= nightStart || nowLocal.TimeOfDay < nightEnd;
        //     bool inLunch = nowLocal.TimeOfDay >= lunchStart && nowLocal.TimeOfDay < lunchEnd;

        //     DateTime? nextAllowed = null;
        //     if (inNight)
        //     {
        //         // nếu đang sau 20:00 → sáng mai 08:15; nếu đang trước 08:15 → hôm nay 08:15
        //         nextAllowed = (nowLocal.TimeOfDay >= nightStart)
        //             ? nowLocal.Date.AddDays(1).Add(nightEnd)
        //             : nowLocal.Date.Add(nightEnd);
        //     }
        //     else if (inLunch)
        //     {
        //         // đang trong khung trưa → hôm nay 13:10
        //         nextAllowed = nowLocal.Date.Add(lunchEnd);
        //     }

        //     if (nextAllowed.HasValue)
        //     {
        //         var retryAfterSeconds = Math.Max(1, (int)(nextAllowed.Value - nowLocal).TotalSeconds);
        //         Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

        //         return StatusCode(403, new
        //         {
        //             ok = false,
        //             message = "Manual trigger bị khóa trong khung giờ 20:00–08:15 và 12:10–13:10 (giờ VN).",
        //             now = nowLocal.ToString("HH:mm:ss dd/MM/yyyy"),
        //             nextAllowed = nextAllowed.Value.ToString("HH:mm:ss dd/MM/yyyy")
        //         });
        //     }
        //     // =======================================================================

        //     // Thời điểm kết thúc cooldown (UTC) để trả về cho client
        //     DateTimeOffset? cooldownUntil = null;

        //     await _gate.WaitAsync();
        //     try
        //     {
        //         // Nếu đang trong cooldown => 429 Too Many Requests
        //         if (_cache.TryGetValue<DateTimeOffset>(COOL_KEY, out var until))
        //         {
        //             cooldownUntil = until;
        //             Response.Headers["Retry-After"] = Math.Max(1, (int)(until - DateTimeOffset.UtcNow).TotalSeconds).ToString();
        //             return StatusCode(429, new
        //             {
        //                 ok = false,
        //                 message = $"Manual trigger đang trong cooldown. Thử lại sau {COOLDOWN_MINUTES} phút.",
        //                 cooldownUntil = until.ToLocalTime().ToString("HH:mm:ss dd/MM/yyyy")
        //             });
        //         }

        //         // Đặt cooldown mới (absolute TTL 5’)
        //         cooldownUntil = DateTimeOffset.UtcNow.AddMinutes(COOLDOWN_MINUTES);
        //         _cache.Set(COOL_KEY, cooldownUntil.Value,
        //             new MemoryCacheEntryOptions
        //             {
        //                 AbsoluteExpiration = cooldownUntil
        //             });
        //     }
        //     finally
        //     {
        //         _gate.Release();
        //     }

        //     // Ngoài critical section: chạy logic dispatch như cũ
        //     try
        //     {
        //         var scheduler = await _schedulerFactory.GetScheduler();

        //         // 1) chạy fetch + operator ngay
        //         await scheduler.TriggerJob(new JobKey("ManualDispatcherJob"));

        //         // 2) schedule escalation one-off: +9' supervisor
        //         var supJob = JobBuilder.Create<EscalationDispatcherService>()
        //             .WithIdentity($"ManualEscalationSupervisor-{Guid.NewGuid()}")
        //             .UsingJobData("Role", "supervisor")
        //             .Build();

        //         var supTrigger = TriggerBuilder.Create()
        //             .StartAt(DateBuilder.FutureDate(9, IntervalUnit.Minute))
        //             .Build();

        //         await scheduler.ScheduleJob(supJob, supTrigger);

        //         // 3) schedule escalation one-off: +19' manager
        //         var mgrJob = JobBuilder.Create<EscalationDispatcherService>()
        //             .WithIdentity($"ManualEscalationManager-{Guid.NewGuid()}")
        //             .UsingJobData("Role", "manager")
        //             .Build();

        //         var mgrTrigger = TriggerBuilder.Create()
        //             .StartAt(DateBuilder.FutureDate(19, IntervalUnit.Minute))
        //             .Build();

        //         await scheduler.ScheduleJob(mgrJob, mgrTrigger);

        //         _logger.LogInformation("Manual API accepted at {time}: operator sent, escalation scheduled (+9'/+19').",
        //             DateTime.Now);

        //         return Ok(new
        //         {
        //             ok = true,
        //             message = $"Manual trigger accepted. Cooldown đến {cooldownUntil.Value.ToLocalTime():HH:mm:ss dd/MM/yyyy}.",
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Manual trigger failed. Clearing cooldown key.");
        //         // Nếu có lỗi, bỏ cooldown để gọi lại được
        //         _cache.Remove(COOL_KEY);

        //         return StatusCode(500, new
        //         {
        //             ok = false,
        //             message = "Có lỗi khi thực thi manual trigger.",
        //             error = ex.Message
        //         });
        //     }
        // }
    }
}
