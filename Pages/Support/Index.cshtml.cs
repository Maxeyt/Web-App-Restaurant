using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;
using MaxFood.Services;

namespace MaxFood.Pages.Support
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly NotificationService _notificationService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(MaxFoodDBContext context, NotificationService notificationService, ILogger<IndexModel> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public IList<MaxFood.Models.Support> Messages { get; set; } = new List<MaxFood.Models.Support>();
        public IList<MaxFood.Models.Support> FavoriteMessages { get; set; } = new List<MaxFood.Models.Support>();
        public int CurrentUserId { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsManager { get; set; }
        public string Section { get; set; } = "personal";
        public string ActiveTab { get; set; } = "messages";
        public IList<ChatUser> PersonalChats { get; set; } = new List<ChatUser>();
        public IList<ChatUser> SupportChats { get; set; } = new List<ChatUser>();
        public int? SelectedUserId { get; set; }
        public string? ChatUserName { get; set; }
        public bool IsFavoriteTab { get; set; }
        public bool IsSupportChat { get; set; }
        public int SupportUnreadCount { get; set; }
        public bool IsHelpStarted { get; set; } = false;
        public Dictionary<int, int> UnreadCounts { get; set; } = new();
        public Dictionary<int, int> SupportUnreadCounts { get; set; } = new();

        public class ChatUser
        {
            public int AppUserId { get; set; }
            public string? FullName { get; set; }
            public string? LastMessage { get; set; }
            public DateTime? LastMessageDate { get; set; }
            public int UnreadCount { get; set; }
        }

        public class MarkAsReadRequest
        {
            public int? UserId { get; set; }
            public bool IsSupportChat { get; set; }
        }

        public async Task OnGetAsync(string tab = "messages", string section = "personal", int? userId = null, int? favorite = null, int? support = null)
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid))
            {
                IsAuthenticated = false;
                return;
            }
            IsAuthenticated = true;
            CurrentUserId = int.Parse(uid);

            var roleId = HttpContext.Session.GetInt32("UserRole") ?? 0;
            IsAdmin = roleId == 4;
            IsManager = roleId == 3;

            ActiveTab = tab;
            Section = section;
            IsFavoriteTab = favorite == 1;
            IsSupportChat = support == 1;
            SelectedUserId = userId;

            await MarkMessagesAsReadInCurrentChat();
            await LoadMessages();
            await LoadPersonalChats();

            if (IsAdmin || IsManager)
            {
                await LoadSupportChats();
            }

            await UpdateSupportUnreadCount();
        }

        private async Task MarkMessagesAsReadInCurrentChat()
        {
            // Личный чат
            if (SelectedUserId.HasValue && SelectedUserId != CurrentUserId && !IsSupportChat && !IsFavoriteTab)
            {
                var unreadMessages = await _context.Supports
                    .Where(s => !s.IsSupport
                                && s.AppUserId == SelectedUserId.Value
                                && s.HelperId == CurrentUserId
                                && !s.IsRead
                                && s.IsFromUser)
                    .ToListAsync();

                if (unreadMessages.Any())
                {
                    foreach (var msg in unreadMessages) msg.IsRead = true;
                    await _context.SaveChangesAsync();
                    await MarkRelatedNotificationsAsRead(SelectedUserId.Value, false);
                }
            }

            // Чат поддержки для админа/менеджера
            if (IsSupportChat && (IsAdmin || IsManager) && SelectedUserId.HasValue)
            {
                var unreadMessages = await _context.Supports
                    .Where(s => s.IsSupport && s.AppUserId == SelectedUserId.Value && !s.IsRead)
                    .ToListAsync();

                if (unreadMessages.Any())
                {
                    foreach (var msg in unreadMessages) msg.IsRead = true;
                    await _context.SaveChangesAsync();
                    await MarkRelatedNotificationsAsRead(SelectedUserId.Value, true);
                }
            }

            // Чат поддержки для пользователя
            if (IsSupportChat && !IsAdmin && !IsManager)
            {
                var unreadMessages = await _context.Supports
                    .Where(s => s.IsSupport && s.AppUserId == CurrentUserId && !s.IsFromUser && !s.IsRead)
                    .ToListAsync();

                if (unreadMessages.Any())
                {
                    foreach (var msg in unreadMessages) msg.IsRead = true;
                    await _context.SaveChangesAsync();
                    await MarkRelatedNotificationsAsRead(CurrentUserId, true);
                }
            }
        }

        private async Task MarkRelatedNotificationsAsRead(int userId, bool isSupportChat)
        {
            List<MaxFood.Models.Notification> notificationsToMark = new();

            if (isSupportChat)
            {
                notificationsToMark = await _context.Notifications
                    .Where(n => n.AppUserId == userId && !n.IsRead &&
                        (n.Title.Contains("Поддержка") || n.Title.Contains("Ответ") ||
                         n.Title.Contains("обращение") || n.Title.Contains("администратор")))
                    .ToListAsync();
            }
            else
            {
                notificationsToMark = await _context.Notifications
                    .Where(n => n.AppUserId == userId && !n.IsRead &&
                        (n.Title.Contains("сообщение") || n.Title.Contains("Сообщение")))
                    .ToListAsync();
            }

            if (notificationsToMark.Any())
            {
                foreach (var n in notificationsToMark) n.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        private async Task UpdateSupportUnreadCount()
        {
            if (IsAdmin || IsManager)
            {
                SupportUnreadCount = await _context.Supports
                    .Where(s => s.IsSupport && s.IsFromUser && !s.IsRead && s.HelperId == CurrentUserId)
                    .CountAsync();
            }
            else
            {
                SupportUnreadCount = await _context.Supports
                    .Where(s => s.IsSupport && s.AppUserId == CurrentUserId && !s.IsFromUser && !s.IsRead)
                    .CountAsync();
            }
        }

        private async Task LoadMessages()
        {
            // Личный чат
            if (SelectedUserId.HasValue && SelectedUserId != CurrentUserId && !IsSupportChat && !IsFavoriteTab)
            {
                Messages = await _context.Supports
                    .Where(s => !s.IsSupport && ((s.AppUserId == CurrentUserId && s.HelperId == SelectedUserId) ||
                                                  (s.AppUserId == SelectedUserId && s.HelperId == CurrentUserId)))
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                var user = await _context.AppUsers.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.AppUserId == SelectedUserId);
                ChatUserName = user?.UserProfile != null ? $"{user.UserProfile.FirstName} {user.UserProfile.LastName}" : $"Пользователь {SelectedUserId}";
            }
            // Чат поддержки для админа/менеджера
            else if (IsSupportChat && (IsAdmin || IsManager) && SelectedUserId.HasValue)
            {
                Messages = await _context.Supports
                    .Where(s => s.IsSupport && s.AppUserId == SelectedUserId.Value)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();

                var user = await _context.AppUsers.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.AppUserId == SelectedUserId);
                ChatUserName = user?.UserProfile != null ? $"{user.UserProfile.FirstName} {user.UserProfile.LastName}" : $"Пользователь {SelectedUserId}";
                IsHelpStarted = await _context.Supports.AnyAsync(s => s.IsSupport && s.AppUserId == SelectedUserId.Value && !s.IsFromUser);
            }
            // Чат поддержки для пользователя
            else if (IsSupportChat)
            {
                Messages = await _context.Supports
                    .Where(s => s.IsSupport && s.AppUserId == CurrentUserId)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();
                ChatUserName = "Поддержка";
            }
            // Избранное
            else if (IsFavoriteTab)
            {
                FavoriteMessages = await _context.Supports
                    .Where(s => s.AppUserId == CurrentUserId && s.IsFavorite)
                    .OrderBy(s => s.CreatedAt)
                    .ToListAsync();
            }
        }

        private async Task LoadPersonalChats()
        {
            var chatUserIds = await _context.Supports
                .Where(s => !s.IsSupport && (s.AppUserId == CurrentUserId || s.HelperId == CurrentUserId))
                .Select(s => s.AppUserId == CurrentUserId ? s.HelperId : s.AppUserId)
                .Where(id => id.HasValue && id != CurrentUserId)
                .Distinct()
                .Select(id => id!.Value)
                .ToListAsync();

            var personalChatList = new List<ChatUser>();
            UnreadCounts = new Dictionary<int, int>();

            foreach (var chatId in chatUserIds)
            {
                var user = await _context.AppUsers.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.AppUserId == chatId);
                var lastMsg = await _context.Supports
                    .Where(s => !s.IsSupport && ((s.AppUserId == CurrentUserId && s.HelperId == chatId) ||
                                                  (s.AppUserId == chatId && s.HelperId == CurrentUserId)))
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.Supports
                    .Where(s => !s.IsSupport && s.AppUserId == chatId && s.HelperId == CurrentUserId && !s.IsRead && s.IsFromUser)
                    .CountAsync();

                personalChatList.Add(new ChatUser
                {
                    AppUserId = chatId,
                    FullName = user?.UserProfile != null ? $"{user.UserProfile.FirstName} {user.UserProfile.LastName}" : chatId.ToString(),
                    LastMessage = lastMsg?.Text?.Length > 50 ? lastMsg.Text.Substring(0, 50) + "..." : lastMsg?.Text,
                    LastMessageDate = lastMsg?.CreatedAt,
                    UnreadCount = unreadCount
                });
                UnreadCounts[chatId] = unreadCount;
            }

            PersonalChats = personalChatList.OrderByDescending(c => c.LastMessageDate).ToList();
        }

        private async Task LoadSupportChats()
        {
            var supportChatUserIds = await _context.Supports
                .Where(s => s.IsSupport)
                .Select(s => s.AppUserId)
                .Distinct()
                .ToListAsync();

            var supportChatList = new List<ChatUser>();

            foreach (var chatId in supportChatUserIds)
            {
                var user = await _context.AppUsers.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.AppUserId == chatId);
                var lastMsg = await _context.Supports
                    .Where(s => s.IsSupport && s.AppUserId == chatId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.Supports
                    .Where(s => s.IsSupport && s.AppUserId == chatId && !s.IsRead && s.IsFromUser)
                    .CountAsync();

                supportChatList.Add(new ChatUser
                {
                    AppUserId = chatId,
                    FullName = user?.UserProfile != null ? $"{user.UserProfile.FirstName} {user.UserProfile.LastName}" : chatId.ToString(),
                    LastMessage = lastMsg?.Text?.Length > 50 ? lastMsg.Text.Substring(0, 50) + "..." : lastMsg?.Text,
                    LastMessageDate = lastMsg?.CreatedAt,
                    UnreadCount = unreadCount
                });
            }

            SupportChats = supportChatList.OrderByDescending(c => c.LastMessageDate).ToList();
        }

        // AJAX Handlers
        public async Task<IActionResult> OnGetUnreadCount()
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return new JsonResult(new { unreadCount = 0 });

            int currentUserId = int.Parse(uid);
            var roleId = HttpContext.Session.GetInt32("UserRole") ?? 0;
            bool isAdminOrManager = (roleId == 4 || roleId == 3);

            int unreadCount = isAdminOrManager
                ? await _context.Supports.Where(s => s.IsSupport && s.IsFromUser && !s.IsRead && s.HelperId == currentUserId).CountAsync()
                : await _context.Supports.Where(s => s.IsSupport && !s.IsFromUser && !s.IsRead && s.AppUserId == currentUserId).CountAsync();

            return new JsonResult(new { unreadCount });
        }

        public async Task<IActionResult> OnPostMarkAsRead([FromBody] MarkAsReadRequest request)
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false });

            int currentUserId = int.Parse(uid);
            var roleId = HttpContext.Session.GetInt32("UserRole") ?? 0;
            bool isAdminOrManager = (roleId == 4 || roleId == 3);

            try
            {
                if (request.IsSupportChat && isAdminOrManager && request.UserId.HasValue)
                {
                    var unread = await _context.Supports.Where(s => s.IsSupport && s.AppUserId == request.UserId.Value && !s.IsRead).ToListAsync();
                    foreach (var msg in unread) msg.IsRead = true;
                    await _context.SaveChangesAsync();
                    await MarkRelatedNotificationsAsRead(request.UserId.Value, true);
                    return new JsonResult(new { success = true });
                }
                else if (!request.IsSupportChat && request.UserId.HasValue && request.UserId.Value != currentUserId)
                {
                    var unread = await _context.Supports
                        .Where(s => !s.IsSupport && s.AppUserId == request.UserId.Value && s.HelperId == currentUserId && !s.IsRead && s.IsFromUser)
                        .ToListAsync();
                    foreach (var msg in unread) msg.IsRead = true;
                    await _context.SaveChangesAsync();
                    await MarkRelatedNotificationsAsRead(request.UserId.Value, false);
                    return new JsonResult(new { success = true });
                }
                return new JsonResult(new { success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в MarkAsRead");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // Отправка сообщений
        public async Task<IActionResult> OnPostSendMessageAsync(int userId, string text)
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return RedirectToPage("/Authorization/Login");
            int currentUserId = int.Parse(uid);

            var support = new MaxFood.Models.Support
            {
                AppUserId = currentUserId,
                HelperId = userId,
                Text = text,
                IsFromUser = true,
                IsFavorite = false,
                IsSupport = false,
                ChatType = "personal",
                CreatedAt = DateTime.Now
            };

            _context.Supports.Add(support);
            await _context.SaveChangesAsync();
            await _notificationService.SendNewPrivateMessageNotificationAsync(support.SupportId, userId, text, currentUserId);
            return RedirectToPage(new { tab = "messages", userId });
        }

        public async Task<IActionResult> OnPostSendNoteAsync(string text)
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return RedirectToPage("/Authorization/Login");
            int currentUserId = int.Parse(uid);

            var note = new MaxFood.Models.Support
            {
                AppUserId = currentUserId,
                Text = text,
                IsFromUser = true,
                IsFavorite = true,
                IsSupport = false,
                ChatType = "personal",
                CreatedAt = DateTime.Now
            };

            _context.Supports.Add(note);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { tab = "messages", favorite = 1 });
        }

        public async Task<IActionResult> OnPostSendToSupportAsync(string text)
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return RedirectToPage("/Authorization/Login");
            int id = int.Parse(uid);

            var support = new MaxFood.Models.Support
            {
                AppUserId = id,
                Text = text,
                IsFromUser = true,
                IsFavorite = false,
                IsSupport = true,
                ChatType = "support",
                CreatedAt = DateTime.Now
            };

            _context.Supports.Add(support);
            await _context.SaveChangesAsync();
            await _notificationService.SendNewMessageNotificationAsync(support.SupportId, id, text);
            return RedirectToPage(new { tab = "messages", support = 1 });
        }

        public async Task<IActionResult> OnPostStartHelpAsync(int userId)
        {
            var roleId = HttpContext.Session.GetInt32("UserRole") ?? 0;
            if (roleId != 4 && roleId != 3) return RedirectToPage("/Authorization/Login");

            int helperId = int.Parse(HttpContext.Session.GetString("UserId")!);

            bool alreadyStarted = await _context.Supports
                .AnyAsync(s => s.IsSupport && s.AppUserId == userId && !s.IsFromUser);

            if (!alreadyStarted)
            {
                var systemMessage = new MaxFood.Models.Support
                {
                    AppUserId = userId,
                    HelperId = helperId,
                    Text = "🟢 Сотрудник поддержки начал диалог",
                    IsFromUser = false,
                    IsFavorite = false,
                    IsSupport = true,
                    ChatType = "support",
                    CreatedAt = DateTime.Now
                };
                _context.Supports.Add(systemMessage);
                await _context.SaveChangesAsync();
                await _notificationService.SendAdminReplyNotificationAsync(systemMessage.SupportId, userId, systemMessage.Text, helperId);
            }

            return RedirectToPage(new { tab = "support", userId, support = 1 });
        }

        // ИСПРАВЛЕНО: ответ поддержки — всегда от лица поддержки (IsFromUser = false)
        public async Task<IActionResult> OnPostAdminReplyAsync(int userId, string text)
        {
            var roleId = HttpContext.Session.GetInt32("UserRole") ?? 0;
            if (roleId != 4 && roleId != 3) return RedirectToPage("/Authorization/Login");

            int helperId = int.Parse(HttpContext.Session.GetString("UserId")!);

            // ВСЕГДА IsFromUser = false (сообщение от поддержки)
            var support = new MaxFood.Models.Support
            {
                AppUserId = userId,
                HelperId = helperId,
                Text = text,
                IsFromUser = false,
                IsFavorite = false,
                IsSupport = true,
                ChatType = "support",
                CreatedAt = DateTime.Now
            };

            _context.Supports.Add(support);
            await _context.SaveChangesAsync();

            // Уведомление получает пользователь (даже если это тот же админ — сам себе)
            if (userId != helperId)
            {
                await _notificationService.SendAdminReplyNotificationAsync(support.SupportId, userId, text, helperId);
            }
            else
            {
                // Если админ отвечает сам себе — уведомление всё равно отправляем
                await _notificationService.SendAdminReplyNotificationAsync(support.SupportId, userId, text, helperId);
                _logger.LogInformation("Админ {HelperId} ответил сам себе в чат поддержки", helperId);
            }

            return RedirectToPage(new { tab = "support", userId, support = 1 });
        }
    }
}