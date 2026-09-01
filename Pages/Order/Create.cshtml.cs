using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Order
{
    public class CreateModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(MaxFoodDBContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public MaxFood.Models.Order Order { get; set; } = default!;

        [BindProperty]
        public string? DeliveryAddress { get; set; }

        [BindProperty]
        public decimal BonusToSpend { get; set; }

        [BindProperty]
        public string? PromoCode { get; set; }

        [BindProperty]
        public decimal FinalTotal { get; set; }

        [BindProperty]
        public int? SelectedDeliveryAddressId { get; set; }

        [BindProperty]
        public int? SelectedPaymentId { get; set; }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public List<Cashback> Cashbacks { get; set; } = new();
        public List<MaxFood.Models.PointPickup> PointPickups { get; set; } = new();
        public List<AddressDelivery> UserAddresses { get; set; } = new();
        public List<Payment> UserPayments { get; set; } = new();
        public decimal BonusBalance { get; set; }
        public List<Promocode> AvailablePromocodes { get; set; } = new();

        public class SaveDeliveryAddressModel
        {
            public int? AddressId { get; set; }
            public string City { get; set; } = string.Empty;
            public string Street { get; set; } = string.Empty;
            public string? Apartment { get; set; }
            public string? Entrance { get; set; }
            public int? Floor { get; set; }
            public string? Domofon { get; set; }
            public string? Comment { get; set; }
            public bool IsDefault { get; set; }
        }

        public class SavePickupPointModel
        {
            public int PointPickupId { get; set; }
        }

        public class ToggleDefaultAddressModel
        {
            public int AddressId { get; set; }
            public bool IsDefault { get; set; }
        }

        public class ToggleDefaultPaymentModel
        {
            public int PaymentId { get; set; }
            public bool IsDefault { get; set; }
        }

        public class SavePaymentModel
        {
            public int? PaymentId { get; set; }
            public string CardNumber { get; set; } = string.Empty;
            public string CardHolder { get; set; } = string.Empty;
            public string ExpiryDate { get; set; } = string.Empty;
            public bool IsDefault { get; set; }
        }

        private static string GetFullAddressString(AddressDelivery addr)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(addr.City)) parts.Add(addr.City);
            if (!string.IsNullOrEmpty(addr.Address)) parts.Add(addr.Address);
            var details = new List<string>();
            if (!string.IsNullOrEmpty(addr.Entrance)) details.Add($"подъезд {addr.Entrance}");
            if (addr.Floor.HasValue && addr.Floor > 0) details.Add($"этаж {addr.Floor}");
            if (!string.IsNullOrEmpty(addr.Domofon)) details.Add($"домофон {addr.Domofon}");
            if (!string.IsNullOrEmpty(addr.Apartment)) details.Add($"кв. {addr.Apartment}");
            var address = string.Join(", ", parts);
            return address + (details.Any() ? ", " + string.Join(", ", details) : "");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToPage("/Authorization/Login");
            int userId = int.Parse(userIdStr);

            PointPickups = await _context.PointPickups.Where(p => p.IsActive).OrderBy(p => p.PointName).ToListAsync();
            UserAddresses = await _context.AddressDeliveries.Where(a => a.AppUserId == userId).OrderByDescending(a => a.IsDefault).ThenBy(a => a.AddressDeliveryId).ToListAsync();
            UserPayments = await _context.Payments.Where(p => p.AppUserId == userId).OrderByDescending(p => p.IsDefault).ThenBy(p => p.PaymentId).ToListAsync();

            var cart = await _context.Carts.Include(c => c.CartItems).ThenInclude(ci => ci.DishVariant).ThenInclude(dv => dv.Dish).FirstOrDefaultAsync(c => c.AppUserId == userId);
            if (cart == null || cart.CartItems == null || cart.CartItems.Count == 0)
            {
                CartItems = new List<CartItem>();
                TotalAmount = 0;
            }
            else
            {
                CartItems = cart.CartItems.ToList();
                TotalAmount = CartItems.Sum(i => i.Quantity * i.PriceAtAdd);
            }

            BonusBalance = await _context.Bonuses.Where(b => b.AppUserId == userId && b.IsActive).SumAsync(b => b.BonusAmount);
            var today = DateTime.Today;
            AvailablePromocodes = await _context.Promocodes.Where(p => p.IsActive && p.ValidFrom <= today && p.ValidTo >= today).ToListAsync();

            var defaultAddress = UserAddresses.FirstOrDefault(a => a.IsDefault);
            if (defaultAddress != null)
            {
                SelectedDeliveryAddressId = defaultAddress.AddressDeliveryId;
                DeliveryAddress = GetFullAddressString(defaultAddress);
            }
            else if (UserAddresses.Count > 0)
            {
                var firstAddress = UserAddresses.First();
                SelectedDeliveryAddressId = firstAddress.AddressDeliveryId;
                DeliveryAddress = GetFullAddressString(firstAddress);
            }

            var defaultPayment = UserPayments.FirstOrDefault(p => p.IsDefault);
            if (defaultPayment != null) SelectedPaymentId = defaultPayment.PaymentId;
            else if (UserPayments.Count > 0) SelectedPaymentId = UserPayments.First().PaymentId;

            ViewData["PointPickupId"] = new SelectList(PointPickups, "PointPickupId", "PointName");

            Order = new MaxFood.Models.Order
            {
                AppUserId = userId,
                CartId = cart?.CartId ?? 0,
                OrderDate = DateTime.Now,
                Status = "Новый",
                TotalAmount = TotalAmount
            };
            var user = await _context.AppUsers.FindAsync(userId);
            if (user?.SelectedPickupPointId != null) Order.PointPickupId = user.SelectedPickupPointId.Value;

            return Page();
        }

        // API: Получить все промокоды пользователя (сохранённые + глобальные)
        public async Task<IActionResult> OnGetUserPromoCodes()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return new JsonResult(new List<string>());

            int userId = int.Parse(userIdStr);
            var today = DateTime.Today;

            // 1. Промокоды, которые пользователь сохранил вручную (saved_promo)
            var savedPromoCodes = await _context.BonusTransactions
                .Where(bt => bt.AppUserId == userId && bt.Type == "saved_promo" && (bt.ExpiryDate == null || bt.ExpiryDate >= DateTime.Now))
                .Select(bt => bt.Source ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToListAsync();

            // 2. Глобальные активные промокоды
            var globalPromoCodes = await _context.Promocodes
                .Where(p => p.IsActive && p.ValidFrom <= today && p.ValidTo >= today)
                .Select(p => p.PromoCode)
                .ToListAsync();

            // Объединяем и убираем дубликаты
            var allCodes = savedPromoCodes
                .Union(globalPromoCodes)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return new JsonResult(allCodes);
        }

        public async Task<IActionResult> OnGetCheckPromoCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                return new JsonResult(new { success = false, message = "Введите промокод" });
            var promo = await _context.Promocodes.FirstOrDefaultAsync(p => p.PromoCode == code && p.IsActive && p.ValidFrom <= DateTime.Today && p.ValidTo >= DateTime.Today);
            if (promo == null)
                return new JsonResult(new { success = false, message = "Промокод не найден или истёк" });
            if (promo.MaxUses.HasValue && promo.CurrentUses >= promo.MaxUses)
                return new JsonResult(new { success = false, message = "Лимит использований исчерпан" });
            return new JsonResult(new { success = true, discountPercent = promo.DiscountPercent });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Authorization/Login");
            int userId = int.Parse(userIdStr);

            var cart = await _context.Carts.Include(c => c.CartItems).ThenInclude(ci => ci.DishVariant).ThenInclude(dv => dv.Dish).FirstOrDefaultAsync(c => c.CartId == Order.CartId && c.AppUserId == userId);
            if (cart == null || cart.CartItems == null || cart.CartItems.Count == 0)
            {
                ModelState.AddModelError("", "Корзина пуста или уже оформлена");
                await LoadData();
                return Page();
            }

            Order.TotalAmount = FinalTotal > 0 ? FinalTotal : cart.CartItems.Sum(i => i.Quantity * i.PriceAtAdd);
            Order.OrderDate = DateTime.Now;
            Order.Status = "Новый";

            if (Order.PointPickupId <= 0 && string.IsNullOrEmpty(DeliveryAddress))
            {
                ModelState.AddModelError("", "Выберите точку самовывоза или адрес доставки");
                await LoadData();
                return Page();
            }
            if (!ModelState.IsValid)
            {
                await LoadData();
                return Page();
            }

            if (!string.IsNullOrEmpty(DeliveryAddress))
            {
                Order.DeliveryAddress = DeliveryAddress;
                Order.PointPickupId = null;
            }

            _context.Orders.Add(Order);
            await _context.SaveChangesAsync();

            foreach (var cartItem in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = Order.OrderId,
                    DishId = cartItem.DishVariant?.DishId ?? 0,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.PriceAtAdd,
                    DishName = cartItem.DishName ?? cartItem.DishVariant?.Dish?.DishName ?? "Блюдо"
                };
                _context.OrderItems.Add(orderItem);
            }
            await _context.SaveChangesAsync();

            if (BonusToSpend > 0)
            {
                var userBonuses = await _context.Bonuses.Where(b => b.AppUserId == userId && b.IsActive && b.BonusAmount > 0).OrderBy(b => b.ValidFrom).ToListAsync();
                decimal remainingToSpend = BonusToSpend;
                foreach (var bonus in userBonuses)
                {
                    if (remainingToSpend <= 0) break;
                    decimal toSpend = Math.Min(remainingToSpend, bonus.BonusAmount);
                    bonus.BonusAmount -= toSpend;
                    remainingToSpend -= toSpend;
                    _context.BonusTransactions.Add(new BonusTransaction
                    {
                        AppUserId = userId,
                        OrderId = Order.OrderId,
                        Amount = -toSpend,
                        Type = "spend",
                        Source = $"Списание за заказ №{Order.OrderId}",
                        CreatedDate = DateTime.Now
                    });
                    if (bonus.BonusAmount <= 0) bonus.IsActive = false;
                }
            }

            if (!string.IsNullOrEmpty(PromoCode))
            {
                var promo = await _context.Promocodes.FirstOrDefaultAsync(p => p.PromoCode == PromoCode && p.IsActive);
                if (promo != null)
                {
                    promo.CurrentUses++;

                    _context.BonusTransactions.Add(new BonusTransaction
                    {
                        AppUserId = userId,
                        OrderId = Order.OrderId,
                        PromocodeId = promo.PromocodeId,
                        Amount = 0,
                        Type = "promo",
                        Source = $"Промокод {promo.PromoCode} (-{promo.DiscountPercent}%)",
                        CreatedDate = DateTime.Now
                    });
                }
            }

            var newBonusAmount = Math.Round(Order.TotalAmount * 0.05m, 2);
            if (newBonusAmount > 0)
            {
                _context.Bonuses.Add(new Bonus
                {
                    AppUserId = userId,
                    BonusAmount = newBonusAmount,
                    OrderId = Order.OrderId,
                    Source = $"Обычный кешбэк за заказ №{Order.OrderId}",
                    ValidFrom = DateTime.Now,
                    ValidTo = DateTime.Now.AddDays(180),
                    IsActive = true
                });
                _context.BonusTransactions.Add(new BonusTransaction
                {
                    AppUserId = userId,
                    OrderId = Order.OrderId,
                    Amount = newBonusAmount,
                    Type = "accrual",
                    Source = $"Обычный кешбэк за заказ №{Order.OrderId}",
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(180)
                });
            }
            await _context.SaveChangesAsync();

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.TotalAmount = 0;
            await _context.SaveChangesAsync();

            return RedirectToPage("/Profile/MyProfile", new { tab = "orders", orderId = Order.OrderId });
        }

        public async Task<IActionResult> OnPostTogglePickupPoint([FromBody] SavePickupPointModel model)
        {
            try
            {
                var uid = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false, message = "Не авторизован" });
                int userId = int.Parse(uid);
                var user = await _context.AppUsers.FindAsync(userId);
                if (user == null) return new JsonResult(new { success = false, message = "Пользователь не найден" });
                if (user.SelectedPickupPointId == model.PointPickupId)
                {
                    user.SelectedPickupPointId = null;
                    await _context.SaveChangesAsync();
                    return new JsonResult(new { success = true, selected = false });
                }
                else
                {
                    user.SelectedPickupPointId = model.PointPickupId;
                    await _context.SaveChangesAsync();
                    return new JsonResult(new { success = true, selected = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка TogglePickupPoint");
                return new JsonResult(new { success = false, message = "Ошибка сервера" });
            }
        }

        public async Task<IActionResult> OnPostToggleDefaultAddress([FromBody] ToggleDefaultAddressModel model)
        {
            try
            {
                var uid = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false });
                int userId = int.Parse(uid);
                if (model.IsDefault)
                {
                    var defs = await _context.AddressDeliveries.Where(a => a.AppUserId == userId && a.IsDefault).ToListAsync();
                    foreach (var a in defs) a.IsDefault = false;
                }
                var addr = await _context.AddressDeliveries.FindAsync(model.AddressId);
                if (addr != null && addr.AppUserId == userId)
                {
                    addr.IsDefault = model.IsDefault;
                    await _context.SaveChangesAsync();
                }
                if (model.IsDefault && addr != null)
                {
                    DeliveryAddress = GetFullAddressString(addr);
                    SelectedDeliveryAddressId = addr.AddressDeliveryId;
                }
                else if (!model.IsDefault && SelectedDeliveryAddressId == model.AddressId)
                {
                    var newDefault = await _context.AddressDeliveries.Where(a => a.AppUserId == userId && a.IsDefault).FirstOrDefaultAsync();
                    if (newDefault != null)
                    {
                        DeliveryAddress = GetFullAddressString(newDefault);
                        SelectedDeliveryAddressId = newDefault.AddressDeliveryId;
                    }
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка ToggleDefaultAddress");
                return new JsonResult(new { success = false, message = "Ошибка сервера" });
            }
        }

        public async Task<IActionResult> OnPostToggleDefaultPayment([FromBody] ToggleDefaultPaymentModel model)
        {
            try
            {
                var uid = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false });
                int userId = int.Parse(uid);
                if (model.IsDefault)
                {
                    var defs = await _context.Payments.Where(p => p.AppUserId == userId && p.IsDefault).ToListAsync();
                    foreach (var p in defs) p.IsDefault = false;
                }
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == model.PaymentId && p.AppUserId == userId);
                if (payment != null)
                {
                    payment.IsDefault = model.IsDefault;
                    await _context.SaveChangesAsync();
                }
                if (model.IsDefault && payment != null) SelectedPaymentId = payment.PaymentId;
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка ToggleDefaultPayment");
                return new JsonResult(new { success = false, message = "Ошибка сервера" });
            }
        }

        public async Task<IActionResult> OnPostSaveDeliveryAddress([FromBody] SaveDeliveryAddressModel model)
        {
            try
            {
                var uid = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false, message = "Не авторизован" });
                int userId = int.Parse(uid);
                var city = model.City?.Trim() ?? "";
                var street = model.Street?.Trim() ?? "";
                if (string.IsNullOrEmpty(street)) return new JsonResult(new { success = false, message = "Введите улицу" });
                if (model.IsDefault)
                {
                    var defs = await _context.AddressDeliveries.Where(a => a.AppUserId == userId && a.IsDefault).ToListAsync();
                    foreach (var a in defs) a.IsDefault = false;
                }
                AddressDelivery addr;
                if (model.AddressId.HasValue && model.AddressId > 0)
                {
                    addr = await _context.AddressDeliveries.FindAsync(model.AddressId.Value);
                    if (addr == null) return new JsonResult(new { success = false, message = "Адрес не найден" });
                }
                else
                {
                    addr = new AddressDelivery { AppUserId = userId };
                    _context.AddressDeliveries.Add(addr);
                }
                addr.City = city;
                addr.Address = street;
                addr.Apartment = model.Apartment;
                addr.Entrance = model.Entrance;
                addr.Floor = model.Floor;
                addr.Domofon = model.Domofon;
                addr.Comment = model.Comment;
                addr.IsDefault = model.IsDefault;
                await _context.SaveChangesAsync();
                if (model.IsDefault)
                {
                    DeliveryAddress = GetFullAddressString(addr);
                    SelectedDeliveryAddressId = addr.AddressDeliveryId;
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка SaveDeliveryAddress");
                return new JsonResult(new { success = false, message = "Ошибка сервера" });
            }
        }

        public async Task<IActionResult> OnPostDeleteDeliveryAddress(int id)
        {
            try
            {
                var addr = await _context.AddressDeliveries.FindAsync(id);
                if (addr != null)
                {
                    _context.AddressDeliveries.Remove(addr);
                    await _context.SaveChangesAsync();
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка DeleteDeliveryAddress");
                return new JsonResult(new { success = false, message = "Ошибка удаления" });
            }
        }

        public async Task<IActionResult> OnPostSavePayment([FromBody] SavePaymentModel model)
        {
            try
            {
                var uid = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false, message = "Не авторизован" });
                int userId = int.Parse(uid);
                if (string.IsNullOrWhiteSpace(model.CardNumber) || model.CardNumber.Length < 16)
                    return new JsonResult(new { success = false, message = "Неверный номер карты" });
                if (string.IsNullOrWhiteSpace(model.CardHolder))
                    return new JsonResult(new { success = false, message = "Введите CVV/CVC" });
                if (string.IsNullOrWhiteSpace(model.ExpiryDate) || model.ExpiryDate.Length < 5)
                    return new JsonResult(new { success = false, message = "Неверный срок действия" });
                if (model.IsDefault)
                {
                    var defs = await _context.Payments.Where(p => p.AppUserId == userId && p.IsDefault).ToListAsync();
                    foreach (var p in defs) p.IsDefault = false;
                }
                Payment payment;
                if (model.PaymentId.HasValue && model.PaymentId > 0)
                {
                    payment = await _context.Payments.FindAsync(model.PaymentId.Value);
                    if (payment == null) return new JsonResult(new { success = false, message = "Карта не найдена" });
                }
                else
                {
                    payment = new Payment { AppUserId = userId };
                    _context.Payments.Add(payment);
                }
                payment.CardNumber = model.CardNumber;
                payment.CardHolder = model.CardHolder;
                payment.ExpiryDate = model.ExpiryDate;
                payment.IsDefault = model.IsDefault;
                await _context.SaveChangesAsync();
                if (model.IsDefault) SelectedPaymentId = payment.PaymentId;
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка SavePayment");
                return new JsonResult(new { success = false, message = "Ошибка сервера" });
            }
        }

        public async Task<IActionResult> OnPostDeletePayment(int id)
        {
            try
            {
                var uid = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false, message = "Не авторизован" });
                int userId = int.Parse(uid);
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == id && p.AppUserId == userId);
                if (payment != null)
                {
                    _context.Payments.Remove(payment);
                    await _context.SaveChangesAsync();
                }
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка DeletePayment");
                return new JsonResult(new { success = false, message = "Ошибка удаления" });
            }
        }

        private async Task LoadData()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return;
            int userId = int.Parse(userIdStr);
            PointPickups = await _context.PointPickups.Where(p => p.IsActive).OrderBy(p => p.PointName).ToListAsync();
            UserAddresses = await _context.AddressDeliveries.Where(a => a.AppUserId == userId).OrderByDescending(a => a.IsDefault).ToListAsync();
            UserPayments = await _context.Payments.Where(p => p.AppUserId == userId).OrderByDescending(p => p.IsDefault).ToListAsync();
            var cart = await _context.Carts.Include(c => c.CartItems).ThenInclude(ci => ci.DishVariant).ThenInclude(dv => dv.Dish).FirstOrDefaultAsync(c => c.AppUserId == userId);
            if (cart != null && cart.CartItems != null)
            {
                CartItems = cart.CartItems.ToList();
                TotalAmount = CartItems.Sum(i => i.Quantity * i.PriceAtAdd);
            }
            BonusBalance = await _context.Bonuses.Where(b => b.AppUserId == userId && b.IsActive).SumAsync(b => b.BonusAmount);
            var today = DateTime.Today;
            AvailablePromocodes = await _context.Promocodes.Where(p => p.IsActive && p.ValidFrom <= today && p.ValidTo >= today).ToListAsync();
            ViewData["PointPickupId"] = new SelectList(PointPickups, "PointPickupId", "PointName", Order.PointPickupId);
        }
    }
}