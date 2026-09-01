using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Cart
{
    public class DetailsModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DetailsModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public Models.Cart? Cart { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "3" && userRole != "4")
            {
                return RedirectToPage("/Authorization/Login");
            }

            Cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.AppUser)
                    .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(c => c.CartId == id.Value);

            if (Cart == null) return NotFound();

            return Page();
        }
    }
}