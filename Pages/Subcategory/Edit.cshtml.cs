using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Subcategory
{
    public class EditModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public EditModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.Subcategory Subcategory { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var subcategory = await _context.Subcategories.FindAsync(id);
            if (subcategory == null) return NotFound();

            Subcategory = subcategory;
            ViewData["Categories"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName", subcategory.CategoryId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(Subcategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Subcategories.Any(e => e.SubcategoryId == Subcategory.SubcategoryId))
                    return NotFound();
                throw;
            }

            return RedirectToPage("/Menu/Index");
        }
    }
}