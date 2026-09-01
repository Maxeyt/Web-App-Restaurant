using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Subcategory
{
    public class CreateModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public CreateModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.Subcategory Subcategory { get; set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["Categories"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryId", "CategoryName");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Subcategories.Add(Subcategory);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Menu/Index");
        }
    }
}