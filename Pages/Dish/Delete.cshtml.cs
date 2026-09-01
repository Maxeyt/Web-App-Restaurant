using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.Dish
{
    public class DeleteModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public DeleteModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Models.Dish Dish { get; set; } = null!;

        public bool HasAccess { get; set; } = false;

        private bool IsAdminOrManager()
        {
            var roleId = HttpContext.Session.GetInt32("UserRole");
            // ИСПРАВЛЕНО: Admin(3) или Manager(2)
            return roleId == 4 || roleId == 3;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (!IsAdminOrManager())
            {
                HasAccess = false;
                return Page();
            }

            HasAccess = true;

            if (id == null) return NotFound();

            var dish = await _context.Dishes
                .Include(d => d.Category)
                .Include(d => d.DishVariants)
                .FirstOrDefaultAsync(m => m.DishId == id);

            if (dish == null) return NotFound();

            Dish = dish;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsAdminOrManager())
                return Unauthorized();

            var dish = await _context.Dishes.FindAsync(Dish.DishId);
            if (dish != null)
            {
                _context.Dishes.Remove(dish);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Dish/Index");
        }
    }
}