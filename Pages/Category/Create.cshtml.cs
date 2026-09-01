using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Category
{
    public class CreateModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public CreateModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.Category Category { get; set; } = new();

        public IActionResult OnGet() => Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Categories.Add(Category);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Menu/Index");
        }
    }
}