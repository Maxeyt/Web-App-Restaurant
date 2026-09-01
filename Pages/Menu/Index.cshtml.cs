using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Menu
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context) => _context = context;

        public List<MaxFood.Models.Category> Categories { get; set; } = new();
        public List<MaxFood.Models.Subcategory> Subcategories { get; set; } = new();
        public List<DishWithStats> Dishes { get; set; } = new();

        public HashSet<int> FavoriteVariantIds { get; set; } = new();
        public int? SelectedCategoryId { get; set; }
        public List<CategoryGroup> GroupedDishes { get; set; } = new();

        public Dictionary<int, decimal> CategoryRatings { get; set; } = new();
        public Dictionary<int, decimal> SubcategoryRatings { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public class DishWithStats
        {
            public int DishId { get; set; }
            public int CategoryId { get; set; }
            public int? SubcategoryId { get; set; }
            public string DishName { get; set; } = string.Empty;
            public string? ShortDescription { get; set; }
            public string? FullDescription { get; set; }
            public string? Ingredients { get; set; }
            public string? NutritionalValue { get; set; }
            public int? Calories { get; set; }
            public int? PreparationTime { get; set; }
            public string? DishImagePath { get; set; }
            public ICollection<DishVariant> DishVariants { get; set; } = new List<DishVariant>();
            public decimal AverageRating { get; set; }
            public decimal Price { get; set; }
            public bool IsFavorite { get; set; }
        }

        public class CategoryGroup
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public decimal? AverageRating { get; set; }
            public List<SubcategoryGroup> Subcategories { get; set; } = new();
        }

        public class SubcategoryGroup
        {
            public int? SubcategoryId { get; set; }
            public string SubcategoryName { get; set; } = string.Empty;
            public decimal? AverageRating { get; set; }
            public List<DishWithStats> Dishes { get; set; } = new();
        }

        private static readonly Dictionary<int, string> _categoryIcons = new()
        {
            { 1, "🍕" }, { 2, "🍔" }, { 3, "🍣" }, { 4, "🍜" }, { 5, "🔥" },
            { 6, "🍳" }, { 7, "🥣" }, { 8, "🥗" }, { 9, "🍟" }, { 10, "🍰" }, { 11, "🥤" }
        };

        private static string GetCategoryIconStatic(int categoryId) =>
            _categoryIcons.TryGetValue(categoryId, out var icon) ? icon : "📁";

        public async Task<IActionResult> OnGetSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
                return new JsonResult(new List<object>());

            term = term.ToLower();

            var allDishes = await _context.Dishes
                .Include(d => d.DishVariants)
                .Include(d => d.Category)
                .Where(d => d.IsAvailable && d.DishName.ToLower().Contains(term))
                .OrderBy(d => d.CategoryId)
                .ThenBy(d => d.DishName)
                .Select(d => new
                {
                    d.DishId,
                    d.DishName,
                    d.ShortDescription,
                    Price = d.DishVariants.FirstOrDefault(v => v.SizeName == "Маленький") != null
                        ? d.DishVariants.First(v => v.SizeName == "Маленький").Price : 0,
                    VariantId = d.DishVariants.FirstOrDefault(v => v.SizeName == "Маленький") != null
                        ? d.DishVariants.First(v => v.SizeName == "Маленький").DishVariantId : 0,
                    Icon = d.Category != null ? GetCategoryIconStatic(d.Category.CategoryId) : "📁"
                })
                .ToListAsync();

            var result = allDishes
                .GroupBy(d => d.Icon)
                .SelectMany(g => g.Take(5))
                .Take(55)
                .ToList();

            return new JsonResult(result);
        }

        public async Task OnGetAsync(int? categoryId = null)
        {
            SelectedCategoryId = categoryId;

            Categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            Subcategories = await _context.Subcategories
                .AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync();

            IQueryable<MaxFood.Models.Dish> query = _context.Dishes
                .AsNoTracking()
                .Include(d => d.DishVariants)
                .Where(d => d.IsAvailable);

            if (!string.IsNullOrEmpty(SearchTerm))
                query = query.Where(d => d.DishName.ToLower().Contains(SearchTerm.ToLower()));

            if (categoryId.HasValue)
                query = query.Where(d => d.CategoryId == categoryId.Value);

            var dishes = await query.Select(d => new DishWithStats
            {
                DishId = d.DishId,
                CategoryId = d.CategoryId,
                SubcategoryId = d.SubcategoryId,
                DishName = d.DishName,
                ShortDescription = d.ShortDescription,
                FullDescription = d.FullDescription,
                Ingredients = d.Ingredients,
                NutritionalValue = d.NutritionalValue,
                Calories = d.Calories,
                PreparationTime = d.PreparationTime,
                DishImagePath = d.ImagePath,
                DishVariants = d.DishVariants,
                Price = d.DishVariants.FirstOrDefault(v => v.SizeName == "Маленький") != null
                    ? d.DishVariants.First(v => v.SizeName == "Маленький").Price : 0,
                IsFavorite = false
            }).ToListAsync();

            var dishIds = dishes.Select(d => d.DishId).ToList();

            // Рейтинги блюд
            var dishRatings = await _context.ReviewDishes
                .Where(r => dishIds.Contains(r.DishId))
                .GroupBy(r => r.DishId)
                .Select(g => new { DishId = g.Key, AvgRating = g.Average(r => (decimal)r.Rating) })
                .ToDictionaryAsync(x => x.DishId, x => Math.Round(x.AvgRating, 1));

            foreach (var dish in dishes)
                dish.AverageRating = dishRatings.TryGetValue(dish.DishId, out var avg) ? avg : 0m;

            // Рейтинги подкатегорий
            var subcategoryIds = dishes.Where(d => d.SubcategoryId.HasValue).Select(d => d.SubcategoryId.Value).Distinct().ToList();
            foreach (var subId in subcategoryIds)
            {
                var subDishIds = dishes.Where(d => d.SubcategoryId == subId).Select(d => d.DishId).ToList();
                var subRatingValue = await _context.ReviewDishes
                    .Where(r => subDishIds.Contains(r.DishId))
                    .AverageAsync(r => (decimal?)r.Rating) ?? 0;
                SubcategoryRatings[subId] = Math.Round(subRatingValue, 1);
            }

            // Рейтинги категорий
            var categoryIds = dishes.Select(d => d.CategoryId).Distinct().ToList();
            foreach (var catId in categoryIds)
            {
                var catDishIds = dishes.Where(d => d.CategoryId == catId).Select(d => d.DishId).ToList();
                var catRatingValue = await _context.ReviewDishes
                    .Where(r => catDishIds.Contains(r.DishId))
                    .AverageAsync(r => (decimal?)r.Rating) ?? 0;
                CategoryRatings[catId] = Math.Round(catRatingValue, 1);
            }

            // Избранное
            var userIdString = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
            {
                FavoriteVariantIds = await _context.FavoriteDishes
                    .Where(f => f.AppUserId == userId)
                    .Select(f => f.DishVariantId)
                    .ToHashSetAsync();

                foreach (var dish in dishes)
                {
                    var smallVariant = dish.DishVariants.FirstOrDefault(v => v.SizeName == "Маленький");
                    if (smallVariant != null && FavoriteVariantIds.Contains(smallVariant.DishVariantId))
                        dish.IsFavorite = true;
                }
            }

            Dishes = SortDishes(dishes);
            GroupedDishes = BuildGroupedDishes(Dishes);
        }

        private List<DishWithStats> SortDishes(List<DishWithStats> dishes)
        {
            if (string.IsNullOrEmpty(SortOrder)) return dishes;
            return SortOrder switch
            {
                "name_asc" => dishes.OrderBy(d => d.DishName).ToList(),
                "name_desc" => dishes.OrderByDescending(d => d.DishName).ToList(),
                "price_asc" => dishes.OrderBy(d => d.Price).ToList(),
                "price_desc" => dishes.OrderByDescending(d => d.Price).ToList(),
                _ => dishes
            };
        }

        private List<CategoryGroup> BuildGroupedDishes(List<DishWithStats> dishes)
        {
            var grouped = new List<CategoryGroup>();

            foreach (var category in Categories)
            {
                var categoryDishes = dishes.Where(d => d.CategoryId == category.CategoryId).ToList();

                var subcategoryGroups = new List<SubcategoryGroup>();

                // Группа "Без подкатегории"
                var dishesWithoutSubcategory = categoryDishes.Where(d => !d.SubcategoryId.HasValue).ToList();
                if (dishesWithoutSubcategory.Any())
                {
                    subcategoryGroups.Add(new SubcategoryGroup
                    {
                        SubcategoryId = null,
                        SubcategoryName = "Без подкатегории",
                        AverageRating = 0,
                        Dishes = dishesWithoutSubcategory
                    });
                }

                // Группы с подкатегориями
                var subcategoryIds = categoryDishes.Where(d => d.SubcategoryId.HasValue).Select(d => d.SubcategoryId.Value).Distinct();
                foreach (var subId in subcategoryIds)
                {
                    var sub = Subcategories.FirstOrDefault(s => s.SubcategoryId == subId);
                    var subDishes = categoryDishes.Where(d => d.SubcategoryId == subId).ToList();

                    // Используем другое имя переменной — subRatingValue
                    SubcategoryRatings.TryGetValue(subId, out var subRatingValue);

                    subcategoryGroups.Add(new SubcategoryGroup
                    {
                        SubcategoryId = subId,
                        SubcategoryName = sub?.SubcategoryName ?? "Подкатегория",
                        AverageRating = subRatingValue,
                        Dishes = subDishes
                    });
                }

                subcategoryGroups = subcategoryGroups.OrderBy(s => s.SubcategoryName).ToList();

                // Рейтинг категории
                CategoryRatings.TryGetValue(category.CategoryId, out var catRatingValue);

                grouped.Add(new CategoryGroup
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    AverageRating = catRatingValue,
                    Subcategories = subcategoryGroups
                });
            }

            return grouped;
        }
    }
}