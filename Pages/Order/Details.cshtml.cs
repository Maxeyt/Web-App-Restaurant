using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Order
{
    public class DetailsModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(MaxFoodDBContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public OrderDetailsViewModel? Order { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; } = new();

        public class OrderDetailsViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerPhone { get; set; } = string.Empty;
            public string DeliveryMethod { get; set; } = string.Empty;
            public string DeliveryAddress { get; set; } = string.Empty;
            public string? DeliveryComment { get; set; }
            public string? Comment { get; set; }
            public int? SelectedPickupPointId { get; set; }
            public string? SelectedPickupPointName { get; set; }
            public int AppUserId { get; set; }
        }

        public class OrderItemViewModel
        {
            public int DishId { get; set; }
            public string DishName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public string? SizeName { get; set; }
        }

        public class OrderActionModel
        {
            public int OrderId { get; set; }
        }

        private int GetUserRole() => HttpContext.Session.GetInt32("UserRole") ?? 1;
        private bool IsAdminOrManagerOrCourier() => GetUserRole() >= 2;
        private bool IsAdminOrManager() => GetUserRole() == 4 || GetUserRole() == 3;
        private bool IsCourier() => GetUserRole() == 2;

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();
            int userRole = GetUserRole();
            var userIdStr = HttpContext.Session.GetString("UserId");
            int? currentUserId = string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);

            var orderQuery = _context.Orders
                .Include(o => o.AppUser).ThenInclude(u => u.UserProfile)
                .Include(o => o.PointPickup)
                .Where(o => o.OrderId == id.Value);

            if (userRole == 1 && currentUserId.HasValue)
                orderQuery = orderQuery.Where(o => o.AppUserId == currentUserId.Value);

            var order = await orderQuery.FirstOrDefaultAsync();
            if (order == null) return NotFound();

            string? pickupPointName = order.SelectedPickupPointId.HasValue
                ? (await _context.PointPickups.FindAsync(order.SelectedPickupPointId.Value))?.PointName
                : (order.PointPickupId.HasValue
                    ? (await _context.PointPickups.FindAsync(order.PointPickupId.Value))?.PointName
                    : "ТРЦ 'City' (ул. Савушкина, 5)");

            Order = new OrderDetailsViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CustomerName = (order.AppUser.UserProfile.FirstName + " " + order.AppUser.UserProfile.LastName).Trim(),
                CustomerPhone = order.AppUser.UserProfile.Phone ?? "",
                DeliveryMethod = order.PointPickupId.HasValue ? "Самовывоз" : "Доставка",
                DeliveryAddress = order.DeliveryAddress ?? "",
                DeliveryComment = order.DeliveryComment,
                Comment = order.Comment,
                SelectedPickupPointId = order.SelectedPickupPointId,
                SelectedPickupPointName = pickupPointName,
                AppUserId = order.AppUserId
            };

            await LoadOrderItems(order.OrderId);
            return Page();
        }

        private async Task LoadOrderItems(int orderId)
        {
            try
            {
                var items = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .Include(oi => oi.Dish)
                    .Select(oi => new OrderItemViewModel
                    {
                        DishId = oi.DishId,
                        DishName = oi.Dish != null ? oi.Dish.DishName : oi.DishName ?? "Блюдо",
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        SizeName = null
                    })
                    .ToListAsync();

                if (items.Count > 0)
                {
                    OrderItems = items;
                    return;
                }

                var order = await _context.Orders.FindAsync(orderId);
                if (order != null && order.CartId > 0)
                {
                    var cart = await _context.Carts
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.DishVariant)
                                .ThenInclude(dv => dv.Dish)
                        .FirstOrDefaultAsync(c => c.CartId == order.CartId);

                    if (cart?.CartItems != null && cart.CartItems.Count > 0)
                    {
                        OrderItems = cart.CartItems.Select(ci => new OrderItemViewModel
                        {
                            DishId = ci.DishVariantId,
                            DishName = ci.DishName ?? ci.DishVariant?.Dish?.DishName ?? "Блюдо",
                            Quantity = ci.Quantity,
                            Price = ci.PriceAtAdd,
                            SizeName = ci.SizeName ?? ci.DishVariant?.SizeName
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка загрузки состава заказа {orderId}");
                OrderItems = new List<OrderItemViewModel>();
            }
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

            var notification = new MaxFood.Models.Notification
            {
                AppUserId = order.AppUserId,
                Title = "Заказ готов!",
                Message = $"Ваш заказ №{order.OrderId} готов к выдаче",
                LongMessage = $"Заказ №{order.OrderId} полностью готов. Можете забрать его.",
                ActionLink = $"/Order/Details?id={order.OrderId}",
                CreatedDate = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
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

            var notification = new MaxFood.Models.Notification
            {
                AppUserId = order.AppUserId,
                Title = "Заказ выдан",
                Message = $"Ваш заказ №{order.OrderId} успешно выдан",
                LongMessage = $"Спасибо, что выбрали MaxFood! Заказ №{order.OrderId} выполнен.",
                ActionLink = $"/Order/Details?id={order.OrderId}",
                CreatedDate = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
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

            var notification = new MaxFood.Models.Notification
            {
                AppUserId = order.AppUserId,
                Title = "Заказ отменён",
                Message = $"Ваш заказ №{order.OrderId} был отменён",
                LongMessage = $"К сожалению, заказ №{order.OrderId} был отменён. Если у вас есть вопросы, обратитесь в поддержку.",
                ActionLink = $"/Order/Details?id={order.OrderId}",
                CreatedDate = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Заказ отменён" });
        }

        // Скачивание полного документа заказа (чека)
        public async Task<IActionResult> OnGetDownloadReceiptFull(int id)
        {
            int role = GetUserRole();
            if (!IsAdminOrManagerOrCourier() && role != 1)
                return Forbid();

            var order = await _context.Orders
                .Include(o => o.AppUser).ThenInclude(u => u.UserProfile)
                .Include(o => o.PointPickup)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
                return NotFound();

            var userIdStr = HttpContext.Session.GetString("UserId");
            int currentUserId = string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
            if (role == 1 && order.AppUserId != currentUserId)
                return Forbid();

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == id)
                .Include(oi => oi.Dish)
                .ToListAsync();

            string pickupPointName = order.PointPickupId.HasValue
                ? (await _context.PointPickups.FindAsync(order.PointPickupId.Value))?.PointName ?? "ТРЦ 'City' (ул. Савушкина, 5)"
                : "ТРЦ 'City' (ул. Савушкина, 5)";

            var htmlBuilder = new System.Text.StringBuilder();
            htmlBuilder.AppendLine("<!DOCTYPE html>");
            htmlBuilder.AppendLine("<html>");
            htmlBuilder.AppendLine("<head><meta charset='utf-8'><title>Документ заказа №" + order.OrderId + "</title>");
            htmlBuilder.AppendLine("<link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css'>");
            htmlBuilder.AppendLine("<link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>");
            htmlBuilder.AppendLine("<style>");
            htmlBuilder.AppendLine("*{font-family:'Segoe UI',Roboto,sans-serif}body{background:white;padding:30px;margin:0}");
            htmlBuilder.AppendLine(".receipt-container{max-width:800px;margin:0 auto;background:white;border-radius:16px;padding:24px;border:1px solid #e9ecef}");
            htmlBuilder.AppendLine(".header{text-align:center;margin-bottom:24px;padding-bottom:16px;border-bottom:2px solid #2b8c4a}");
            htmlBuilder.AppendLine(".header h1{font-size:1.5rem;font-weight:700;color:#2b8c4a;margin:0}");
            htmlBuilder.AppendLine(".header p{color:#6c757d;margin:5px 0 0}");
            htmlBuilder.AppendLine(".info-row{display:flex;margin-bottom:12px;flex-wrap:wrap;border-bottom:1px solid #e9ecef;padding-bottom:8px}");
            htmlBuilder.AppendLine(".info-label{width:140px;font-weight:700;color:#495057}");
            htmlBuilder.AppendLine(".info-value{flex:1;color:#212529}");
            htmlBuilder.AppendLine(".items-table{width:100%;margin-top:20px;border-collapse:collapse}");
            htmlBuilder.AppendLine(".items-table th,.items-table td{border:1px solid #dee2e6;padding:10px;text-align:left}");
            htmlBuilder.AppendLine(".items-table th{background:#f8f9fa;font-weight:700}");
            htmlBuilder.AppendLine(".total-row{font-weight:700;background:#e8f5e9}");
            htmlBuilder.AppendLine(".footer{margin-top:30px;padding-top:16px;border-top:1px dashed #dee2e6;text-align:center;font-size:0.7rem;color:#6c757d}");
            htmlBuilder.AppendLine(".badge{display:inline-block;padding:4px 12px;border-radius:30px;font-size:0.7rem;font-weight:600}");
            htmlBuilder.AppendLine(".badge-delivery{background:#cce5ff;color:#004085}");
            htmlBuilder.AppendLine(".badge-pickup{background:#d4edda;color:#155724}");
            htmlBuilder.AppendLine("</style></head><body>");
            htmlBuilder.AppendLine("<div class='receipt-container'>");
            htmlBuilder.AppendLine("<div class='header'><h1><i class='bi bi-receipt'></i> Документ заказа №" + order.OrderId + "</h1><p>Максимум еды - детали заказа</p></div>");
            htmlBuilder.AppendLine($"<div class='info-row'><div class='info-label'>Дата заказа:</div><div class='info-value'>{order.OrderDate:dd.MM.yyyy HH:mm}</div></div>");
            htmlBuilder.AppendLine($"<div class='info-row'><div class='info-label'>Клиент:</div><div class='info-value'>{System.Net.WebUtility.HtmlEncode(order.AppUser.UserProfile.FirstName)} {System.Net.WebUtility.HtmlEncode(order.AppUser.UserProfile.LastName)}</div></div>");
            htmlBuilder.AppendLine($"<div class='info-row'><div class='info-label'>Телефон:</div><div class='info-value'>{System.Net.WebUtility.HtmlEncode(order.AppUser.UserProfile.Phone)}</div></div>");

            string method = order.PointPickupId.HasValue ? "Самовывоз" : "Доставка";
            string badgeClass = method == "Доставка" ? "badge-delivery" : "badge-pickup";
            htmlBuilder.AppendLine($"<div class='info-row'><div class='info-label'>Способ получения:</div><div class='info-value'><span class='badge {badgeClass}'>{method}</span></div></div>");

            if (method == "Доставка")
            {
                htmlBuilder.AppendLine($"<div class='info-row'><div class='info-label'>Адрес доставки:</div><div class='info-value'><strong>{System.Net.WebUtility.HtmlEncode(order.DeliveryAddress)}</strong></div></div>");
                if (!string.IsNullOrEmpty(order.DeliveryComment))
                    htmlBuilder.AppendLine($"<div class='info-row'><div class='info-label'>Комментарий курьеру:</div><div class='info-value'>{System.Net.WebUtility.HtmlEncode(order.DeliveryComment)}</div></div>");
            }
            else
            {
                htmlBuilder.AppendLine($"<div class='info-row'><div class='info-label'>Точка самовывоза:</div><div class='info-value'><strong>{System.Net.WebUtility.HtmlEncode(pickupPointName)}</strong></div></div>");
            }

            if (!string.IsNullOrEmpty(order.Comment))
            {
                htmlBuilder.AppendLine($"<div class='info-row'><div class='info-label'>Комментарий к заказу:</div><div class='info-value'>{System.Net.WebUtility.HtmlEncode(order.Comment)}</div></div>");
            }

            htmlBuilder.AppendLine("<h5 class='mt-4 mb-3'><i class='bi bi-basket-fill me-2'></i> Состав заказа</h5>");
            htmlBuilder.AppendLine("<table class='items-table'><thead>");
            htmlBuilder.AppendLine("<tr><th>Блюдо</th><th style='width:100px'>Кол-во</th><th style='width:120px'>Цена</th><th style='width:120px'>Сумма</th></tr>");
            htmlBuilder.AppendLine("</thead><tbody>");

            foreach (var item in orderItems)
            {
                decimal total = item.Price * item.Quantity;
                string dishName = System.Net.WebUtility.HtmlEncode(item.DishName ?? item.Dish?.DishName ?? "Блюдо");
                htmlBuilder.AppendLine($"<tr><td>{dishName}</td><td>{item.Quantity} шт.</td><td>{item.Price:F2} ₽</td><td>{total:F2} ₽</td></tr>");
            }

            string displayStatus = order.Status switch
            {
                "Новый" => "Новый",
                "Готов" => "Готов",
                "Завершен" => "Завершён",
                "Завершён" => "Завершён",
                "Отменен" => "Отменён",
                "Отменён" => "Отменён",
                _ => order.Status
            };

            htmlBuilder.AppendLine($"<tr class='total-row'><td colspan='3' style='text-align:right; font-weight:700;'>ИТОГО:</td><td style='font-weight:700;'>{order.TotalAmount:F2} ₽</td></tr>");
            htmlBuilder.AppendLine("</tbody>");
            htmlBuilder.AppendLine("</table>");
            htmlBuilder.AppendLine("<div class='footer'><p>© 2025-2026 Максимум еды - доставка еды</p>");
            htmlBuilder.AppendLine($"<p>Статус заказа: {displayStatus}</p>");
            htmlBuilder.AppendLine($"<p>Документ сформирован автоматически {DateTime.Now:dd.MM.yyyy HH:mm}</p></div>");
            htmlBuilder.AppendLine("</div></body></html>");

            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(htmlBuilder.ToString());
            return File(fileBytes, "text/html", $"document_zakaz_{order.OrderId}.html");
        }
    }
}