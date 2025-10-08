using System.Collections.Generic;
using System.Threading.Tasks;

namespace SigmaNotificationBackend.Services
{
    public interface IZaloSendService
    {
        Task SendMessageToUserAsync(string userId, string message);
        Task SendDepartmentSelectionTemplateAsync(string userId);
        Task HandleDepartmentSelectionAsync(string userId, string messageText);

        // Giữ nguyên chữ ký cũ (dùng khi gửi tin gộp)
        Task SendCombinedProductionMessageTemplateAsync(string userId, List<dynamic> messages, DateTime sendTime, string? extraNote = null);

        // ✅ NEW: update nhiều messageId cùng lúc khi người dùng bấm “Đã xem”
        Task MarkMessagesAsViewedByUserAsync(IEnumerable<int> messageIds, string userId);

        // Back-compat: nếu chỗ nào trong code gọi single-id vẫn hoạt động
        Task MarkMessageAsViewedByUserAsync(int messageId, string userId);
    }
}