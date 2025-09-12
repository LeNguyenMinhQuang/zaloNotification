// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using SigmaNotificationBackend.Models;
// using SigmaNotificationBackend.Services;

// namespace SigmaNotificationBackend.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class NotificationController : ControllerBase
//     {
//         private readonly NotificationService _notificationService;

//         public NotificationController(NotificationService notificationService)
//         {
//             _notificationService = notificationService;
//         }


//         // Trả về tất cả thông báo của người dùng
//         [Authorize]
//         [HttpGet("GetUserNotifications")]
//         [Route("/api/notifications/GetUserNotifications")]
//         public async Task<IActionResult> GetUserNotifications()
//         {
//             var user = HttpContext.Items["CurrentUser"] as SVN_Users;

//             if (user == null)
//                 return Unauthorized(new { message = "User not found!" });

//             var messages = await _notificationService.GetMessagesForUser(user.id_department);

//             return Ok(new
//             {
//                 count = messages.Count,
//                 data = messages.Select(m => new
//                 {
//                     id = m.id_message,
//                     content = m.content,
//                     type_message = m.type_message,
//                     create_at = m.create_at,
//                     viewed = m.viewed,
//                     to_department = m.to_department
//                 })
//             });
//         }


//         // Trả về chi tiết một thông báo
//         [Authorize]
//         [HttpGet("GetUserNotificationByID")]
//         [Route("/api/Notifications/GetUserNotificationByID")]
//         public async Task<IActionResult> GetUserNotificationByID([FromQuery] int id)
//         {
//             var user = HttpContext.Items["CurrentUser"] as SVN_Users;

//             if (user == null)
//                 return Unauthorized(new { message = "User not found!" });

//             var message = await _notificationService.GetAndMarkAsRead(id, user.id_department);

//             if (message == null)
//                 return NotFound(new { message = "Notification not found!" });

//             return Ok(new
//             {
//                 id = message.id_message,
//                 content = message.content,
//                 type_message = message.type_message,
//                 create_at = message.create_at,
//                 viewed = message.viewed,
//                 to_department = message.to_department
//             });
//         }


//         // Đánh dấu thông báo đã đọc
//         [Authorize]
//         [HttpPost("MarkAsRead")]
//         [Route("/api/Notifications/MarkAsRead")]
//         public async Task<IActionResult> MarkAsRead([FromQuery] int id)
//         {
//             var user = HttpContext.Items["CurrentUser"] as SVN_Users;

//             if (user == null)
//                 return Unauthorized(new { message = "User not found!" });

//             var result = await _notificationService.MarkMessageAsRead(id, user.id_department);

//             if (!result)
//                 return NotFound(new { message = "Notification not found!" });

//             return Ok(new { message = "Đã đọc thông báo!" });
//         }


//         // Xóa một thông báo
//         [Authorize]
//         [HttpDelete("DeleteNotification")]
//         [Route("/api/Notifications/DeleteNotification")]
//         public async Task<IActionResult> DeleteNotification([FromQuery] int id)
//         {
//             var user = HttpContext.Items["CurrentUser"] as SVN_Users;

//             if (user == null)
//                 return Unauthorized(new { message = "User not found!" });

//             var result = await _notificationService.DeleteMessage(id, user.id_department);

//             if (!result)
//                 return NotFound(new { message = "Notification not found!" });

//             return Ok(new { message = "Thông báo đã được xoá!" });
//         }

//     }
// }
