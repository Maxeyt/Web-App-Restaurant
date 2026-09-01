using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Promotion
{
    public class EditModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public EditModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.Promotion Promotion { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions.FirstOrDefaultAsync(m => m.PromotionId == id);
            if (promotion == null) return NotFound();

            Promotion = promotion;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(Promotion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Promotions.Any(e => e.PromotionId == Promotion.PromotionId))
                    return NotFound();
                throw;
            }

            return RedirectToPage("./Index");
        }
    }
}