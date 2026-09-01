using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Cart
{
    public class EditModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public EditModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.Cart Cart { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "3" && userRole != "4")
            {
                return RedirectToPage("/Authorization/Login");
            }

            Cart = await _context.Carts
                .Include(c => c.AppUser).ThenInclude(u => u.UserProfile)
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(m => m.CartId == id);

            if (Cart == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "3" && userRole != "4")
            {
                return RedirectToPage("/Authorization/Login");
            }

            if (!ModelState.IsValid) return Page();

            _context.Attach(Cart).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Carts.Any(e => e.CartId == Cart.CartId))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToPage("./Details", new { id = Cart.CartId });
        }
    }
}