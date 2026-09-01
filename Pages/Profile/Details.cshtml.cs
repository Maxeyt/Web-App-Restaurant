using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Profile
{
    public class DetailsModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DetailsModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public MaxFood.Models.UserProfile Profile { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(m => m.ProfileId == id);
            if (profile == null) return NotFound();

            Profile = profile;
            return Page();
        }
    }
}