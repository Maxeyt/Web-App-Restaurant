using MaxFood.Data;
using MaxFood.Models;
using Microsoft.EntityFrameworkCore;

namespace MaxFood.Services
{
    public class NotificationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IServiceProvider serviceProvider, ILogger<NotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public class SimpleRecipientItem
        {
            public string Type { get; set; } = string.Empty;
            public string? GroupType { get; set; }
            public int? UserId { get; set; }
            public string? UserName { get; set; }
        }

        public async Task SendScheduledNotificationAsync(
            string title,
            string message,
            string? longMessage,
            string? actionLink,
            List<SimpleRecipientItem> recipients)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaxFoodDBContext>();

                List<AppUser> targetUsers = new();

                foreach (var recipient in recipients)
                {
                    if (recipient.Type == "group")
                    {
                        switch (recipient.GroupType)
                        {
                            case "users":
                                var users = await context.AppUsers.Where(u => u.RoleId == 1).ToListAsync();
                                targetUsers.AddRange(users);
                                break;
                            case "managers":
                                var managers = await context.AppUsers.Where(u => u.RoleId == 2).ToListAsync();
                                targetUsers.AddRange(managers);
                                break;
                            case "couriers":
                                var couriers = await context.AppUsers.Where(u => u.RoleId == 3).ToListAsync();
                                targetUsers.AddRange(couriers);
                                break;
                            case "admins":
                                var admins = await context.AppUsers.Where(u => u.RoleId == 4).ToListAsync();
                                targetUsers.AddRange(admins);
                                break;
                        }
                    }
                    else if (recipient.Type == "user" && recipient.UserId.HasValue)
                    {
                        var user = await context.AppUsers.FindAsync(recipient.UserId.Value);
                        if (user != null) targetUsers.Add(user);
                    }
                }

                if (targetUsers.Count == 0) return;

                targetUsers = targetUsers.GroupBy(u => u.AppUserId).Select(g => g.First()).ToList();

                var notifications = targetUsers.Select(u => new MaxFood.Models.Notification
                {
                    AppUserId = u.AppUserId,
                    Title = title,
                    Message = message,
                    LongMessage = longMessage,
                    ActionLink = actionLink,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                }).ToList();

                await context.Notifications.AddRangeAsync(notifications);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Отложенное уведомление отправлено {targetUsers.Count} получателям: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке отложенного уведомления: {title}");
            }
        }

        // Новое сообщение в чате поддержки от пользователя → уведомление админам/менеджерам
        public async Task SendNewMessageNotificationAsync(int supportId, int userId, string messageText)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaxFoodDBContext>();

                var adminsAndManagers = await context.AppUsers
                    .Where(u => u.RoleId == 4 || u.RoleId == 2)
                    .ToListAsync();

                var user = await context.AppUsers
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.AppUserId == userId);

                var userName = user?.UserProfile?.FirstName ?? "Пользователь";

                foreach (var admin in adminsAndManagers)
                {
                    var notification = new MaxFood.Models.Notification
                    {
                        AppUserId = admin.AppUserId,
                        Title = "Новое сообщение в поддержку",
                        Message = $"{userName} написал(а): {messageText}",
                        LongMessage = $"Пользователь {userName} написал сообщение в поддержку:\n\n{messageText}\n\nПерейдите в раздел поддержки, чтобы ответить.",
                        ActionLink = $"/Support/Index?tab=support&userId={userId}&support=1",
                        CreatedDate = DateTime.Now,
                        IsRead = false
                    };
                    await context.Notifications.AddAsync(notification);
                }

                await context.SaveChangesAsync();
                _logger.LogInformation($"Уведомление о новом сообщении отправлено {adminsAndManagers.Count} администраторам/менеджерам");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о новом сообщении");
            }
        }

        // Личное сообщение (перегрузка с 3 параметрами)
        public async Task SendNewPrivateMessageNotificationAsync(int supportId, int receiverId, string messageText)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaxFoodDBContext>();

                var sender = await context.AppUsers
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.AppUserId == supportId);

                var senderName = sender?.UserProfile?.FirstName ?? "Пользователь";

                var notification = new MaxFood.Models.Notification
                {
                    AppUserId = receiverId,
                    Title = "Новое личное сообщение",
                    Message = $"{senderName} написал(а) вам: {messageText}",
                    LongMessage = $"Пользователь {senderName} написал вам личное сообщение:\n\n{messageText}\n\nПерейдите в раздел сообщений, чтобы ответить.",
                    ActionLink = $"/Support/Index?tab=messages&userId={supportId}",
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                await context.Notifications.AddAsync(notification);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Личное уведомление отправлено пользователю {receiverId} от {supportId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о личном сообщении");
            }
        }

        // Личное сообщение (перегрузка с 4 параметрами для совместимости с Support/Index)
        public async Task SendNewPrivateMessageNotificationAsync(int supportId, int receiverId, string messageText, int senderId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaxFoodDBContext>();

                var sender = await context.AppUsers
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.AppUserId == senderId);

                var senderName = sender?.UserProfile?.FirstName ?? "Пользователь";

                var notification = new MaxFood.Models.Notification
                {
                    AppUserId = receiverId,
                    Title = "Новое личное сообщение",
                    Message = $"{senderName} написал(а) вам: {messageText}",
                    LongMessage = $"Пользователь {senderName} написал вам личное сообщение:\n\n{messageText}\n\nПерейдите в раздел сообщений, чтобы ответить.",
                    ActionLink = $"/Support/Index?tab=messages&userId={senderId}",
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                await context.Notifications.AddAsync(notification);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Личное уведомление отправлено пользователю {receiverId} от {senderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления о личном сообщении");
            }
        }

        // Ответ администратора/менеджера в поддержку → уведомление пользователю
        public async Task SendAdminReplyNotificationAsync(int supportId, int userId, string replyText, int adminId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaxFoodDBContext>();

                var admin = await context.AppUsers
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.AppUserId == adminId);

                var adminName = admin?.UserProfile?.FirstName ?? "Администратор";

                var notification = new MaxFood.Models.Notification
                {
                    AppUserId = userId,
                    Title = "Ответ от поддержки",
                    Message = $"{adminName} ответил(а) на ваше обращение: {replyText}",
                    LongMessage = $"Администратор {adminName} ответил на ваше обращение:\n\n{replyText}\n\nПерейдите в раздел поддержки, чтобы продолжить диалог.",
                    ActionLink = "/Support/Index?tab=support&support=1",
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                await context.Notifications.AddAsync(notification);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Уведомление об ответе отправлено пользователю {userId} от админа {adminId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке уведомления об ответе");
            }
        }
    }
}