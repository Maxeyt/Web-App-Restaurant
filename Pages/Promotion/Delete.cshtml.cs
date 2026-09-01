using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Promotion
{
    public class DeleteModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DeleteModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public  MaxFood.Models.Promotion Promotion { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions.FirstOrDefaultAsync(m => m.PromotionId == id);
            if (promotion == null) return NotFound();

            Promotion = promotion;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                Promotion = promotion;
                _context.Promotions.Remove(Promotion);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}