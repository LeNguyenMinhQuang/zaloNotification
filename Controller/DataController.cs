// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Mvc;
// using SigmaNotificationBackend.Controllers;

// namespace SigmaNotificationBackend.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]

//     public class DataController : ControllerBase
//     {
//         private readonly DashboardService _dashboardService;

//         public DataController(DashboardService dashboardService)
//         {
//             _dashboardService = dashboardService;
//         }


//         [HttpGet("GetDashboardData")]
//         public async Task<IActionResult> GetDashboardData([FromQuery] string? date)
//         {
//             var data = await _dashboardService.GetDashboardDataByDate(date);
//             return Ok(data);
//         }

//         [HttpPost("SyncDashboard")]
//         public async Task<IActionResult> SyncDashboard([FromBody] List<DashBoardSummaryDto> dtos)
//         {
//             var messages = await _dashboardService.GenerateAndSaveWarningsAsync(dtos);

//             return Ok(new
//             {
//                 message = "Đã tạo cảnh báo thành công",
//                 count = messages.Count,
//                 data = messages
//             });
//         }

//     }
// }

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SigmaNotificationBackend.Controllers;
using SigmaNotificationBackend.Models;            // 👈 Thêm: dùng DailyTarget
using SigmaNotificationBackend.Services;          // (nếu chưa có sẵn trong project của bạn)

namespace SigmaNotificationBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly DashboardService _dashboardService;

        public DataController(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("GetDashboardData")]
        public async Task<IActionResult> GetDashboardData([FromQuery] string? date)
        {
            var data = await _dashboardService.GetDashboardDataByDate(date);
            return Ok(data);
        }

        [HttpPost("SyncDashboard")]
        public async Task<IActionResult> SyncDashboard([FromBody] List<DailyTarget> rows)   // 👈 Đổi: DashBoardSummaryDto -> DailyTarget
        {
            var messages = await _dashboardService.GenerateAndSaveWarningsAsync(rows);      // 👈 Gọi theo kiểu mới

            return Ok(new
            {
                message = "Đã tạo cảnh báo thành công",
                count = messages.Count,
                data = messages
            });
        }
    }
}
