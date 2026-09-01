using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Profile
{
    public class EditModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public EditModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MaxFood.Models.UserProfile Profile { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.UserProfiles.FindAsync(id);
            if (profile == null) return NotFound();

            Profile = profile;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(Profile).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.UserProfiles.Any(e => e.ProfileId == Profile.ProfileId))
                    return NotFound();
                throw;
            }

            return RedirectToPage("./Index");
        }
    }
}