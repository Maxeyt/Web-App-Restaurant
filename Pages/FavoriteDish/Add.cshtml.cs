using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.FavoriteDish
{
    public class AddModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public AddModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public class ToggleRequest
        {
            public int DishVariantId { get; set; }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostToggle([FromBody] ToggleRequest request)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return new JsonResult(new { success = false, message = "Необходимо авторизоваться" });

            if (!int.TryParse(userIdString, out int userId))
                return new JsonResult(new { success = false, message = "Ошибка идентификации" });

            if (request == null || request.DishVariantId <= 0)
                return new JsonResult(new { success = false, message = "Неверный запрос" });

            var variantExists = await _context.DishVariants
                .AnyAsync(v => v.DishVariantId == request.DishVariantId && v.IsAvailable);

            if (!variantExists)
                return new JsonResult(new { success = false, message = "Вариант блюда не найден или недоступен" });

            var existing = await _context.FavoriteDishes
                .FirstOrDefaultAsync(f => f.AppUserId == userId && f.DishVariantId == request.DishVariantId);

            bool isFavorite;

            if (existing != null)
            {
                _context.FavoriteDishes.Remove(existing);
                await _context.SaveChangesAsync();
                isFavorite = false;
            }
            else
            {
                var favorite = new MaxFood.Models.FavoriteDish
                {
                    AppUserId = userId,
                    DishVariantId = request.DishVariantId,
                    AddedDate = DateTime.Now
                };
                _context.FavoriteDishes.Add(favorite);
                await _context.SaveChangesAsync();
                isFavorite = true;
            }

            return new JsonResult(new { success = true, isFavorite });
        }

        // Новый метод для получения количества избранных
        public async Task<IActionResult> OnGetCount()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return new JsonResult(new { count = 0 });

            if (!int.TryParse(userIdString, out int userId))
                return new JsonResult(new { count = 0 });

            var count = await _context.FavoriteDishes
                .Where(f => f.AppUserId == userId)
                .CountAsync();

            return new JsonResult(new { count });
        }
    }
}