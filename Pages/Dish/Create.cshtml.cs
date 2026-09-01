using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Dish
{
    public class CreateModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CreateModel(MaxFoodDBContext context, IWebHostEnvironment webHostEnvironment)
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

        public List<MaxFood.Models.Category> CategoriesList { get; set; } = new List<MaxFood.Models.Category>();

        private bool IsAdminOrManager()
        {
            var roleId = HttpContext.Session.GetInt32("UserRole");
            return roleId == 4 || roleId == 3;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdminOrManager()) return Unauthorized();

            await LoadCategories();

            if (DishVariants.Count == 0)
            {
                DishVariants.Add(new DishVariant { SizeName = "", IsAvailable = true });
            }

            return Page();
        }

        public async Task<IActionResult> OnGetSubcategoriesByCategoryAsync(int categoryId)
        {
            var subcategories = await _context.Subcategories
                .Where(s => s.CategoryId == categoryId)
                .OrderBy(s => s.SubcategoryName)
                .Select(s => new { s.SubcategoryId, s.SubcategoryName })
                .ToListAsync();

            return new JsonResult(subcategories);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!IsAdminOrManager()) return Unauthorized();

            await LoadCategories();

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
                else if (!string.IsNullOrEmpty(v.SizeName) || v.Price > 0 || v.Weight > 0)
                {
                    ModelState.AddModelError($"DishVariants[{i}].SizeName", "Заполните все поля варианта");
                    isValid = false;
                }
            }

            if (!hasValidVariant)
            {
                ModelState.AddModelError("DishVariants", "Добавьте хотя бы один вариант");
                isValid = false;
            }

            string iconType = Request.Form["iconType"];
            string emojiValue = Request.Form["emojiValue"];
            string colorValue = Request.Form["colorValue"];

            if (UploadedImage != null && UploadedImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "dishes");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(UploadedImage.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadedImage.CopyToAsync(fileStream);
                }

                Dish.ImagePath = "/uploads/dishes/" + uniqueFileName;
            }
            else if (iconType == "emoji" && !string.IsNullOrEmpty(emojiValue))
            {
                Dish.ImagePath = $"emoji:{emojiValue}|{colorValue}";
            }
            else
            {
                ModelState.AddModelError("UploadedImage", "Выберите фото или эмодзи");
                isValid = false;
            }

            if (!isValid)
            {
                return Page();
            }

            _context.Dishes.Add(Dish);
            await _context.SaveChangesAsync();

            foreach (var v in DishVariants)
            {
                if (!string.IsNullOrEmpty(v.SizeName) && v.Price > 0 && v.Weight > 0)
                {
                    v.DishId = Dish.DishId;
                    _context.DishVariants.Add(v);
                }
            }
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task LoadCategories()
        {
            CategoriesList = await _context.Categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.CategoryName).ToListAsync();
        }
    }
}