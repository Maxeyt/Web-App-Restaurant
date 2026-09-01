using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.User
{
    public class EditModel : PageModel
    {
        private readonly MaxFood.Data.MaxFoodDBContext _context;

        public EditModel(MaxFood.Data.MaxFoodDBContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AppUser AppUser { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appuser = await _context.AppUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.AppUserId == id);

            if (appuser == null)
            {
                return NotFound();
            }

            // Проверка: нельзя редактировать администратора, если текущий пользователь не админ
            var currentUserRole = HttpContext.Session.GetInt32("UserRole");

            if (appuser.Role?.RoleName == "Admin" && currentUserRole != 3)
            {
                TempData["Error"] = "Недостаточно прав для редактирования администратора";
                return RedirectToPage("./Index");
            }

            AppUser = appuser;

            ViewData["ProfileId"] = new SelectList(_context.UserProfiles, "ProfileId", "Email", AppUser.ProfileId);
            ViewData["SelectedPickupPointId"] = new SelectList(_context.PointPickups, "PointPickupId", "Address", AppUser.SelectedPickupPointId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["ProfileId"] = new SelectList(_context.UserProfiles, "ProfileId", "Email", AppUser.ProfileId);
                ViewData["SelectedPickupPointId"] = new SelectList(_context.PointPickups, "PointPickupId", "Address", AppUser.SelectedPickupPointId);
                return Page();
            }

            // Получаем оригинального пользователя из БД
            var existingUser = await _context.AppUsers
                .FirstOrDefaultAsync(m => m.AppUserId == AppUser.AppUserId);

            if (existingUser == null)
            {
                return NotFound();
            }

            // Проверка: нельзя редактировать администратора, если текущий пользователь не админ
            var currentUserRole = HttpContext.Session.GetInt32("UserRole");
            var userRole = await _context.Roles
                .Where(r => r.RoleId == existingUser.RoleId)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync();

            if (userRole == "Admin" && currentUserRole != 3)
            {
                TempData["Error"] = "Недостаточно прав для редактирования администратора";
                return RedirectToPage("./Index");
            }

            // Обновляем поля (RoleId НЕ обновляем)
            existingUser.ProfileId = AppUser.ProfileId;
            existingUser.SelectedPickupPointId = AppUser.SelectedPickupPointId;
            existingUser.LastLoginDate = AppUser.LastLoginDate;

            // Обновляем пароль, только если он не пустой
            if (!string.IsNullOrWhiteSpace(AppUser.PasswordHash))
            {
                existingUser.PasswordHash = AppUser.PasswordHash;
            }

            _context.Attach(existingUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Пользователь успешно обновлён";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppUserExists(AppUser.AppUserId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool AppUserExists(int id)
        {
            return _context.AppUsers.Any(e => e.AppUserId == id);
        }
    }
}