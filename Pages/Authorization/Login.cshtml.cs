using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MaxFood.Data;
using MaxFood.Models;
using Microsoft.EntityFrameworkCore;

namespace MaxFood.Pages.Authorization
{
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(MaxFoodDBContext context, ILogger<LoginModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToPage("/Index");
            return Page();
        }

        public async Task<IActionResult> OnPostLoginAsync([FromForm] LoginInputModel input)
        {
            _logger.LogInformation("=== НАЧАЛО АВТОРИЗАЦИИ ===");

            if (input == null)
            {
                _logger.LogError("input = null!");
                return new JsonResult(new { success = false, message = "Ошибка: данные не получены" });
            }

            _logger.LogInformation($"Login: '{input.Login}'");

            if (string.IsNullOrWhiteSpace(input.Login))
                return new JsonResult(new { success = false, message = "Введите логин" });

            if (string.IsNullOrWhiteSpace(input.Password))
                return new JsonResult(new { success = false, message = "Введите пароль" });

            // Поиск пользователя
            var appUser = await _context.AppUsers
                .Include(u => u.UserProfile)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.UserProfile.Email == input.Login || u.UserProfile.Phone == input.Login);

            if (appUser == null)
            {
                _logger.LogWarning($"Пользователь НЕ НАЙДЕН: {input.Login}");
                return new JsonResult(new { success = false, message = "Неверный логин или пароль" });
            }

            _logger.LogInformation($"Пользователь НАЙДЕН: {appUser.UserProfile.Email}, RoleId={appUser.RoleId}");

            // Проверка пароля
            string computedHash = AccountManager.HashPassword(input.Password);
            bool passwordMatch = computedHash == appUser.PasswordHash;

            if (!passwordMatch)
            {
                _logger.LogWarning($"НЕВЕРНЫЙ ПАРОЛЬ для {input.Login}");
                return new JsonResult(new { success = false, message = "Неверный логин или пароль" });
            }

            if (appUser.UserProfile?.IsActive == false)
            {
                _logger.LogWarning($"Аккаунт деактивирован");
                return new JsonResult(new { success = false, message = "Аккаунт деактивирован" });
            }

            // Сохранение в сессию
            HttpContext.Session.SetString("UserId", appUser.AppUserId.ToString());
            HttpContext.Session.SetInt32("UserRole", appUser.RoleId);
            HttpContext.Session.SetString("UserName", $"{appUser.UserProfile?.FirstName} {appUser.UserProfile?.LastName}");

            if (input.RememberMe)
            {
                HttpContext.Session.SetString("RememberMe", "true");
            }

            await HttpContext.Session.CommitAsync();

            // ✅ ВСЕХ пользователей перенаправляем на главную, независимо от роли
            string redirectUrl = "/Index";

            _logger.LogInformation($"✅ УСПЕХ! Пользователь {input.Login} вошёл в систему, RoleId={appUser.RoleId}, Redirect={redirectUrl}");
            return new JsonResult(new { success = true, redirectUrl = redirectUrl });
        }

        public class LoginInputModel
        {
            public string Login { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool RememberMe { get; set; }
        }
    }
}