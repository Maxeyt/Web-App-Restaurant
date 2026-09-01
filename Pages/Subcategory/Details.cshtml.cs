using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Subcategory
{
    public class DetailsModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        public DetailsModel(MaxFoodDBContext context) => _context = context;

        [BindProperty]
        public MaxFood.Models.Subcategory Subcategory { get; set; } = null!;  // Исправлено: Subcategory (единственное число) и null!

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Subcategory = await _context.Subcategories.FirstOrDefaultAsync(m => m.SubcategoryId == id);  // Исправлено: используем SubcategoryId
            if (Subcategory == null) return NotFound();

            return Page();
        }
    }
}