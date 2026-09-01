using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Dish
{
    public class EditModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EditModel(MaxFoodDBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public Models.Dish Dish { get; set; } = new Models.Dish();

        [BindProperty]
        public List<DishVariant> DishVariants { get; set; } = new List<DishVariant>();

        [BindProperty]
        public IFormFile? UploadedImage { get; set; }

        private bool IsAdminOrManager()
        {
            var roleId = HttpContext.Session.GetInt32("UserRole");
            return roleId == 4 || roleId == 3;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!IsAdminOrManager()) return Unauthorized();

            var dish = await _context.Dishes
                .Include(d => d.DishVariants)
                .FirstOrDefaultAsync(m => m.DishId == id);

            if (dish == null) return NotFound();

            Dish = dish;
            DishVariants = dish.DishVariants.ToList();

            var categories = await _context.Categories.ToListAsync();
            var subcategories = await _context.Subcategories.ToListAsync();

            ViewData["CategoryId"] = new SelectList(categories, "CategoryId", "CategoryName", dish.CategoryId);
            ViewData["SubcategoryId"] = new SelectList(subcategories, "SubcategoryId", "SubcategoryName", dish.SubcategoryId);

            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!IsAdminOrManager()) return Unauthorized();

            bool isValid = true;

            if (Dish.CategoryId == 0)
            {
                ModelState.AddModelError("Dish.CategoryId", "Выберите категорию");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(Dish.DishName))
            {
                ModelState.AddModelError("Dish.DishName", "Введите название блюда");
                isValid = false;
            }

            bool hasValidVariant = false;
            for (int i = 0; i < DishVariants.Count; i++)
            {
                var v = DishVariants[i];
                if (!string.IsNullOrEmpty(v.SizeName) && v.Price > 0 && v.Weight > 0)
                {
                    hasValidVariant = true;
                }
            }

            if (!hasValidVariant && DishVariants.Count > 0)
            {
                ModelState.AddModelError("DishVariants", "Добавьте хотя бы один полностью заполненный вариант");
                isValid = false;
            }

            string iconType = Request.Form["iconType"];
            string emojiValue = Request.Form["emojiValue"];
            string colorValue = Request.Form["colorValue"];

            // Загружаем существующее блюдо из БД
            var existingDish = await _context.Dishes
                .Include(d => d.DishVariants)
                .FirstOrDefaultAsync(d => d.DishId == Dish.DishId);

            if (existingDish == null)
            {
                return NotFound();
            }

            // Обновляем поля
            existingDish.CategoryId = Dish.CategoryId;
            existingDish.SubcategoryId = Dish.SubcategoryId;
            existingDish.DishName = Dish.DishName;
            existingDish.ShortDescription = Dish.ShortDescription;
            existingDish.FullDescription = Dish.FullDescription;
            existingDish.Ingredients = Dish.Ingredients;
            existingDish.NutritionalValue = Dish.NutritionalValue;
            existingDish.Calories = Dish.Calories;
            existingDish.PreparationTime = Dish.PreparationTime;
            existingDish.IsAvailable = Dish.IsAvailable;

            // Обработка изображения
            if (UploadedImage != null && UploadedImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "dishes");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                if (!string.IsNullOrEmpty(existingDish.ImagePath) && !existingDish.ImagePath.StartsWith("emoji:"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingDish.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(UploadedImage.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadedImage.CopyToAsync(fileStream);
                }

                existingDish.ImagePath = "/uploads/dishes/" + uniqueFileName;
            }
            else if (iconType == "emoji" && !string.IsNullOrEmpty(emojiValue))
            {
                if (!string.IsNullOrEmpty(existingDish.ImagePath) && !existingDish.ImagePath.StartsWith("emoji:"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingDish.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }
                existingDish.ImagePath = $"emoji:{emojiValue}|{colorValue}";
            }

            if (!isValid)
            {
                Dish = existingDish;
                var categories = await _context.Categories.ToListAsync();
                var subcategories = await _context.Subcategories.ToListAsync();
                ViewData["CategoryId"] = new SelectList(categories, "CategoryId", "CategoryName", existingDish.CategoryId);
                ViewData["SubcategoryId"] = new SelectList(subcategories, "SubcategoryId", "SubcategoryName", existingDish.SubcategoryId);
                return Page();
            }

            // Обновляем варианты
            var existingVariants = existingDish.DishVariants.ToList();

            foreach (var existing in existingVariants)
            {
                if (!DishVariants.Any(v => v.DishVariantId == existing.DishVariantId))
                {
                    _context.DishVariants.Remove(existing);
                }
            }

            foreach (var v in DishVariants)
            {
                if (v.DishVariantId == 0)
                {
                    if (!string.IsNullOrEmpty(v.SizeName) && v.Price > 0 && v.Weight > 0)
                    {
                        v.DishId = existingDish.DishId;
                        _context.DishVariants.Add(v);
                    }
                }
                else
                {
                    var existingVariant = await _context.DishVariants.FindAsync(v.DishVariantId);
                    if (existingVariant != null)
                    {
                        existingVariant.SizeName = v.SizeName;
                        existingVariant.Price = v.Price;
                        existingVariant.Weight = v.Weight;
                        existingVariant.IsAvailable = v.IsAvailable;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}