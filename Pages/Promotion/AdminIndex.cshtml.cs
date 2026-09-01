using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Promotion
{
    public class AdminIndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AdminIndexModel> _logger;

        public AdminIndexModel(MaxFoodDBContext context, IWebHostEnvironment webHostEnvironment, ILogger<AdminIndexModel> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public List<MaxFood.Models.Promotion> PromotionsList { get; set; } = new();
        public List<MaxFood.Models.Promocode> PromocodesList { get; set; } = new();
        public List<CashbackCategoryItem> AllCashbackCategories { get; set; } = new();
        public string? ActiveSubTab { get; set; }

        public class CashbackCategoryItem
        {
            public int Id { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public decimal CashbackPercent { get; set; }
            public bool IsActive { get; set; }
        }

        public class SavePromotionModel
        {
            public int PromotionId { get; set; }
            public string PromotionName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal? DiscountPercent { get; set; }
            public string DiscountType { get; set; } = "percent";
            public string? PromocodeValue { get; set; }
            public bool IsActive { get; set; }
            public DateTime ValidFrom { get; set; }
            public DateTime ValidTo { get; set; }
            public string IconType { get; set; } = "emoji";
            public string IconColor { get; set; } = "#28a745";
            public string IconEmoji { get; set; } = "🎁";
            public string? ExistingImagePath { get; set; }
            public IFormFile? UploadedImage { get; set; }
        }

        private bool IsAdminOrManager()
        {
            var roleId = HttpContext.Session.GetInt32("UserRole");
            return roleId == 4 || roleId == 3;
        }

        private void InitCashbackCategories()
        {
            if (AllCashbackCategories.Any()) return;
            AllCashbackCategories = new List<CashbackCategoryItem>
            {
                new() { Id = 1, CategoryName = "Пицца", Icon = "🍕", CashbackPercent = 5, IsActive = true },
                new() { Id = 2, CategoryName = "Бургеры", Icon = "🍔", CashbackPercent = 3, IsActive = true },
                new() { Id = 3, CategoryName = "Роллы", Icon = "🍣", CashbackPercent = 7, IsActive = true },
                new() { Id = 4, CategoryName = "Лапша", Icon = "🍜", CashbackPercent = 4, IsActive = true },
                new() { Id = 5, CategoryName = "Шашлык", Icon = "🔥", CashbackPercent = 6, IsActive = true },
                new() { Id = 6, CategoryName = "Горячее", Icon = "🍳", CashbackPercent = 3, IsActive = true },
                new() { Id = 7, CategoryName = "Супы", Icon = "🥣", CashbackPercent = 4, IsActive = true },
                new() { Id = 8, CategoryName = "Салаты", Icon = "🥗", CashbackPercent = 5, IsActive = true },
                new() { Id = 9, CategoryName = "Закуски", Icon = "🍟", CashbackPercent = 3, IsActive = true },
                new() { Id = 10, CategoryName = "Десерты", Icon = "🍰", CashbackPercent = 5, IsActive = true },
                new() { Id = 11, CategoryName = "Напитки", Icon = "🥤", CashbackPercent = 10, IsActive = true }
            };
        }

        public async Task<IActionResult> OnGetAsync(string? tab)
        {
            if (!IsAdminOrManager())
                return RedirectToPage("/Authorization/Login");

            ActiveSubTab = tab ?? "promotions";
            InitCashbackCategories();

            if (ActiveSubTab == "promotions")
            {
                PromotionsList = await _context.Promotions.OrderByDescending(p => p.PromotionId).ToListAsync();
            }
            else if (ActiveSubTab == "promocodes")
            {
                PromocodesList = await _context.Promocodes.OrderByDescending(p => p.PromocodeId).ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnGetGetPromotion(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return new JsonResult(new { success = false });
            return new JsonResult(new
            {
                promo.PromotionId,
                promo.PromotionName,
                promo.Description,
                promo.DiscountPercent,
                promo.IsActive,
                promo.ValidFrom,
                promo.ValidTo,
                promo.IconColor,
                promo.IconEmoji,
                promo.IconImagePath,
                DiscountType = promo.DiscountType,
                PromocodeValue = promo.PromocodeValue
            });
        }

        public async Task<IActionResult> OnGetGetPromocode(int id)
        {
            var promocode = await _context.Promocodes.FindAsync(id);
            if (promocode == null)
                return new JsonResult(new { success = false });

            return new JsonResult(new
            {
                promocodeId = promocode.PromocodeId,
                promoCode = promocode.PromoCode,
                discountPercent = promocode.DiscountPercent,
                validFrom = promocode.ValidFrom.ToString("yyyy-MM-dd"),
                validTo = promocode.ValidTo.ToString("yyyy-MM-dd"),
                maxUses = promocode.MaxUses,
                isActive = promocode.IsActive
            });
        }

        public async Task<IActionResult> OnPostSavePromocode([FromBody] SavePromocodeModel model)
        {
            if (!IsAdminOrManager())
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            if (string.IsNullOrWhiteSpace(model.Code))
                return new JsonResult(new { success = false, message = "Введите код промокода" });

            if (model.DiscountPercent <= 0 || model.DiscountPercent > 100)
                return new JsonResult(new { success = false, message = "Скидка должна быть от 1 до 100%" });

            try
            {
                if (model.PromocodeId == 0)
                {
                    var existing = await _context.Promocodes.FirstOrDefaultAsync(p => p.PromoCode == model.Code);
                    if (existing != null)
                        return new JsonResult(new { success = false, message = "Промокод уже существует" });

                    _context.Promocodes.Add(new Promocode
                    {
                        PromoCode = model.Code.ToUpper(),
                        DiscountPercent = model.DiscountPercent,
                        ValidFrom = model.ValidFrom,
                        ValidTo = model.ValidTo,
                        MaxUses = model.MaxUses,
                        CurrentUses = 0,
                        IsActive = true
                    });
                }
                else
                {
                    var existing = await _context.Promocodes.FindAsync(model.PromocodeId);
                    if (existing == null)
                        return new JsonResult(new { success = false, message = "Промокод не найден" });

                    existing.PromoCode = model.Code.ToUpper();
                    existing.DiscountPercent = model.DiscountPercent;
                    existing.ValidFrom = model.ValidFrom;
                    existing.ValidTo = model.ValidTo;
                    existing.MaxUses = model.MaxUses;
                }
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения промокода");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostUpdatePromocode([FromBody] UpdatePromocodeModel model)
        {
            if (!IsAdminOrManager())
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            var promocode = await _context.Promocodes.FindAsync(model.PromocodeId);
            if (promocode == null)
                return new JsonResult(new { success = false, message = "Промокод не найден" });

            promocode.PromoCode = model.Code?.ToUpper() ?? promocode.PromoCode;
            promocode.DiscountPercent = model.DiscountPercent;
            promocode.ValidFrom = model.ValidFrom;
            promocode.ValidTo = model.ValidTo;
            promocode.MaxUses = model.MaxUses;

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeletePromocode(int id)
        {
            if (!IsAdminOrManager())
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            var promo = await _context.Promocodes.FindAsync(id);
            if (promo == null) return new JsonResult(new { success = false, message = "Промокод не найден" });
            _context.Promocodes.Remove(promo);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostSavePromotion([FromForm] SavePromotionModel model)
        {
            if (!IsAdminOrManager())
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            if (string.IsNullOrWhiteSpace(model.PromotionName))
                return new JsonResult(new { success = false, message = "Введите название акции" });

            try
            {
                string? savedImagePath = null;

                // Обработка загруженного изображения
                if (model.UploadedImage != null && model.UploadedImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "promotions");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.UploadedImage.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.UploadedImage.CopyToAsync(fileStream);
                    }

                    savedImagePath = "/uploads/promotions/" + uniqueFileName;
                }
                else if (model.IconType == "image" && !string.IsNullOrEmpty(model.ExistingImagePath))
                {
                    savedImagePath = model.ExistingImagePath;
                }

                if (model.PromotionId == 0)
                {
                    var promotion = new MaxFood.Models.Promotion
                    {
                        PromotionName = model.PromotionName.Trim(),
                        Description = model.Description,
                        DiscountPercent = model.DiscountPercent,
                        DiscountType = model.DiscountType,
                        PromocodeValue = model.PromocodeValue,
                        ValidFrom = model.ValidFrom,
                        ValidTo = model.ValidTo,
                        IsActive = model.IsActive,
                        IconColor = model.IconType == "image" ? null : model.IconColor,
                        IconEmoji = model.IconType == "image" ? null : model.IconEmoji,
                        IconImagePath = savedImagePath
                    };
                    _context.Promotions.Add(promotion);
                }
                else
                {
                    var existing = await _context.Promotions.FindAsync(model.PromotionId);
                    if (existing == null)
                        return new JsonResult(new { success = false, message = "Акция не найдена" });

                    // Если загружено новое изображение и было старое - удаляем старое
                    if (model.UploadedImage != null && !string.IsNullOrEmpty(existing.IconImagePath) && !existing.IconImagePath.StartsWith("emoji:"))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existing.IconImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    existing.PromotionName = model.PromotionName.Trim();
                    existing.Description = model.Description;
                    existing.DiscountPercent = model.DiscountPercent;
                    existing.DiscountType = model.DiscountType;
                    existing.PromocodeValue = model.PromocodeValue;
                    existing.ValidFrom = model.ValidFrom;
                    existing.ValidTo = model.ValidTo;
                    existing.IsActive = model.IsActive;

                    if (model.IconType == "image")
                    {
                        existing.IconColor = null;
                        existing.IconEmoji = null;
                        if (savedImagePath != null)
                            existing.IconImagePath = savedImagePath;
                    }
                    else
                    {
                        existing.IconColor = model.IconColor;
                        existing.IconEmoji = model.IconEmoji;
                        if (existing.IconImagePath != null && !existing.IconImagePath.StartsWith("emoji:"))
                        {
                            var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existing.IconImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                                System.IO.File.Delete(oldFilePath);
                        }
                        existing.IconImagePath = null;
                    }
                }
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения акции");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostDeletePromotion(int id)
        {
            if (!IsAdminOrManager())
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null)
                return new JsonResult(new { success = false, message = "Акция не найдена" });

            // Удаляем файл изображения, если он есть
            if (!string.IsNullOrEmpty(promo.IconImagePath) && !promo.IconImagePath.StartsWith("emoji:"))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, promo.IconImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.Promotions.Remove(promo);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostToggleCashbackCategory([FromBody] ToggleCashbackModel model)
        {
            if (!IsAdminOrManager())
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            _logger.LogInformation($"Категория {model.CategoryId} стала {(model.IsActive ? "активной" : "неактивной")}");
            return new JsonResult(new { success = true });
        }

        // ========== МОДЕЛИ ==========

        public class SavePromocodeModel
        {
            public int PromocodeId { get; set; }
            public string Code { get; set; } = string.Empty;
            public decimal DiscountPercent { get; set; }
            public DateTime ValidFrom { get; set; }
            public DateTime ValidTo { get; set; }
            public int? MaxUses { get; set; }
        }

        public class UpdatePromocodeModel
        {
            public int PromocodeId { get; set; }
            public string Code { get; set; } = string.Empty;
            public decimal DiscountPercent { get; set; }
            public DateTime ValidFrom { get; set; }
            public DateTime ValidTo { get; set; }
            public int? MaxUses { get; set; }
        }

        public class ToggleCashbackModel
        {
            public int CategoryId { get; set; }
            public bool IsActive { get; set; }
        }
    }
}