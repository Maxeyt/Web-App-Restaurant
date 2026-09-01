using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaxFood.Data;
using MaxFood.Models;

namespace MaxFood.Pages.User
{
    public class IndexModel : PageModel
    {
        private readonly MaxFoodDBContext _context;

        public IndexModel(MaxFoodDBContext context)
        {
            _context = context;
        }

        public List<UserViewModel> Users { get; set; } = new();

        public class UserViewModel
        {
            public int AppUserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public int RoleId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public DateTime RegistrationDate { get; set; }
        }

        public class ChangeRoleRequest
        {
            public int UserId { get; set; }
            public int NewRoleId { get; set; }
        }

        public class DeleteUserRequest
        {
            public int UserId { get; set; }
        }

        private bool IsAdminOrManager()
        {
            var roleId = HttpContext.Session.GetInt32("UserRole");
            return roleId == 4 || roleId == 3 || roleId == 2;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAdminOrManager())
            {
                return RedirectToPage("/Authorization/Login");
            }

            Users = await _context.AppUsers
                .Include(u => u.UserProfile)
                .Include(u => u.Role)
                .Select(u => new UserViewModel
                {
                    AppUserId = u.AppUserId,
                    FullName = u.UserProfile.FirstName + " " + u.UserProfile.LastName,
                    Email = u.UserProfile.Email,
                    Phone = u.UserProfile.Phone,
                    RoleId = u.RoleId,
                    RoleName = u.Role.RoleName,
                    RegistrationDate = u.UserProfile.RegistrationDate
                })
                .OrderBy(u => u.RoleId)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostChangeUserRole([FromBody] ChangeRoleRequest request)
        {
            if (request == null)
                return new JsonResult(new { success = false, message = "Неверный запрос" });

            var currentUserRole = HttpContext.Session.GetInt32("UserRole");
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            if (currentUserRole != 4 && currentUserRole != 3 && currentUserRole != 2)
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            var user = await _context.AppUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.AppUserId == request.UserId);

            if (user == null)
                return new JsonResult(new { success = false, message = "Пользователь не найден" });

            if (user.Role?.RoleName == "Admin")
                return new JsonResult(new { success = false, message = "Нельзя изменить роль администратора" });

            if (currentUserId == request.UserId)
                return new JsonResult(new { success = false, message = "Нельзя изменить свою роль" });

            if (currentUserRole == 2)
            {
                if (request.NewRoleId == 3 || request.NewRoleId == 4)
                    return new JsonResult(new { success = false, message = "Менеджер не может назначить менеджера или администратора" });
                if (user.Role?.RoleName == "Manager")
                    return new JsonResult(new { success = false, message = "Менеджер не может изменять роль другого менеджера" });
            }

            if ((currentUserRole == 4 || currentUserRole == 3) && request.NewRoleId == 4)
                return new JsonResult(new { success = false, message = "Назначение администратора доступно только через базу данных" });

            if (request.NewRoleId < 1 || request.NewRoleId > 4)
                return new JsonResult(new { success = false, message = "Некорректная роль" });

            user.RoleId = request.NewRoleId;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Роль успешно изменена" });
        }

        public async Task<IActionResult> OnPostDeleteUser([FromBody] DeleteUserRequest request)
        {
            if (request == null)
                return new JsonResult(new { success = false, message = "Неверный запрос" });

            var currentUserRole = HttpContext.Session.GetInt32("UserRole");
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            if (currentUserRole != 4 && currentUserRole != 3 && currentUserRole != 2)
                return new JsonResult(new { success = false, message = "Недостаточно прав" });

            var user = await _context.AppUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.AppUserId == request.UserId);

            if (user == null)
                return new JsonResult(new { success = false, message = "Пользователь не найден" });

            if (user.Role?.RoleName == "Admin")
                return new JsonResult(new { success = false, message = "Невозможно удалить администратора" });

            if (currentUserRole == 2 && user.Role?.RoleName == "Manager")
                return new JsonResult(new { success = false, message = "Менеджер не может удалять другого менеджера" });

            if (currentUserId == request.UserId)
                return new JsonResult(new { success = false, message = "Нельзя удалить свой аккаунт" });

            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Пользователь удалён" });
        }
    }
}