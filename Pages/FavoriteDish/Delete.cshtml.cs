using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;

namespace MaxFood.Pages.FavoriteDish
{
    public class DeleteModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DeleteModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.FavoriteDish FavoriteDish { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

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

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var favoritedish = await _context.FavoriteDishes.FindAsync(id);
            if (favoritedish != null)
            {
                _context.FavoriteDishes.Remove(favoritedish);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./MyFavoriteDish");
        }
    }
}