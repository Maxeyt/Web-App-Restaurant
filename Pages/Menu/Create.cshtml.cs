using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Menu
{
    public class CreateModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public CreateModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.Category Category { get; set; } = new()
        {
            CategoryName = string.Empty,
            IsActive = true
        };

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Categories.Add(Category);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}