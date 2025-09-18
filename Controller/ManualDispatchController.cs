using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        public ManualDispatchController(
            ISchedulerFactory schedulerFactory,
            ILogger<ManualDispatchController> logger)
        {
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        [HttpGet("trigger")]
        public async Task<IActionResult> Trigger()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            // 1) chạy fetch + operator ngay
            await scheduler.TriggerJob(new JobKey("ManualDispatcherJob"));

            // 2) schedule escalation one-off: +2' supervisor
            var supJob = JobBuilder.Create<EscalationDispatcherService>()
                .WithIdentity($"ManualEscalationSupervisor-{Guid.NewGuid()}")
                .UsingJobData("Role", "supervisor")
                .Build();

            var supTrigger = TriggerBuilder.Create()
                .StartAt(DateBuilder.FutureDate(2, IntervalUnit.Minute)) // test 2'
                .Build();

            await scheduler.ScheduleJob(supJob, supTrigger);

            // 3) schedule escalation one-off: +3' manager
            var mgrJob = JobBuilder.Create<EscalationDispatcherService>()
                .WithIdentity($"ManualEscalationManager-{Guid.NewGuid()}")
                .UsingJobData("Role", "manager")
                .Build();

            var mgrTrigger = TriggerBuilder.Create()
                .StartAt(DateBuilder.FutureDate(3, IntervalUnit.Minute)) // test 3'
                .Build();

            await scheduler.ScheduleJob(mgrJob, mgrTrigger);

            _logger.LogInformation("Manual API called at {time}: operator sent, escalation scheduled (+2'/+3').",
                DateTime.Now);

            return Ok(new { ok = true });
        }
    }
}
