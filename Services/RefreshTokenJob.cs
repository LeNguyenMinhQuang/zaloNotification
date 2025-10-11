using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using SigmaNotificationBackend.Services;


namespace SigmaNotificationBackend.Jobs
{
    public class RefreshTokenJob : IJob
    {
        private readonly TokenStorageService _zaloAuthService;
        private readonly ILogger<RefreshTokenJob> _logger;

        public RefreshTokenJob(TokenStorageService zaloAuthService, ILogger<RefreshTokenJob> logger)
        {
            _zaloAuthService = zaloAuthService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("🔁 Running RefreshTokenJob...");
            await _zaloAuthService.RefreshTokensAsync();
            _logger.LogInformation("✅ RefreshTokenJob done.");
        }
    }
}
