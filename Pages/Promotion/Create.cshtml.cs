using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Promotion
{
    public class CreateModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public CreateModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.Promotion Promotion { get; set; } = new()
        {
            PromotionName = string.Empty,
            ValidFrom = DateTime.Now,
            ValidTo = DateTime.Now.AddDays(30),
            IsActive = true
        };

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Promotions.Add(Promotion);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}