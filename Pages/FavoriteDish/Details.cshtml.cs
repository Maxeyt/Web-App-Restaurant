using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;

namespace MaxFood.Pages.FavoriteDish
{
    public class DetailsModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DetailsModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public MaxFood.Models.FavoriteDish FavoriteDish { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var favoritedish = await _context.FavoriteDishes
                .Include(f => f.AppUser)
                .Include(f => f.DishVariant)
                .FirstOrDefaultAsync(m => m.FavoriteDishId == id);

            if (favoritedish is not null)
            {
                FavoriteDish = favoritedish;
                return Page();
            }

            return NotFound();
        }
    }
}