using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Review
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context) => _context = context;

        public List<Models.ReviewDish> ReviewDishes { get; set; } = new();
        public List<Models.ReviewRestaurant> ReviewRestaurants { get; set; } = new();
        public string ActiveTab { get; set; } = "dish";
        public Models.Dish? SelectedDish { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string? SelectedCategoryName { get; set; }
        public List<CategoryFilter> Categories { get; set; } = new();
        public List<ReviewGroup> ReviewGroups { get; set; } = new();

        // Словари для рейтингов категорий и подкатегорий
        public Dictionary<int, decimal> CategoryRatings { get; set; } = new();
        public Dictionary<int, decimal> SubcategoryRatings { get; set; } = new();

        public class CategoryFilter
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
        }

        public class ReviewGroup
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public decimal AverageRating { get; set; }
            public List<SubcategoryReviewGroup> SubcategoryGroups { get; set; } = new();
        }

        public class SubcategoryReviewGroup
        {
            public int? SubcategoryId { get; set; }
            public string SubcategoryName { get; set; } = string.Empty;
            public decimal AverageRating { get; set; }
            public List<Models.ReviewDish> Reviews { get; set; } = new();
        }

        private static readonly Dictionary<int, string> _categoryIcons = new()
        {
            { 1, "🍕" }, { 2, "🍔" }, { 3, "🍣" }, { 4, "🍜" }, { 5, "🔥" },
            { 6, "🍳" }, { 7, "🥣" }, { 8, "🥗" }, { 9, "🍟" }, { 10, "🍰" }, { 11, "🥤" }
        };

        public async Task OnGetAsync(string type = "dish", int? dishId = null, int? categoryId = null)
        {
            ActiveTab = type == "restaurant" ? "restaurant" : "dish";
            SelectedCategoryId = categoryId;

            var categoriesFromDb = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new { c.CategoryId, c.CategoryName })
                .OrderBy(c => c.CategoryId)
                .ToListAsync();

            Categories = categoriesFromDb.Select(c => new CategoryFilter
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Icon = _categoryIcons.ContainsKey(c.CategoryId) ? _categoryIcons[c.CategoryId] : "📁"
            }).ToList();

            if (ActiveTab == "dish")
            {
                IQueryable<Models.ReviewDish> query = _context.ReviewDishes
                    .Include(r => r.AppUser).ThenInclude(u => u.UserProfile)
                    .Include(r => r.Dish).ThenInclude(d => d.Category)
                    .Include(r => r.Dish).ThenInclude(d => d.Subcategory)
                    .Where(r => r.IsModerated);

                if (dishId.HasValue)
                {
                    query = query.Where(r => r.DishId == dishId.Value);
                    SelectedDish = await _context.Dishes
                        .Include(d => d.Category)
                        .FirstOrDefaultAsync(d => d.DishId == dishId.Value);
                }
                else if (categoryId.HasValue)
                {
                    query = query.Where(r => r.Dish != null && r.Dish.CategoryId == categoryId.Value);
                    SelectedCategoryName = await _context.Categories
                        .Where(c => c.CategoryId == categoryId.Value)
                        .Select(c => c.CategoryName)
                        .FirstOrDefaultAsync();
                }

                var reviews = await query.OrderByDescending(r => r.ReviewDate).ToListAsync();

                // ========== РАСЧЁТ РЕЙТИНГОВ (среднее арифметическое, не умноженное на 10) ==========
                var allDishes = await _context.Dishes.Select(d => new { d.DishId, d.CategoryId, d.SubcategoryId }).ToListAsync();

                var allRatings = await _context.ReviewDishes
                    .Where(r => r.IsModerated)
                    .GroupBy(r => r.DishId)
                    .Select(g => new { DishId = g.Key, AvgRating = g.Average(r => (decimal)r.Rating) })
                    .ToDictionaryAsync(x => x.DishId, x => x.AvgRating);

                var categoryRatingsTemp = new Dictionary<int, List<decimal>>();
                var subcategoryRatingsTemp = new Dictionary<int, List<decimal>>();

                foreach (var dish in allDishes)
                {
                    if (allRatings.TryGetValue(dish.DishId, out var rating))
                    {
                        if (!categoryRatingsTemp.ContainsKey(dish.CategoryId))
                            categoryRatingsTemp[dish.CategoryId] = new List<decimal>();
                        categoryRatingsTemp[dish.CategoryId].Add(rating);

                        if (dish.SubcategoryId.HasValue)
                        {
                            if (!subcategoryRatingsTemp.ContainsKey(dish.SubcategoryId.Value))
                                subcategoryRatingsTemp[dish.SubcategoryId.Value] = new List<decimal>();
                            subcategoryRatingsTemp[dish.SubcategoryId.Value].Add(rating);
                        }
                    }
                }

                foreach (var cat in categoryRatingsTemp)
                    CategoryRatings[cat.Key] = Math.Round(cat.Value.Average(), 1);

                foreach (var sub in subcategoryRatingsTemp)
                    SubcategoryRatings[sub.Key] = Math.Round(sub.Value.Average(), 1);
                // ========== КОНЕЦ РАСЧЁТА ==========

                var grouped = reviews
                    .Where(r => r.Dish != null)
                    .GroupBy(r => new { r.Dish!.CategoryId, CategoryName = r.Dish.Category?.CategoryName ?? "Без категории" })
                    .OrderBy(g => g.Key.CategoryId);

                foreach (var catGroup in grouped)
                {
                    // Средний рейтинг категории (по всем блюдам категории, а не по отзывам в выборке)
                    var catRating = CategoryRatings.TryGetValue(catGroup.Key.CategoryId, out var cr) ? cr : 0m;

                    var rg = new ReviewGroup
                    {
                        CategoryId = catGroup.Key.CategoryId,
                        CategoryName = catGroup.Key.CategoryName,
                        Icon = _categoryIcons.ContainsKey(catGroup.Key.CategoryId) ? _categoryIcons[catGroup.Key.CategoryId] : "📁",
                        AverageRating = catRating
                    };

                    var subGroups = catGroup
                        .GroupBy(r => new { r.Dish?.SubcategoryId, SubcategoryName = r.Dish?.Subcategory?.SubcategoryName ?? "Без подкатегории" })
                        .OrderBy(g => g.Key.SubcategoryName);

                    foreach (var sub in subGroups)
                    {
                        var subRating = SubcategoryRatings.TryGetValue(sub.Key.SubcategoryId ?? 0, out var sr) ? sr : 0m;

                        rg.SubcategoryGroups.Add(new SubcategoryReviewGroup
                        {
                            SubcategoryId = sub.Key.SubcategoryId,
                            SubcategoryName = sub.Key.SubcategoryName,
                            AverageRating = subRating,
                            Reviews = sub.ToList()
                        });
                    }

                    ReviewGroups.Add(rg);
                }
            }
            else
            {
                ReviewRestaurants = await _context.ReviewRestaurants
                    .Include(r => r.AppUser).ThenInclude(u => u.UserProfile)
                    .Where(r => r.IsModerated)
                    .OrderByDescending(r => r.ReviewDate)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnGetSearchDishesForReview(string term = "")
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 1)
                return new JsonResult(new List<object>());

            term = term.ToLower();

            var allDishes = await _context.Dishes
                .Where(d => d.IsAvailable && d.DishName.ToLower().Contains(term))
                .Select(d => new {
                    d.DishId,
                    d.DishName,
                    d.CategoryId,
                    d.ShortDescription,
                    Price = d.DishVariants.FirstOrDefault(v => v.SizeName == "Маленький") != null
                        ? d.DishVariants.First(v => v.SizeName == "Маленький").Price
                        : 0
                })
                .OrderBy(d => d.CategoryId)
                .ThenBy(d => d.DishName)
                .ToListAsync();

            var dishesWithIcons = allDishes.Select(d => new {
                d.DishId,
                d.DishName,
                d.CategoryId,
                Icon = _categoryIcons.ContainsKey(d.CategoryId) ? _categoryIcons[d.CategoryId] : "📁",
                d.ShortDescription,
                d.Price
            }).ToList();

            var result = dishesWithIcons
                .GroupBy(d => d.Icon)
                .SelectMany(g => g.Take(5))
                .Take(55)
                .ToList();

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostCreateDishReview([FromBody] CreateDishReviewModel input)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var existing = await _context.ReviewDishes
                .FirstOrDefaultAsync(r => r.AppUserId == userId && r.DishId == input.DishId);
            if (existing != null)
                return new JsonResult(new { success = false, message = "Вы уже оставляли отзыв на это блюдо" });

            var review = new Models.ReviewDish
            {
                AppUserId = userId,
                DishId = input.DishId,
                Rating = input.Rating,
                Comment = input.Comment,
                ReviewDate = DateTime.Now,
                IsModerated = true
            };
            _context.ReviewDishes.Add(review);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostUpdateDishReview([FromBody] UpdateDishReviewModel input)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var review = await _context.ReviewDishes.FindAsync(input.ReviewDishId);
            if (review == null) return new JsonResult(new { success = false, message = "Отзыв не найден" });
            if (review.AppUserId != userId) return new JsonResult(new { success = false, message = "Нет прав" });

            review.Rating = input.Rating;
            review.Comment = input.Comment;
            review.IsModerated = false;
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteDishReview(int id)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var review = await _context.ReviewDishes.FindAsync(id);
            if (review == null) return new JsonResult(new { success = false, message = "Отзыв не найден" });
            if (review.AppUserId != userId) return new JsonResult(new { success = false, message = "Нет прав" });

            _context.ReviewDishes.Remove(review);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostCreateRestaurantReview([FromBody] CreateRestaurantReviewModel input)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var review = new Models.ReviewRestaurant
            {
                AppUserId = userId,
                Rating = input.Rating,
                Comment = input.Comment,
                ReviewDate = DateTime.Now,
                IsModerated = true
            };
            _context.ReviewRestaurants.Add(review);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostUpdateRestaurantReview([FromBody] UpdateRestaurantReviewModel input)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var review = await _context.ReviewRestaurants.FindAsync(input.ReviewRestaurantId);
            if (review == null) return new JsonResult(new { success = false, message = "Отзыв не найден" });
            if (review.AppUserId != userId) return new JsonResult(new { success = false, message = "Нет прав" });

            review.Rating = input.Rating;
            review.Comment = input.Comment;
            review.IsModerated = false;
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteRestaurantReview(int id)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            var review = await _context.ReviewRestaurants.FindAsync(id);
            if (review == null) return new JsonResult(new { success = false, message = "Отзыв не найден" });
            if (review.AppUserId != userId) return new JsonResult(new { success = false, message = "Нет прав" });

            _context.ReviewRestaurants.Remove(review);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public class CreateDishReviewModel
        {
            public int DishId { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }

        public class UpdateDishReviewModel
        {
            public int ReviewDishId { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }

        public class CreateRestaurantReviewModel
        {
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }

        public class UpdateRestaurantReviewModel
        {
            public int ReviewRestaurantId { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }
    }
}