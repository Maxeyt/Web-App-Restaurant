using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Dish
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public IList<MaxFood.Models.Dish> Dishes { get; set; } = new List<MaxFood.Models.Dish>();

        // ✅ Словарь для рейтингов
        public Dictionary<int, decimal> Ratings { get; set; } = new Dictionary<int, decimal>();

        public async Task OnGetAsync()
        {
            // Загружаем блюда с категориями, подкатегориями и вариантами
            Dishes = await _context.Dishes
                .AsNoTracking()
                .Include(d => d.Category)
                .Include(d => d.Subcategory)
                .Include(d => d.DishVariants)
                .OrderBy(d => d.CategoryId)
                .ThenBy(d => d.DishName)
                .ToListAsync();

            // Загружаем рейтинги для всех блюд
            var dishIds = Dishes.Select(d => d.DishId).ToList();

            var ratingsQuery = await _context.ReviewDishes
                .Where(r => dishIds.Contains(r.DishId))
                .GroupBy(r => r.DishId)
                .Select(g => new { DishId = g.Key, AvgRating = g.Average(r => (decimal)r.Rating) })
                .ToDictionaryAsync(x => x.DishId, x => Math.Round(x.AvgRating, 1));

            Ratings = ratingsQuery;
        }
    }
}