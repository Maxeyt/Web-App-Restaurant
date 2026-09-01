using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Profile
{
    public class CreateModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public CreateModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.UserProfile Profile { get; set; } = new()
        {
            FirstName = string.Empty,
            LastName = string.Empty,
            Phone = string.Empty,
            Email = string.Empty,
            RegistrationDate = DateTime.Now,
            IsActive = true
        };

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.UserProfiles.Add(Profile);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}