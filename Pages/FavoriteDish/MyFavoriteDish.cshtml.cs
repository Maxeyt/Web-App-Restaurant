using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.FavoriteDish
{
    public class MyFavoriteDishModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public MyFavoriteDishModel(MaxFoodDBContext context) => _context = context;

        public bool IsAuthenticated { get; set; }
        public List<CategoryGroup> Categories { get; set; } = new();
        public List<MaxFood.Models.Subcategory> Subcategories { get; set; } = new();

        public class CategoryGroup
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public List<SubcategoryGroup> SubcategoryGroups { get; set; } = new();
        }

        public class SubcategoryGroup
        {
            public int? SubcategoryId { get; set; }
            public string SubcategoryName { get; set; } = "Без подкатегории";
            public List<FavoriteDishItem> Dishes { get; set; } = new();
        }

        public class FavoriteDishItem
        {
            public int FavoriteDishId { get; set; }
            public int DishVariantId { get; set; }
            public int DishId { get; set; }
            public string DishName { get; set; } = string.Empty;
            public string? ShortDescription { get; set; }
            public decimal Price { get; set; }
            public decimal Weight { get; set; }
            public decimal AverageRating { get; set; }
            public bool IsFavorite { get; set; } = true;
        }

        public async Task OnGetAsync()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            IsAuthenticated = !string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out _);

            if (!IsAuthenticated) return;

            int userId = int.Parse(userIdString!);

            Subcategories = await _context.Subcategories
                .AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync();

            var favorites = await _context.FavoriteDishes
                .Where(f => f.AppUserId == userId)
                .Include(f => f.DishVariant)
                    .ThenInclude(v => v.Dish)
                        .ThenInclude(d => d.Category)
                .Include(f => f.DishVariant)
                    .ThenInclude(v => v.Dish)
                        .ThenInclude(d => d.Subcategory)
                .ToListAsync();

            var dishIds = favorites
                .Select(f => f.DishVariant?.DishId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            var ratings = await _context.ReviewDishes
                .Where(r => dishIds.Contains(r.DishId))
                .GroupBy(r => r.DishId)
                .Select(g => new { DishId = g.Key, AvgRating = g.Average(r => (decimal)r.Rating) })
                .ToDictionaryAsync(x => x.DishId, x => Math.Round(x.AvgRating, 1));

            var groupedByCategory = favorites
                .Where(f => f.DishVariant?.Dish != null)
                .GroupBy(f => new { f.DishVariant!.Dish!.CategoryId, CategoryName = f.DishVariant.Dish.Category?.CategoryName ?? "Без категории" })
                .OrderBy(g => g.Key.CategoryId);

            foreach (var categoryGroup in groupedByCategory)
            {
                var catGroup = new CategoryGroup
                {
                    CategoryId = categoryGroup.Key.CategoryId,
                    CategoryName = categoryGroup.Key.CategoryName
                };

                var groupedBySubcategory = categoryGroup
                    .GroupBy(f => f.DishVariant?.Dish?.SubcategoryId)
                    .OrderBy(g => g.Key);

                foreach (var subcategoryGroup in groupedBySubcategory)
                {
                    var subcatName = subcategoryGroup.Key != null
                        ? Subcategories.FirstOrDefault(s => s.SubcategoryId == subcategoryGroup.Key)?.SubcategoryName ?? "Без подкатегории"
                        : "Без подкатегории";

                    var subGroup = new SubcategoryGroup
                    {
                        SubcategoryId = subcategoryGroup.Key,
                        SubcategoryName = subcatName,
                        Dishes = subcategoryGroup.Select(f => new FavoriteDishItem
                        {
                            FavoriteDishId = f.FavoriteDishId,
                            DishVariantId = f.DishVariantId,
                            DishId = f.DishVariant?.Dish?.DishId ?? 0,
                            DishName = f.DishVariant?.Dish?.DishName ?? "Блюдо",
                            ShortDescription = f.DishVariant?.Dish?.ShortDescription,
                            Price = f.DishVariant?.Price ?? 0,
                            Weight = f.DishVariant?.Weight ?? 0,
                            AverageRating = f.DishVariant?.Dish != null && ratings.TryGetValue(f.DishVariant.Dish.DishId, out var avg) ? avg : 0m,
                            IsFavorite = true
                        }).ToList()
                    };

                    catGroup.SubcategoryGroups.Add(subGroup);
                }

                Categories.Add(catGroup);
            }
        }
    }
}