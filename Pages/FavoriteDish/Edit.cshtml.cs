using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;

namespace MaxFood.Pages.FavoriteDish
{
    public class EditModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public EditModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.FavoriteDish FavoriteDish { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var favoritedish = await _context.FavoriteDishes.FirstOrDefaultAsync(m => m.FavoriteDishId == id);
            if (favoritedish == null)
            {
                return NotFound();
            }
            FavoriteDish = favoritedish;
            ViewData["AppUserId"] = new SelectList(_context.AppUsers, "AppUserId", "PasswordHash");
            ViewData["DishVariantId"] = new SelectList(_context.DishVariants, "DishVariantId", "SizeName");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(FavoriteDish).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FavoriteDishExists(FavoriteDish.FavoriteDishId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./MyFavoriteDish");
        }

        private bool FavoriteDishExists(int id)
        {
            return _context.FavoriteDishes.Any(e => e.FavoriteDishId == id);
        }
    }
}