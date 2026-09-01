using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;
using System.Security.Claims;

namespace MaxFood.Pages
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context) => _context = context;

        public List<PopularDishDto> PopularDishes { get; set; } = new();
        public List<MaxFood.Models.Promotion> ActivePromotions { get; set; } = new();
        public List<MaxFood.Models.Category> AllCategories { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public class PopularDishDto
        {
            public int DishId { get; set; }
            public int CategoryId { get; set; }
            public string DishName { get; set; } = string.Empty;
            public string? ShortDescription { get; set; }
            public ICollection<DishVariant> DishVariants { get; set; } = new List<DishVariant>();
            public int SmallVariantId { get; set; }
            public decimal AverageRating { get; set; }
            public bool IsFavorite { get; set; }
        }

        public async Task OnGetAsync()
        {
            AllCategories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            if (string.IsNullOrEmpty(SearchTerm) && !SelectedCategoryId.HasValue)
            {
                var today = DateTime.Today;
                ActivePromotions = await _context.Promotions
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.ValidFrom <= today && p.ValidTo >= today)
                    .OrderBy(p => p.PromotionId)
                    .Take(5)
                    .ToListAsync();
            }

            IQueryable<MaxFood.Models.Dish> query = _context.Dishes
                .AsNoTracking()
                .Include(d => d.DishVariants)
                .Where(d => d.IsAvailable);

            if (!string.IsNullOrEmpty(SearchTerm))
                query = query.Where(d => d.DishName.Contains(SearchTerm));

            if (SelectedCategoryId.HasValue)
                query = query.Where(d => d.CategoryId == SelectedCategoryId.Value);

            if (string.IsNullOrEmpty(SearchTerm) && !SelectedCategoryId.HasValue)
            {
                var popularCategoryIds = new[] { 1, 2, 3, 5, 8 };
                var dishes = new List<PopularDishDto>();
                foreach (var catId in popularCategoryIds)
                {
                    var dish = await _context.Dishes
                        .AsNoTracking()
                        .Include(d => d.DishVariants)
                        .Where(d => d.IsAvailable && d.CategoryId == catId)
                        .OrderBy(d => d.DishId)
                        .Select(d => new PopularDishDto
                        {
                            DishId = d.DishId,
                            CategoryId = d.CategoryId,
                            DishName = d.DishName,
                            ShortDescription = d.ShortDescription,
                            DishVariants = d.DishVariants
                        })
                        .FirstOrDefaultAsync();
                    if (dish != null) dishes.Add(dish);
                }
                PopularDishes = dishes;
            }
            else
            {
                query = SortOrder switch
                {
                    "name_asc" => query.OrderBy(d => d.DishName),
                    "name_desc" => query.OrderByDescending(d => d.DishName),
                    "price_asc" => query.OrderBy(d => d.DishVariants.Min(v => v.Price)),
                    "price_desc" => query.OrderByDescending(d => d.DishVariants.Min(v => v.Price)),
                    _ => query.OrderBy(d => d.DishName)
                };

                PopularDishes = await query.Select(d => new PopularDishDto
                {
                    DishId = d.DishId,
                    CategoryId = d.CategoryId,
                    DishName = d.DishName,
                    ShortDescription = d.ShortDescription,
                    DishVariants = d.DishVariants
                }).ToListAsync();
            }

            // Получаем рейтинги
            var dishIds = PopularDishes.Select(d => d.DishId).ToList();
            var ratings = await _context.ReviewDishes
                .Where(r => dishIds.Contains(r.DishId))
                .GroupBy(r => r.DishId)
                .Select(g => new { DishId = g.Key, AvgRating = g.Average(r => (decimal)r.Rating) })
                .ToDictionaryAsync(x => x.DishId, x => Math.Round(x.AvgRating, 1));

            // ✅ Проверяем избранное пользователя
            HashSet<int> favoriteVariantIds = new HashSet<int>();
            var userIdString = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
            {
                favoriteVariantIds = await _context.FavoriteDishes
                    .Where(f => f.AppUserId == userId)
                    .Select(f => f.DishVariantId)
                    .ToHashSetAsync();
            }

            foreach (var dish in PopularDishes)
            {
                var smallVariant = dish.DishVariants.FirstOrDefault(v => v.SizeName == "Маленький");
                dish.SmallVariantId = smallVariant?.DishVariantId ?? 0;
                dish.AverageRating = ratings.TryGetValue(dish.DishId, out var avg) ? avg : 0m;

                // ✅ Устанавливаем статус избранного
                dish.IsFavorite = smallVariant != null && favoriteVariantIds.Contains(smallVariant.DishVariantId);
            }

            if (SortOrder == "rating_desc")
                PopularDishes = PopularDishes.OrderByDescending(d => d.AverageRating).ToList();
        }
    }
}