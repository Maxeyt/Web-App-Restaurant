using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;
using Hangfire;
using MaxFood.Services;
using System.Text.Json;

namespace MaxFood.Pages.Notification
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(MaxFoodDBContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<UserViewModel> Users { get; set; } = new();
        public List<NotificationHistoryItem> SentNotifications { get; set; } = new();
        public List<NotificationTemplateViewModel> Templates { get; set; } = new();

        public class UserViewModel
        {
            public int AppUserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public int RoleId { get; set; }
            public string RoleName { get; set; } = string.Empty;
        }

        public class NotificationHistoryItem
        {
            public int NotificationId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? LongMessage { get; set; }
            public string? ActionLink { get; set; }
            public DateTime CreatedDate { get; set; }
            public bool IsRead { get; set; }
        }

        public class NotificationTemplateViewModel
        {
            public string TemplateType { get; set; } = string.Empty;
            public string TemplateName { get; set; } = string.Empty;
            public string DefaultTitle { get; set; } = string.Empty;
            public string DefaultShortMessage { get; set; } = string.Empty;
            public string DefaultLongMessage { get; set; } = string.Empty;
            public string IconClass { get; set; } = string.Empty;
            public string BadgeColor { get; set; } = string.Empty;
        }

        public class SendNotificationModel
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? LongMessage { get; set; }
            public string? ActionLink { get; set; }
            public List<RecipientItem> Recipients { get; set; } = new();
        }

        public class ScheduleNotificationModel
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? LongMessage { get; set; }
            public string? ActionLink { get; set; }
            public List<RecipientItem> Recipients { get; set; } = new();
            public string ScheduledDate { get; set; } = string.Empty;
        }

        public class RecipientItem
        {
            public string Type { get; set; } = string.Empty;
            public string? GroupType { get; set; }
            public int? UserId { get; set; }
            public string? UserName { get; set; }
        }

        public class ScheduledJobInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public DateTime ScheduledDate { get; set; }
        }

        private bool IsAdminOrManager()
        {
            int? roleId = HttpContext.Session.GetInt32("UserRole");
            return roleId == 4 || roleId == 3;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdminOrManager())
            {
                return RedirectToPage("/Authorization/Login");
            }

            var dbTemplates = await _context.NotificationTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            if (dbTemplates != null && dbTemplates.Count > 0)
            {
                Templates = dbTemplates.Select(t => new NotificationTemplateViewModel
                {
                    TemplateType = t.TemplateType,
                    TemplateName = t.TemplateName,
                    DefaultTitle = t.DefaultTitle,
                    DefaultShortMessage = t.DefaultShortMessage,
                    DefaultLongMessage = t.DefaultLongMessage,
                    IconClass = t.IconClass ?? "bi bi-bell",
                    BadgeColor = t.BadgeColor ?? "primary"
                }).ToList();
            }
            else
            {
                Templates = new List<NotificationTemplateViewModel>
                {
                    new() { TemplateType = "ready", TemplateName = "Заказ готов", IconClass = "bi bi-box", BadgeColor = "success",
                        DefaultTitle = "Заказ готов!", DefaultShortMessage = "Ваш заказ готов к выдаче!", DefaultLongMessage = "Ваш заказ полностью готов и ждёт вас." },
                    new() { TemplateType = "promo", TemplateName = "Акция", IconClass = "bi bi-megaphone", BadgeColor = "danger",
                        DefaultTitle = "Новая акция!", DefaultShortMessage = "Скидка 15% на всю продукцию!", DefaultLongMessage = "У нас новая акция: скидка 15% на всю продукцию!" },
                    new() { TemplateType = "bonus", TemplateName = "Бонусы", IconClass = "bi bi-gift", BadgeColor = "success",
                        DefaultTitle = "Начисление бонусов", DefaultShortMessage = "Вам начислено 250 бонусов!", DefaultLongMessage = "Поздравляем! Вам начислено 250 бонусов." },
                    new() { TemplateType = "review", TemplateName = "Отзыв", IconClass = "bi bi-star-fill", BadgeColor = "warning",
                        DefaultTitle = "Спасибо за отзыв", DefaultShortMessage = "Спасибо за ваш отзыв!", DefaultLongMessage = "Спасибо, что поделились мнением о MaxFood!" }
                };
            }

            Users = await _context.AppUsers
                .Include(u => u.UserProfile)
                .Include(u => u.Role)
                .Select(u => new UserViewModel
                {
                    AppUserId = u.AppUserId,
                    FullName = (u.UserProfile.FirstName + " " + u.UserProfile.LastName).Trim(),
                    Email = u.UserProfile.Email,
                    Phone = u.UserProfile.Phone,
                    RoleId = u.RoleId,
                    RoleName = u.Role.RoleName
                })
                .OrderBy(u => u.RoleId)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            SentNotifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedDate)
                .Take(50)
                .Select(n => new NotificationHistoryItem
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    LongMessage = n.LongMessage,
                    ActionLink = n.ActionLink,
                    CreatedDate = n.CreatedDate,
                    IsRead = n.IsRead
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSendNotification([FromBody] SendNotificationModel model)
        {
            try
            {
                if (!IsAdminOrManager())
                    return new JsonResult(new { success = false, message = "Недостаточно прав" });

                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Message))
                    return new JsonResult(new { success = false, message = "Заполните заголовок и текст" });

                if (model.Recipients == null || model.Recipients.Count == 0)
                    return new JsonResult(new { success = false, message = "Выберите получателя" });

                List<AppUser> targetUsers = new();

                foreach (var recipient in model.Recipients)
                {
                    if (recipient.Type == "group")
                    {
                        switch (recipient.GroupType)
                        {
                            case "users":
                                var users = await _context.AppUsers.Where(u => u.RoleId == 1).ToListAsync();
                                targetUsers.AddRange(users);
                                break;
                            case "managers":
                                var managers = await _context.AppUsers.Where(u => u.RoleId == 2).ToListAsync();
                                targetUsers.AddRange(managers);
                                break;
                            case "couriers":
                                var couriers = await _context.AppUsers.Where(u => u.RoleId == 3).ToListAsync();
                                targetUsers.AddRange(couriers);
                                break;
                            case "admins":
                                var admins = await _context.AppUsers.Where(u => u.RoleId == 4).ToListAsync();
                                targetUsers.AddRange(admins);
                                break;
                        }
                    }
                    else if (recipient.Type == "user" && recipient.UserId.HasValue)
                    {
                        var user = await _context.AppUsers.FindAsync(recipient.UserId.Value);
                        if (user != null) targetUsers.Add(user);
                    }
                }

                if (targetUsers.Count == 0)
                    return new JsonResult(new { success = false, message = "Нет получателей" });

                targetUsers = targetUsers.GroupBy(u => u.AppUserId).Select(g => g.First()).ToList();

                var notifications = targetUsers.Select(u => new MaxFood.Models.Notification
                {
                    AppUserId = u.AppUserId,
                    Title = model.Title,
                    Message = model.Message,
                    LongMessage = model.LongMessage,
                    ActionLink = model.ActionLink,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                }).ToList();

                await _context.Notifications.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = $"Отправлено {targetUsers.Count} получателям" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки");
                return new JsonResult(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnPostScheduleNotification([FromBody] ScheduleNotificationModel model)
        {
            try
            {
                if (!IsAdminOrManager())
                    return new JsonResult(new { success = false, message = "Недостаточно прав" });

                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Message))
                    return new JsonResult(new { success = false, message = "Заполните заголовок и текст" });

                if (model.Recipients == null || model.Recipients.Count == 0)
                    return new JsonResult(new { success = false, message = "Выберите получателя" });

                if (string.IsNullOrWhiteSpace(model.ScheduledDate))
                    return new JsonResult(new { success = false, message = "Выберите дату и время" });

                if (!DateTime.TryParse(model.ScheduledDate, out DateTime scheduledDate))
                    return new JsonResult(new { success = false, message = "Неверный формат даты" });

                if (scheduledDate <= DateTime.Now)
                    return new JsonResult(new { success = false, message = "Дата должна быть в будущем" });

                var simpleRecipients = model.Recipients.Select(r => new MaxFood.Services.NotificationService.SimpleRecipientItem
                {
                    Type = r.Type,
                    GroupType = r.GroupType,
                    UserId = r.UserId,
                    UserName = r.UserName
                }).ToList();

                TimeSpan delay = scheduledDate - DateTime.Now;

                string jobId = BackgroundJob.Schedule<NotificationService>(
                    service => service.SendScheduledNotificationAsync(
                        model.Title,
                        model.Message,
                        model.LongMessage,
                        model.ActionLink,
                        simpleRecipients),
                    delay);

                return new JsonResult(new { success = true, message = $"Запланировано на {scheduledDate:dd.MM.yyyy HH:mm}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка планирования");
                return new JsonResult(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        public IActionResult OnGetGetScheduledJobs()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var scheduledJobs = monitoringApi.ScheduledJobs(0, 100);
                var jobs = new List<ScheduledJobInfo>();

                foreach (var job in scheduledJobs)
                {
                    string title = ExtractTitleFromJob(job.Value.Job?.Args);
                    jobs.Add(new ScheduledJobInfo
                    {
                        Id = job.Key,
                        Title = title,
                        ScheduledDate = job.Value.EnqueueAt
                    });
                }
                return new JsonResult(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения списка");
                return new JsonResult(new List<ScheduledJobInfo>());
            }
        }

        public IActionResult OnPostCancelScheduledJob(string jobId)
        {
            try
            {
                BackgroundJob.Delete(jobId);
                return new JsonResult(new { success = true, message = "Отменено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отмены");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private static string ExtractTitleFromJob(IReadOnlyList<object>? args)
        {
            if (args == null || args.Count == 0) return "Уведомление";
            try
            {
                if (args.Count > 0 && args[0] != null)
                {
                    string title = args[0].ToString() ?? "Уведомление";
                    if (title.Length > 50) title = title[..50];
                    return title;
                }
            }
            catch { }
            return "Уведомление";
        }
    }
}