using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quartz;
using SigmaNotificationBackend.Jobs;

namespace SigmaNotificationBackend.Controllers
{
    /// <summary>
    /// API thủ công:
    ///   - Gọi MANUAL dispatcher: fetch dữ liệu + gửi operator NGAY LẬP TỨC
    ///   - Đặt 2 trigger động cho EscalationDispatcherService:
    ///       + now + 15 phút  → supervisor
    ///       + now + 30 phút  → manager
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ManualDispatchController : ControllerBase
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<ManualDispatchController> _logger;

        public ManualDispatchController(
            ISchedulerFactory schedulerFactory,
            ILogger<ManualDispatchController> logger)
        {
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        [HttpGet("trigger")]
        public async Task<IActionResult> TriggerManualDispatch()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            // 1) Trigger job thủ công chạy NGAY (fetch + send operator)
            var manualJobKey = new JobKey("ManualDispatcherJob");
            await scheduler.TriggerJob(manualJobKey);

            // 2) Schedule escalation supervisor (now + 15 phút)
            var supJob = JobBuilder.Create<EscalationDispatcherService>()
                .WithIdentity($"ManualEscalationSupervisor-{Guid.NewGuid()}")
                .Build();

            var supTrigger = TriggerBuilder.Create()
                .StartAt(DateBuilder.FutureDate(2, IntervalUnit.Minute))
                .Build();

            await scheduler.ScheduleJob(supJob, supTrigger);

            // 3) Schedule escalation manager (now + 30 phút)
            var mgrJob = JobBuilder.Create<EscalationDispatcherService>()
                .WithIdentity($"ManualEscalationManager-{Guid.NewGuid()}")
                .Build();

            var mgrTrigger = TriggerBuilder.Create()
                .StartAt(DateBuilder.FutureDate(30, IntervalUnit.Minute))
                .Build();

            await scheduler.ScheduleJob(mgrJob, mgrTrigger);

            _logger.LogInformation("Manual dispatch triggered at {time}. Escalation scheduled at +15m and +30m.",
                DateTime.Now);

            return Ok(new
            {
                success = true,
                message = "Manual dispatcher triggered. Escalation will run at +15m and +30m from now."
            });
        }
    }
}
