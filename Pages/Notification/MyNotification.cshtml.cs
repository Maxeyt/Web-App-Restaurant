using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Notification
{
    public class MyNotificationModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly ILogger<MyNotificationModel> _logger;

        public MyNotificationModel(MaxFoodDBContext context, ILogger<MyNotificationModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<MaxFood.Models.Notification> Notifications { get; set; } = new();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        public string? FilterStatus { get; set; }
        public string? SearchTerm { get; set; }

        public bool IsAuthenticated { get; set; }

        public async Task<IActionResult> OnGetAsync(int? page, string? filter, string? search)
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                IsAuthenticated = false;
                return Page();
            }

            IsAuthenticated = true;
            CurrentPage = page ?? 1;
            FilterStatus = filter ?? "all";
            SearchTerm = search;

            var query = _context.Notifications.Where(n => n.AppUserId == userId);

            if (FilterStatus == "unread")
            {
                query = query.Where(n => !n.IsRead);
            }
            else if (FilterStatus == "read")
            {
                query = query.Where(n => n.IsRead);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(n => n.Title.Contains(SearchTerm) || n.Message.Contains(SearchTerm));
            }

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            Notifications = await query
                .OrderByDescending(n => n.CreatedDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetCount()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return new JsonResult(new { count = 0 });
            }

            var count = await _context.Notifications
                .Where(n => n.AppUserId == userId && !n.IsRead)
                .CountAsync();

            return new JsonResult(new { count = count });
        }

        public async Task<IActionResult> OnGetFilterCounts()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return new JsonResult(new { all = 0, unread = 0, read = 0 });
            }

            var all = await _context.Notifications.Where(n => n.AppUserId == userId).CountAsync();
            var unread = await _context.Notifications.Where(n => n.AppUserId == userId && !n.IsRead).CountAsync();
            var read = await _context.Notifications.Where(n => n.AppUserId == userId && n.IsRead).CountAsync();

            return new JsonResult(new { all, unread, read });
        }

        public async Task<IActionResult> OnPostMarkAsRead([FromBody] MarkAsReadModel model)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return new JsonResult(new { success = false, message = "Не авторизован" });
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == model.NotificationId && n.AppUserId == userId);

                if (notification == null)
                {
                    return new JsonResult(new { success = false, message = "Уведомление не найдено" });
                }

                if (notification.Title.Contains("Поддержка") || notification.Title.Contains("Ответ"))
                {
                    var unreadSupportMessages = await _context.Supports
                        .Where(s => s.IsSupport && s.AppUserId == userId && !s.IsFromUser && !s.IsRead)
                        .ToListAsync();

                    foreach (var msg in unreadSupportMessages)
                    {
                        msg.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка при отметке уведомления как прочитанного");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostMarkAllAsRead()
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return new JsonResult(new { success = false, message = "Не авторизован" });
                }

                var unreadNotifications = await _context.Notifications
                    .Where(n => n.AppUserId == userId && !n.IsRead)
                    .ToListAsync();

                int count = unreadNotifications.Count;

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = $"Отмечено {count} уведомлений" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка при отметке всех уведомлений");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public class MarkAsReadModel
        {
            public int NotificationId { get; set; }
        }
    }
}