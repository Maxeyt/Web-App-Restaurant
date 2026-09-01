using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public IList<Models.Cart> Carts { get; set; } = new List<Models.Cart>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "3" && userRole != "4")
            {
                return RedirectToPage("/Authorization/Login");
            }

            Carts = await _context.Carts
                .Include(c => c.AppUser).ThenInclude(u => u.UserProfile)
                .Include(c => c.CartItems)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostImpersonateAsync(int userId)
        {
            var user = await _context.AppUsers
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.AppUserId == userId);

            if (user == null)
            {
                TempData["Message"] = "Пользователь не найден";
                return RedirectToPage("./Index");
            }

            HttpContext.Session.SetString("UserId", user.AppUserId.ToString());
            HttpContext.Session.SetString("UserRole", user.RoleId.ToString());
            HttpContext.Session.SetString("UserName", $"{user.UserProfile.FirstName} {user.UserProfile.LastName}");

            TempData["Message"] = $"Вы вошли как {user.UserProfile.FirstName} {user.UserProfile.LastName}";
            return RedirectToPage("/Menu/Index");
        }
    }
}