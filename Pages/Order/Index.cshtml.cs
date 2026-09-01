using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Order
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

        public List<OrderViewModel> Orders { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public string CurrentStatusFilter { get; set; } = "all";

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerPhone { get; set; } = string.Empty;
            public string DeliveryAddress { get; set; } = string.Empty;
            public string? Comment { get; set; }
        }

        public class OrderActionModel
        {
            public int OrderId { get; set; }
        }

        private int GetUserRole()
        {
            return HttpContext.Session.GetInt32("UserRole") ?? 1;
        }

        private bool IsAdminOrManager()
        {
            int role = GetUserRole();
            return role == 4 || role == 3;
        }

        private bool IsCourier()
        {
            return GetUserRole() == 2;
        }

        private bool IsAdminOrManagerOrCourier()
        {
            int role = GetUserRole();
            return role == 4 || role == 3 || role == 2;
        }

        public async Task<IActionResult> OnGetAsync(int? page, string? status)
        {
            if (!IsAdminOrManagerOrCourier())
            {
                return RedirectToPage("/Authorization/Login");
            }

            CurrentPage = page ?? 1;
            CurrentStatusFilter = status ?? "all";

            var query = _context.Orders
                .Include(o => o.AppUser)
                    .ThenInclude(u => u.UserProfile)
                .Include(o => o.PointPickup)
                .AsQueryable();

            // Админ и менеджер видят ВСЕ заказы (без фильтра по пользователю)
            // Курьер тоже видит все заказы
            // Фильтр по статусу
            if (CurrentStatusFilter != "all")
            {
                string statusRu = CurrentStatusFilter switch
                {
                    "new" => "Новый",
                    "ready" => "Готов",
                    "completed" => "Завершён",
                    "cancelled" => "Отменён",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(statusRu))
                {
                    query = query.Where(o => o.Status == statusRu);
                }
            }

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    CustomerName = (o.AppUser.UserProfile.FirstName + " " + o.AppUser.UserProfile.LastName).Trim(),
                    CustomerPhone = o.AppUser.UserProfile.Phone ?? "",
                    DeliveryAddress = o.PointPickup != null ? o.PointPickup.Address : (o.DeliveryAddress ?? "Самовывоз"),
                    Comment = o.Comment
                })
                .ToListAsync();

            Orders = orders;
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostMarkAsReady([FromBody] OrderActionModel model)
        {
            int role = GetUserRole();
            if (!IsAdminOrManagerOrCourier())
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            var order = await _context.Orders.FindAsync(model.OrderId);
            if (order == null)
                return new JsonResult(new { success = false, message = "Заказ не найден" });

            if (order.Status != "Новый")
                return new JsonResult(new { success = false, message = "Можно отметить готовым только новый заказ" });

            order.Status = "Готов";
            await _context.SaveChangesAsync();

            var userNotification = new MaxFood.Models.Notification
            {
                AppUserId = order.AppUserId,
                Title = "Заказ готов!",
                Message = $"Ваш заказ №{order.OrderId} готов к выдаче",
                LongMessage = $"Заказ №{order.OrderId} полностью готов. Можете забрать его.",
                ActionLink = $"/Order/Details?id={order.OrderId}",
                CreatedDate = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(userNotification);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Заказ отмечен как готовый" });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCompleteOrder([FromBody] OrderActionModel model)
        {
            int role = GetUserRole();
            if (!IsAdminOrManagerOrCourier())
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            var order = await _context.Orders.FindAsync(model.OrderId);
            if (order == null)
                return new JsonResult(new { success = false, message = "Заказ не найден" });

            if (order.Status != "Готов")
                return new JsonResult(new { success = false, message = "Можно выдать только готовый заказ" });

            order.Status = "Выдан";
            await _context.SaveChangesAsync();

            var userNotification = new MaxFood.Models.Notification
            {
                AppUserId = order.AppUserId,
                Title = "Заказ выдан",
                Message = $"Ваш заказ №{order.OrderId} успешно выдан",
                LongMessage = $"Спасибо, что выбрали MaxFood! Заказ №{order.OrderId} выполнен.",
                ActionLink = $"/Order/Details?id={order.OrderId}",
                CreatedDate = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(userNotification);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Заказ выдан" });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCancelOrder([FromBody] OrderActionModel model)
        {
            int role = GetUserRole();
            if (!IsAdminOrManager())
                return new JsonResult(new { success = false, message = "Недостаточно прав. Только администратор или менеджер могут отменить заказ." });

            var order = await _context.Orders.FindAsync(model.OrderId);
            if (order == null)
                return new JsonResult(new { success = false, message = "Заказ не найден" });

            if (order.Status == "Отменён")
                return new JsonResult(new { success = false, message = "Заказ уже отменён" });

            if (order.Status == "Выдан")
                return new JsonResult(new { success = false, message = "Выданный заказ нельзя отменить" });

            order.Status = "Отменён";
            await _context.SaveChangesAsync();

            var userNotification = new MaxFood.Models.Notification
            {
                AppUserId = order.AppUserId,
                Title = "Заказ отменён",
                Message = $"Ваш заказ №{order.OrderId} был отменён",
                LongMessage = $"К сожалению, заказ №{order.OrderId} был отменён. Если у вас есть вопросы, обратитесь в поддержку.",
                ActionLink = $"/Order/Details?id={order.OrderId}",
                CreatedDate = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(userNotification);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Заказ отменён" });
        }
    }
}