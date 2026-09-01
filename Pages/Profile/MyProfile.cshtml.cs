using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Profile
{
    public class MyProfileModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly ILogger<MyProfileModel> _logger;

        public MyProfileModel(MaxFoodDBContext context, ILogger<MyProfileModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty] public string FirstName { get; set; } = string.Empty;
        [BindProperty] public string LastName { get; set; } = string.Empty;
        [BindProperty] public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? ActiveTab { get; set; }
        public string? BonusSubTab { get; set; }
        public string? CashbackSubTab { get; set; }
        public string? AddressesSubTab { get; set; }
        public bool IsEditing => true;
        public decimal BonusTotal { get; set; }
        public decimal CashbackTotal { get; set; }
        public int ActivePromocodes { get; set; }
        public int? SelectedPickupPointId { get; set; }
        public string? DefaultDeliveryAddress { get; set; }
        public List<AddressDelivery> UserAddresses { get; set; } = new();
        public List<Payment> UserPayments { get; set; } = new();
        public List<CashbackCategoryDetailed> AllCashbackCategories { get; set; } = new();
        public List<PromocodeViewModel> ActivePromocodesList { get; set; } = new();
        public List<PromocodeViewModel> UsedPromocodesList { get; set; } = new();
        public List<int> SelectedCategoryIds { get; set; } = new();
        public List<BonusHistoryItem> BonusHistory { get; set; } = new();
        public List<BonusHistoryItem> BoostedBonusHistory { get; set; } = new();
        public List<OrderViewModel> NewOrders { get; set; } = new();
        public List<OrderViewModel> OrderHistory { get; set; } = new();
        public List<MaxFood.Models.PointPickup> AllPointPickups { get; set; } = new();
        public List<MapLocation> MapLocations { get; set; } = new();

        public StatOverview Overview { get; set; } = new();
        public ExtendedStatOverview ExtendedOverview { get; set; } = new();
        public List<CategoryStatForChart> MenuStats { get; set; } = new();
        public List<CategoryStatForChart> FavoriteStats { get; set; } = new();
        public List<CategoryStatForChart> ReviewStats { get; set; } = new();
        public string StatPeriod { get; set; } = "all";
        public string StatTab { get; set; } = "menu";

        public class CashbackCategoryDetailed
        {
            public int Id { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public decimal CashbackPercent { get; set; }
            public string Description { get; set; } = string.Empty;
            public List<string> Subcategories { get; set; } = new();
        }

        public class PromocodeViewModel
        {
            public string Code { get; set; } = string.Empty;
            public decimal? DiscountPercent { get; set; }
            public decimal? DiscountAmount { get; set; }
            public DateTime ValidFrom { get; set; }
            public DateTime ValidTo { get; set; }
            public bool IsActive { get; set; }
            public bool IsSaved { get; set; } = false;
            public DateTime? SavedAt { get; set; }
        }

        public class BonusHistoryItem
        {
            public decimal Amount { get; set; }
            public DateTime AccrualDate { get; set; }
            public DateTime ExpiryDate { get; set; }
            public string Source { get; set; } = string.Empty;
            public string Type { get; set; } = "bonus";
        }

        public class MapLocation
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string? Phone { get; set; }
            public string? WorkHours { get; set; }
            public string? IconType { get; set; } = "red";
        }

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public string? Comment { get; set; }
            public string? PointPickupName { get; set; }
            public string? PointPickupAddress { get; set; }
        }

        public class SavePickupPointModel
        {
            public int PointPickupId { get; set; }
        }

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

        public class SavePaymentModel
        {
            public int? PaymentId { get; set; }
            public string CardNumber { get; set; } = string.Empty;
            public string CardHolder { get; set; } = string.Empty;
            public string ExpiryDate { get; set; } = string.Empty;
            public bool IsDefault { get; set; }
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

        public class StatOverview
        {
            public int TotalOrders { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class ExtendedStatOverview
        {
            public int TotalOrders { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal AvgCheck { get; set; }
            public int NewOrders { get; set; }
            public int ReadyOrders { get; set; }
            public int CompletedOrders { get; set; }
            public int CancelledOrders { get; set; }
            public int UniqueCustomers { get; set; }
            public decimal MaxOrderAmount { get; set; }
            public decimal MinOrderAmount { get; set; }
            public int TotalItems { get; set; }
            public decimal AvgItemsPerOrder { get; set; }
            public int DeliveryOrders { get; set; }
            public int PickupOrders { get; set; }
        }

        public class DishStat
        {
            public int DishId { get; set; }
            public string DishName { get; set; } = "";
            public decimal Value { get; set; }
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public int? SubcategoryId { get; set; }
            public string SubcategoryName { get; set; } = "";
        }

        public class SubcategoryStatForChart
        {
            public string SubcategoryName { get; set; } = "";
            public decimal Value { get; set; }
        }

        public class CategoryStatForChart
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public List<SubcategoryStatForChart> Subcategories { get; set; } = new();
            public List<DishStat> TopDishes { get; set; } = new();
        }

        private bool IsAdminOrManager()
        {
            var roleId = HttpContext.Session.GetInt32("UserRole");
            return roleId == 4 || roleId == 3;
        }

        private async Task SaveCashbacksToDatabase(int userId, List<int> categoryIds)
        {
            using var t = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.Cashbacks.Where(c => c.AppUserId == userId).ToListAsync();
                foreach (var c in existing) c.IsActive = false;
                await _context.SaveChangesAsync();

                foreach (var id in categoryIds)
                {
                    var ec = await _context.Cashbacks.FirstOrDefaultAsync(c => c.AppUserId == userId && c.CategoryId == id);
                    var cat = AllCashbackCategories.FirstOrDefault(c => c.Id == id);
                    var pct = cat?.CashbackPercent ?? 0;

                    if (ec != null)
                    {
                        ec.IsActive = true;
                        ec.CashbackPercent = pct;
                    }
                    else
                    {
                        await _context.Cashbacks.AddAsync(new Cashback
                        {
                            AppUserId = userId,
                            CategoryId = id,
                            CashbackPercent = pct,
                            IsActive = true
                        });
                    }
                }
                await _context.SaveChangesAsync();
                await t.CommitAsync();
            }
            catch (Exception ex)
            {
                await t.RollbackAsync();
                _logger.LogError(ex, $"Ошибка транзакции кешбэка userId={userId}");
                throw;
            }
        }

        private void InitializeCashbackCategories()
        {
            if (AllCashbackCategories.Any()) return;

            AllCashbackCategories = new List<CashbackCategoryDetailed>
            {
                new() { Id = 1, CategoryName = "Пицца", Icon = "🍕", CashbackPercent = 5, Description = "Кешбэк на все виды пиццы", Subcategories = new List<string> { "Мясная", "Сырная", "Острая" } },
                new() { Id = 2, CategoryName = "Бургеры", Icon = "🍔", CashbackPercent = 3, Description = "Кешбэк на все бургеры", Subcategories = new List<string> { "Говяжьи", "Куриные", "Рыбные и веган" } },
                new() { Id = 3, CategoryName = "Роллы", Icon = "🍣", CashbackPercent = 7, Description = "Повышенный кешбэк на роллы", Subcategories = new List<string> { "Классические", "Запеченные", "Суши и гунканы" } },
                new() { Id = 4, CategoryName = "Лапша", Icon = "🍜", CashbackPercent = 4, Description = "Кешбэк на лапшу WOK", Subcategories = new List<string> { "С мясом", "С морепродуктами", "Фунчоза и рис" } },
                new() { Id = 5, CategoryName = "Шашлык", Icon = "🔥", CashbackPercent = 6, Description = "Кешбэк на шашлык", Subcategories = new List<string> { "Шашлык", "Люля-кебаб", "Стейки" } },
                new() { Id = 6, CategoryName = "Горячее", Icon = "🍳", CashbackPercent = 3, Description = "Кешбэк на горячее", Subcategories = new List<string> { "Мясные", "Рыбные", "Гарниры" } },
                new() { Id = 7, CategoryName = "Супы", Icon = "🥣", CashbackPercent = 4, Description = "Кешбэк на супы", Subcategories = new List<string> { "Горячие", "Крем-супы", "Холодные" } },
                new() { Id = 8, CategoryName = "Салаты", Icon = "🥗", CashbackPercent = 5, Description = "Кешбэк на салаты", Subcategories = new List<string> { "Мясные", "Овощные", "Рыбные" } },
                new() { Id = 9, CategoryName = "Закуски", Icon = "🍟", CashbackPercent = 3, Description = "Кешбэк на закуски", Subcategories = new List<string> { "Картофельные", "Куриные", "Сырные" } },
                new() { Id = 10, CategoryName = "Десерты", Icon = "🍰", CashbackPercent = 5, Description = "Кешбэк на десерты", Subcategories = new List<string> { "Торты", "Мороженое", "Блины" } },
                new() { Id = 11, CategoryName = "Напитки", Icon = "🥤", CashbackPercent = 10, Description = "Максимальный кешбэк", Subcategories = new List<string> { "Горячие", "Холодные", "Соки и вода" } }
            };
        }

        private async Task LoadExtendedStatistics(DateTime startDate)
        {
            var ordersQuery = _context.Orders.AsQueryable();
            if (startDate != DateTime.MinValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate);

            var ordersList = await ordersQuery.ToListAsync();

            ExtendedOverview = new ExtendedStatOverview
            {
                TotalOrders = ordersList.Count,
                TotalAmount = ordersList.Sum(o => o.TotalAmount),
                AvgCheck = ordersList.Any() ? ordersList.Average(o => o.TotalAmount) : 0,
                NewOrders = ordersList.Count(o => o.Status == "Новый"),
                ReadyOrders = ordersList.Count(o => o.Status == "Готов"),
                CompletedOrders = ordersList.Count(o => o.Status == "Завершён"),
                CancelledOrders = ordersList.Count(o => o.Status == "Отменён"),
                UniqueCustomers = ordersList.Select(o => o.AppUserId).Distinct().Count(),
                MaxOrderAmount = ordersList.Any() ? ordersList.Max(o => o.TotalAmount) : 0,
                MinOrderAmount = ordersList.Any() ? ordersList.Min(o => o.TotalAmount) : 0,
                DeliveryOrders = ordersList.Count(o => o.PointPickupId == null && !string.IsNullOrEmpty(o.DeliveryAddress)),
                PickupOrders = ordersList.Count(o => o.PointPickupId != null)
            };

            var orderIds = ordersList.Select(o => o.OrderId).ToList();
            var orderItems = await _context.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToListAsync();

            ExtendedOverview.TotalItems = orderItems.Sum(oi => oi.Quantity);
            ExtendedOverview.AvgItemsPerOrder = ExtendedOverview.TotalOrders > 0
                ? (decimal)ExtendedOverview.TotalItems / ExtendedOverview.TotalOrders
                : 0;
        }

        private async Task LoadStatistics(string period, string tab)
        {
            StatPeriod = period;
            StatTab = tab;

            DateTime startDate = period switch
            {
                "day" => DateTime.Today,
                "week" => DateTime.Today.AddDays(-7),
                "month" => DateTime.Today.AddMonths(-1),
                "year" => DateTime.Today.AddYears(-1),
                _ => DateTime.MinValue
            };

            var ordersQuery = _context.Orders.AsQueryable();
            if (startDate != DateTime.MinValue)
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate);

            Overview = new StatOverview
            {
                TotalOrders = await ordersQuery.CountAsync(),
                TotalAmount = await ordersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0
            };

            await LoadExtendedStatistics(startDate);

            var categories = await _context.Categories.ToDictionaryAsync(c => c.CategoryId, c => c.CategoryName);
            var subcategories = await _context.Subcategories.ToDictionaryAsync(s => s.SubcategoryId, s => s.SubcategoryName);

            var orderItemsQuery = _context.OrderItems
                .Include(oi => oi.Order)
                .AsQueryable();

            if (startDate != DateTime.MinValue)
                orderItemsQuery = orderItemsQuery.Where(oi => oi.Order.OrderDate >= startDate);

            var menuStatsRaw = await orderItemsQuery
                .GroupBy(oi => oi.DishId)
                .Select(g => new DishStat
                {
                    DishId = g.Key,
                    Value = g.Sum(oi => oi.Quantity)
                })
                .ToListAsync();

            await EnrichDishStats(menuStatsRaw, categories, subcategories);
            MenuStats = BuildCategoryHierarchy(menuStatsRaw);

            var favQuery = _context.FavoriteDishes
                .Include(f => f.DishVariant)
                .AsQueryable();

            if (startDate != DateTime.MinValue)
                favQuery = favQuery.Where(f => f.AddedDate >= startDate);

            var favStatsRaw = await favQuery
                .Where(f => f.DishVariant != null)
                .GroupBy(f => f.DishVariant.DishId)
                .Select(g => new DishStat
                {
                    DishId = g.Key,
                    Value = g.Count()
                })
                .ToListAsync();

            await EnrichDishStats(favStatsRaw, categories, subcategories);
            FavoriteStats = BuildCategoryHierarchy(favStatsRaw);

            var reviewQuery = _context.ReviewDishes.AsQueryable();
            if (startDate != DateTime.MinValue)
                reviewQuery = reviewQuery.Where(r => r.ReviewDate >= startDate);

            var reviewStatsRaw = await reviewQuery
                .GroupBy(r => r.DishId)
                .Select(g => new DishStat
                {
                    DishId = g.Key,
                    Value = (decimal)g.Average(r => r.Rating)
                })
                .ToListAsync();

            await EnrichDishStats(reviewStatsRaw, categories, subcategories);
            ReviewStats = BuildCategoryHierarchy(reviewStatsRaw);
        }

        private async Task EnrichDishStats(List<DishStat> stats, Dictionary<int, string> categories, Dictionary<int, string> subcategories)
        {
            var dishIds = stats.Select(s => s.DishId).Distinct().ToList();
            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .ToDictionaryAsync(d => d.DishId, d => new { d.DishName, d.CategoryId, d.SubcategoryId });

            foreach (var stat in stats)
            {
                if (dishes.TryGetValue(stat.DishId, out var dish))
                {
                    stat.DishName = dish.DishName;
                    stat.CategoryId = dish.CategoryId;
                    stat.CategoryName = categories.GetValueOrDefault(dish.CategoryId, "Без категории");
                    stat.SubcategoryId = dish.SubcategoryId;
                    stat.SubcategoryName = dish.SubcategoryId.HasValue
                        ? subcategories.GetValueOrDefault(dish.SubcategoryId.Value, "Без подкатегории")
                        : "Без подкатегории";
                }
            }
        }

        private List<CategoryStatForChart> BuildCategoryHierarchy(List<DishStat> stats)
        {
            var result = new List<CategoryStatForChart>();

            var groupedByCategory = stats
                .Where(s => s.CategoryId > 0)
                .GroupBy(s => new { s.CategoryId, s.CategoryName })
                .OrderBy(g => g.Key.CategoryId);

            foreach (var catGroup in groupedByCategory)
            {
                var catStat = new CategoryStatForChart
                {
                    CategoryId = catGroup.Key.CategoryId,
                    CategoryName = catGroup.Key.CategoryName,
                    TopDishes = catGroup.OrderByDescending(s => s.Value).Take(3).ToList()
                };

                var subGroups = catGroup
                    .Where(s => !string.IsNullOrEmpty(s.SubcategoryName))
                    .GroupBy(s => s.SubcategoryName)
                    .Select(g => new SubcategoryStatForChart
                    {
                        SubcategoryName = g.Key,
                        Value = g.Sum(s => s.Value)
                    })
                    .OrderByDescending(s => s.Value)
                    .ToList();

                catStat.Subcategories = subGroups;
                result.Add(catStat);
            }

            return result;
        }

        public async Task<IActionResult> OnGetAsync(string? tab, string? bonusSubTab, string? cashbackSubTab, string? addressesSubTab, string? period, string? statTab, string? mainTab)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Authorization/Login");
            int userId = int.Parse(userIdStr);

            ActiveTab = tab ?? "personal";
            BonusSubTab = bonusSubTab ?? "bonus";
            CashbackSubTab = cashbackSubTab ?? "regular";
            AddressesSubTab = addressesSubTab ?? "delivery";
            StatPeriod = period ?? "all";
            StatTab = statTab ?? "menu";

            var appUser = await _context.AppUsers
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.AppUserId == userId);

            if (appUser?.UserProfile == null) return NotFound();

            FirstName = appUser.UserProfile.FirstName ?? string.Empty;
            LastName = appUser.UserProfile.LastName ?? string.Empty;
            Email = appUser.UserProfile.Email ?? string.Empty;
            Phone = appUser.UserProfile.Phone;
            SelectedPickupPointId = appUser.SelectedPickupPointId;
            DefaultDeliveryAddress = HttpContext.Session.GetString("DefaultDeliveryAddress");

            UserAddresses = await _context.AddressDeliveries
                .Where(a => a.AppUserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.AddressDeliveryId)
                .ToListAsync();

            UserPayments = await _context.Payments
                .Where(p => p.AppUserId == userId)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.PaymentId)
                .ToListAsync();

            AllPointPickups = await _context.PointPickups
                .Where(p => p.IsActive)
                .OrderBy(p => p.PointName)
                .ToListAsync();

            MapLocations = AllPointPickups
                .Where(p => p.Latitude.HasValue && p.Longitude.HasValue)
                .Select(p => new MapLocation
                {
                    Id = p.PointPickupId,
                    Name = p.PointName,
                    Address = p.Address,
                    Latitude = (double)p.Latitude.Value,
                    Longitude = (double)p.Longitude.Value,
                    Phone = p.Phone,
                    WorkHours = p.WorkHours,
                    IconType = "red"
                }).ToList();

            BonusTotal = await _context.Bonuses
                .Where(b => b.AppUserId == userId && b.IsActive)
                .SumAsync(b => b.BonusAmount);

            CashbackTotal = await _context.BonusTransactions
                .Where(bt => bt.AppUserId == userId && bt.Type == "cashback")
                .SumAsync(bt => (decimal?)bt.Amount) ?? 0;

            var today = DateTime.Today;

            InitializeCashbackCategories();

            try
            {
                var userCashbacks = await _context.Cashbacks
                    .Where(c => c.AppUserId == userId && c.IsActive)
                    .Select(c => c.CategoryId)
                    .ToListAsync();

                if (userCashbacks.Any())
                {
                    SelectedCategoryIds = userCashbacks;
                    HttpContext.Session.SetString("SelectedCashbackIds", string.Join(",", SelectedCategoryIds));
                }
                else
                {
                    SelectedCategoryIds = new List<int> { 1, 3, 11 };
                    HttpContext.Session.SetString("SelectedCashbackIds", string.Join(",", SelectedCategoryIds));
                    await SaveCashbacksToDatabase(userId, SelectedCategoryIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка загрузки категорий кешбэка userId={userId}");
                var s = HttpContext.Session.GetString("SelectedCashbackIds");
                SelectedCategoryIds = !string.IsNullOrEmpty(s) ? s.Split(',').Select(int.Parse).ToList() : new List<int> { 1, 3, 11 };
            }

            // ========== ЗАГРУЗКА ПРОМОКОДОВ ==========
            var savedPromocodes = await _context.BonusTransactions
                .Where(bt => bt.AppUserId == userId && bt.Type == "saved_promo" && (bt.ExpiryDate == null || bt.ExpiryDate >= DateTime.Now))
                .OrderByDescending(bt => bt.CreatedDate)
                .ToListAsync();

            ActivePromocodesList = savedPromocodes.Select(bt => new PromocodeViewModel
            {
                Code = bt.Source ?? string.Empty,
                DiscountPercent = 0,
                ValidFrom = bt.CreatedDate,
                ValidTo = bt.ExpiryDate ?? DateTime.Now.AddDays(30),
                IsActive = true,
                IsSaved = true,
                SavedAt = bt.CreatedDate
            }).Where(p => !string.IsNullOrEmpty(p.Code)).ToList();

            var globalPromos = await _context.Promocodes
                .Where(p => p.IsActive && p.ValidFrom <= today && p.ValidTo >= today)
                .ToListAsync();

            var existingCodes = ActivePromocodesList.Select(p => p.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var promo in globalPromos)
            {
                if (!existingCodes.Contains(promo.PromoCode))
                {
                    ActivePromocodesList.Add(new PromocodeViewModel
                    {
                        Code = promo.PromoCode,
                        DiscountPercent = promo.DiscountPercent,
                        ValidFrom = promo.ValidFrom,
                        ValidTo = promo.ValidTo,
                        IsActive = true,
                        IsSaved = false,
                        SavedAt = null
                    });
                }
            }

            UsedPromocodesList = await _context.Promocodes
                .Where(p => !p.IsActive || p.ValidTo < today)
                .Take(5)
                .Select(p => new PromocodeViewModel
                {
                    Code = p.PromoCode,
                    DiscountPercent = p.DiscountPercent,
                    ValidFrom = p.ValidFrom,
                    ValidTo = p.ValidTo,
                    IsActive = false,
                    IsSaved = false,
                    SavedAt = null
                }).ToListAsync();

            if (ActiveTab == "orders")
            {
                var allOrders = await _context.Orders
                    .Include(o => o.PointPickup)
                    .Where(o => o.AppUserId == userId)
                    .Select(o => new OrderViewModel
                    {
                        OrderId = o.OrderId,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        TotalAmount = o.TotalAmount,
                        Comment = o.Comment,
                        PointPickupName = o.PointPickup != null ? o.PointPickup.PointName : null,
                        PointPickupAddress = o.PointPickup != null ? o.PointPickup.Address : null
                    })
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                NewOrders = allOrders.Where(o => o.Status == "Новый" || o.Status == "Ожидает").ToList();
                OrderHistory = allOrders.Where(o => o.Status != "Новый" && o.Status != "Ожидает").ToList();
            }

            var allBonuses = await _context.Bonuses
                .Where(b => b.AppUserId == userId && b.IsActive)
                .OrderByDescending(b => b.ValidFrom)
                .Take(20)
                .ToListAsync();

            var regularHistory = allBonuses.Select(b => new BonusHistoryItem
            {
                Amount = b.BonusAmount,
                AccrualDate = b.ValidFrom ?? DateTime.Now,
                ExpiryDate = b.ValidTo ?? DateTime.Now.AddDays(180),
                Source = "Обычный кешбэк",
                Type = "bonus"
            }).ToList();

            var cashbackHistories = await _context.BonusTransactions
                .Where(bt => bt.AppUserId == userId && bt.Type == "cashback")
                .OrderByDescending(bt => bt.CreatedDate)
                .Take(20)
                .ToListAsync();

            BoostedBonusHistory = cashbackHistories.Select(bt => new BonusHistoryItem
            {
                Amount = bt.Amount,
                AccrualDate = bt.CreatedDate,
                ExpiryDate = bt.ExpiryDate ?? bt.CreatedDate.AddDays(180),
                Source = bt.Source ?? "Повышенный кешбэк за категорию",
                Type = "cashback"
            }).ToList();

            BonusHistory = regularHistory.Concat(BoostedBonusHistory).OrderByDescending(b => b.AccrualDate).ToList();

            var role = HttpContext.Session.GetInt32("UserRole");
            if (role == 4 && ActiveTab == "statistics")
            {
                DateTime startDate = StatPeriod switch
                {
                    "day" => DateTime.Today,
                    "week" => DateTime.Today.AddDays(-7),
                    "month" => DateTime.Today.AddMonths(-1),
                    "year" => DateTime.Today.AddYears(-1),
                    _ => DateTime.MinValue
                };
                await LoadStatistics(StatPeriod, StatTab);
                await LoadExtendedStatistics(startDate);
            }

            return Page();
        }

        public async Task<IActionResult> OnGetStatistics(string period, string tab)
        {
            var role = HttpContext.Session.GetInt32("UserRole");
            if (role != 4) return new JsonResult(new { error = "Доступ запрещён" }) { StatusCode = 403 };

            StatPeriod = period;
            StatTab = tab;
            await LoadStatistics(period, tab);

            var result = tab switch
            {
                "menu" => MenuStats,
                "favorite" => FavoriteStats,
                "review" => ReviewStats,
                _ => MenuStats
            };
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetGetSavedPromocodes()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return new JsonResult(new List<string>());

            int userId = int.Parse(userIdStr);

            var promoCodes = await _context.BonusTransactions
                .Where(bt => bt.AppUserId == userId && bt.Type == "saved_promo" && (bt.ExpiryDate == null || bt.ExpiryDate >= DateTime.Now))
                .Select(bt => bt.Source ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToListAsync();

            return new JsonResult(promoCodes);
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveCategories(List<int> selectedCategoryIds)
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid))
            {
                TempData["CashbackError"] = "Не авторизован";
                return RedirectToPage(new { tab = "userbonuses", bonusSubTab = "bonus", cashbackSubTab = "boosted" });
            }

            int userId = int.Parse(uid);

            if (selectedCategoryIds == null || selectedCategoryIds.Count == 0)
            {
                TempData["CashbackError"] = "Выберите хотя бы одну категорию!";
                return RedirectToPage(new { tab = "userbonuses", bonusSubTab = "bonus", cashbackSubTab = "boosted" });
            }

            if (selectedCategoryIds.Count > 3)
            {
                TempData["CashbackError"] = "Можно выбрать максимум 3 категории!";
                return RedirectToPage(new { tab = "userbonuses", bonusSubTab = "bonus", cashbackSubTab = "boosted" });
            }

            try
            {
                var valid = selectedCategoryIds.Where(id => id >= 1 && id <= 11).ToList();
                if (valid.Count != selectedCategoryIds.Count)
                {
                    TempData["CashbackError"] = "Несуществующие категории!";
                    return RedirectToPage(new { tab = "userbonuses", bonusSubTab = "bonus", cashbackSubTab = "boosted" });
                }

                InitializeCashbackCategories();
                HttpContext.Session.SetString("SelectedCashbackIds", string.Join(",", selectedCategoryIds));
                await SaveCashbacksToDatabase(userId, selectedCategoryIds);

                TempData["CashbackSuccess"] = "Категории кешбэка сохранены!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка сохранения категорий userId={userId}");
                TempData["CashbackError"] = "Ошибка: " + ex.Message;
            }

            return RedirectToPage(new { tab = "userbonuses", bonusSubTab = "bonus", cashbackSubTab = "boosted" });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostTogglePickupPoint([FromBody] SavePickupPointModel model)
        {
            if (model == null || model.PointPickupId <= 0)
            {
                return new JsonResult(new { success = false, message = "Неверные данные" });
            }

            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false, message = "Не авторизован" });
            int userId = int.Parse(uid);

            var appUser = await _context.AppUsers.FindAsync(userId);
            if (appUser == null) return new JsonResult(new { success = false, message = "Пользователь не найден" });

            if (appUser.SelectedPickupPointId == model.PointPickupId)
            {
                appUser.SelectedPickupPointId = null;
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, selected = false });
            }
            else
            {
                appUser.SelectedPickupPointId = model.PointPickupId;
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, selected = true });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSavePickupPoint([FromBody] SavePickupPointModel model)
        {
            if (model == null || model.PointPickupId <= 0)
            {
                return new JsonResult(new { success = false, message = "Неверные данные" });
            }

            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false, message = "Не авторизован" });
            int userId = int.Parse(uid);

            var appUser = await _context.AppUsers.FindAsync(userId);
            if (appUser == null) return new JsonResult(new { success = false, message = "Пользователь не найден" });

            appUser.SelectedPickupPointId = model.PointPickupId;
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveDeliveryAddress([FromBody] SaveDeliveryAddressModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Street))
            {
                return new JsonResult(new { success = false, message = "Неверные данные адреса" });
            }

            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false, message = "Не авторизован" });
            int userId = int.Parse(uid);

            var city = model.City?.Trim() ?? "";
            var street = model.Street?.Trim() ?? "";
            var fullStreet = street;

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
            addr.Address = fullStreet;
            addr.Apartment = model.Apartment;
            addr.Entrance = model.Entrance;
            addr.Floor = model.Floor;
            addr.Domofon = model.Domofon;
            addr.Comment = model.Comment;
            addr.IsDefault = model.IsDefault;
            await _context.SaveChangesAsync();

            var details = new List<string>();
            if (!string.IsNullOrEmpty(model.Entrance)) details.Add($"подъезд {model.Entrance.Trim()}");
            if (model.Floor.HasValue && model.Floor > 0) details.Add($"этаж {model.Floor}");
            if (!string.IsNullOrEmpty(model.Apartment)) details.Add($"кв. {model.Apartment.Trim()}");
            if (!string.IsNullOrEmpty(model.Domofon)) details.Add($"домофон {model.Domofon.Trim()}");
            var fullWithDetails = (city.Length > 0 ? city + ", " : "") + fullStreet + (details.Any() ? ", " + string.Join(", ", details) : "");
            HttpContext.Session.SetString("DefaultDeliveryAddress", fullWithDetails);

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteDeliveryAddress(int id)
        {
            var addr = await _context.AddressDeliveries.FindAsync(id);
            if (addr != null)
            {
                _context.AddressDeliveries.Remove(addr);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { tab = "addresses", addressesSubTab = "delivery" });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostToggleDefaultAddress([FromBody] ToggleDefaultAddressModel model)
        {
            if (model == null || model.AddressId <= 0)
            {
                return new JsonResult(new { success = false });
            }

            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false });
            int userId = int.Parse(uid);

            if (model.IsDefault)
            {
                var defs = await _context.AddressDeliveries.Where(a => a.AppUserId == userId && a.IsDefault).ToListAsync();
                foreach (var a in defs) a.IsDefault = false;
            }

            var addr = await _context.AddressDeliveries.FindAsync(model.AddressId);
            if (addr != null)
            {
                addr.IsDefault = model.IsDefault;
                await _context.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostSetDefaultAddress(int id)
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return RedirectToPage();
            int userId = int.Parse(uid);

            var defs = await _context.AddressDeliveries.Where(a => a.AppUserId == userId && a.IsDefault).ToListAsync();
            foreach (var a in defs) a.IsDefault = false;

            var addr = await _context.AddressDeliveries.FindAsync(id);
            if (addr != null)
            {
                addr.IsDefault = true;
                var fullWithDetails = (string.IsNullOrEmpty(addr.City) ? "" : addr.City + ", ") + addr.Address;
                HttpContext.Session.SetString("DefaultDeliveryAddress", fullWithDetails);
            }
            await _context.SaveChangesAsync();
            return RedirectToPage(new { tab = "addresses", addressesSubTab = "delivery" });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSavePayment([FromBody] SavePaymentModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CardNumber) || model.CardNumber.Length < 16)
            {
                return new JsonResult(new { success = false, message = "Неверные данные карты" });
            }

            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false, message = "Не авторизован" });
            int userId = int.Parse(uid);

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
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeletePayment(int id)
        {
            var p = await _context.Payments.FindAsync(id);
            if (p != null)
            {
                _context.Remove(p);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { tab = "payment" });
        }

        public async Task<IActionResult> OnPostSetDefaultPayment(int id)
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return RedirectToPage();
            int userId = int.Parse(uid);

            var defs = await _context.Payments.Where(p => p.AppUserId == userId && p.IsDefault).ToListAsync();
            foreach (var p in defs) p.IsDefault = false;

            var payment = await _context.Payments.FindAsync(id);
            if (payment != null) payment.IsDefault = true;
            await _context.SaveChangesAsync();
            return RedirectToPage(new { tab = "payment" });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostToggleDefaultPayment([FromBody] ToggleDefaultPaymentModel model)
        {
            if (model == null || model.PaymentId <= 0)
            {
                return new JsonResult(new { success = false });
            }

            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return new JsonResult(new { success = false });
            int userId = int.Parse(uid);

            if (model.IsDefault)
            {
                var defs = await _context.Payments.Where(p => p.AppUserId == userId && p.IsDefault).ToListAsync();
                foreach (var p in defs) p.IsDefault = false;
            }

            var payment = await _context.Payments.FindAsync(model.PaymentId);
            if (payment != null)
            {
                payment.IsDefault = model.IsDefault;
                await _context.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            var uid = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uid)) return RedirectToPage("/Authorization/Login");

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                ModelState.AddModelError("FirstName", "Имя обязательно");
                return Page();
            }
            if (string.IsNullOrWhiteSpace(LastName))
            {
                ModelState.AddModelError("LastName", "Фамилия обязательна");
                return Page();
            }
            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError("Email", "E-mail обязателен");
                return Page();
            }

            var user = await _context.AppUsers
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.AppUserId == int.Parse(uid));

            if (user?.UserProfile == null) return NotFound();

            user.UserProfile.FirstName = FirstName.Trim();
            user.UserProfile.LastName = LastName.Trim();
            user.UserProfile.Email = Email.Trim();
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", $"{FirstName.Trim()} {LastName.Trim()}");
            TempData["Success"] = "Профиль обновлён!";
            return RedirectToPage(new { tab = "personal" });
        }
    }
}