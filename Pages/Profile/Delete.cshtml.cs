using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Profile
{
    public class DeleteModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DeleteModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.UserProfile Profile { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(m => m.ProfileId == id);
            if (profile == null) return NotFound();

            Profile = profile;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.UserProfiles.FindAsync(id);
            if (profile != null)
            {
                Profile = profile;
                _context.UserProfiles.Remove(Profile);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}