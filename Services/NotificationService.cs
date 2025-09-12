// using Microsoft.EntityFrameworkCore;
// using SigmaNotificationBackend.Data;
// using SigmaNotificationBackend.Models;

// public class NotificationService
// {
//     private readonly AppDbContext _context;

//     public NotificationService(AppDbContext context)
//     {
//         _context = context;
//     }

//     // Phương thức lấy tất cả thông báo của người dùng
//     public async Task<List<SVN_Messages>> GetMessagesForUser(int departmentId)
//     {
//         return await _context.SVN_Messages
//             .Where(m => m.to_department == departmentId)
//             .OrderByDescending(m => m.create_at)
//             .Take(10) // get top 5
//             .ToListAsync();
//     }


//     // Phương thức lấy chi tiết của một thông báo
//     public async Task<SVN_Messages?> GetMessageByIdAndUser(int messageId, int departmentId)
//     {
//         return await _context.SVN_Messages
//             .FirstOrDefaultAsync(m => m.id_message == messageId && m.to_department == departmentId);
//     }


//     // Phương thức xóa vĩnh viễn thông báo
//     public async Task<bool> DeleteMessage(int messageId, int departmentId)
//     {
//         var message = await _context.SVN_Messages
//             .FirstOrDefaultAsync(m => m.id_message == messageId && m.to_department == departmentId);

//         if (message == null)
//             return false;

//         _context.SVN_Messages.Remove(message);
//         await _context.SaveChangesAsync();
//         return true;
//     }



//     // Phương thức đánh dấu đã đọc thông báo v1
//     public async Task<bool> MarkMessageAsRead(int messageId, int departmentId)
//     {
//         var message = await _context.SVN_Messages
//             .FirstOrDefaultAsync(m => m.id_message == messageId && m.to_department == departmentId);

//         if (message == null)
//             return false;

//         message.viewed = 1;
//         await _context.SaveChangesAsync();
//         return true;
//     }

//     // Phương thức đánh dấu đã đọc thông báo v2
//     public async Task<SVN_Messages?> GetAndMarkAsRead(int messageId, int departmentId)
//     {
//         var message = await _context.SVN_Messages
//             .FirstOrDefaultAsync(m => m.id_message == messageId && m.to_department == departmentId);

//         if (message == null)
//             return null;

//         if (message.viewed == 0)
//         {
//             message.viewed = 1;
//             await _context.SaveChangesAsync();
//         }

//         return message;
//     }
// }
