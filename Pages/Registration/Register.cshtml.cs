using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MaxFood.Data;
using MaxFood.Models;
using Microsoft.EntityFrameworkCore;

namespace MaxFood.Pages.Registration
{
    [IgnoreAntiforgeryToken]
    public class RegisterModel : PageModel
    {
        private readonly MaxFoodDBContext _context;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(MaxFoodDBContext context, ILogger<RegisterModel> logger)
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

        public async Task<IActionResult> OnPostRegisterAsync([FromBody] RegisterInputModel input)
        {
            _logger.LogInformation("=== НАЧАЛО РЕГИСТРАЦИИ ===");

            if (input == null)
            {
                return new JsonResult(new { success = false, message = "Ошибка: данные не получены" });
            }

            // Проверка согласия с условиями
            if (!input.AgreeTerms)
            {
                return new JsonResult(new { success = false, message = "Необходимо согласиться с условиями использования" });
            }

            // Проверка на существующего пользователя по телефону (email теперь необязателен)
            var existingUserByPhone = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.Phone == input.Phone);

            if (existingUserByPhone != null)
            {
                return new JsonResult(new { success = false, message = "Пользователь с таким телефоном уже существует" });
            }

            // Если email заполнен — проверяем уникальность
            if (!string.IsNullOrWhiteSpace(input.Email))
            {
                var existingUserByEmail = await _context.UserProfiles
                    .FirstOrDefaultAsync(u => u.Email == input.Email);
                if (existingUserByEmail != null)
                {
                    return new JsonResult(new { success = false, message = "Пользователь с таким email уже существует" });
                }
            }

            // Создание профиля пользователя
            var userProfile = new UserProfile
            {
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                Phone = input.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(input.Email) ? "" : input.Email.Trim().ToLower(),
                RegistrationDate = DateTime.Now,
                IsActive = true
            };

            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync();

            // Создание учетной записи с ролью 1 (пользователь)
            var appUser = new AppUser
            {
                ProfileId = userProfile.ProfileId,
                RoleId = 1,
                PasswordHash = AccountManager.HashPassword(input.Password),
                LastLoginDate = null,
                SelectedPickupPointId = null
            };

            _context.AppUsers.Add(appUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Зарегистрирован новый пользователь: {input.Phone}, ProfileId={userProfile.ProfileId}, RoleId=1");

            // Автоматический вход после регистрации
            HttpContext.Session.SetString("UserId", appUser.AppUserId.ToString());
            HttpContext.Session.SetInt32("UserRole", appUser.RoleId);
            HttpContext.Session.SetString("UserName", $"{userProfile.FirstName} {userProfile.LastName}");
            await HttpContext.Session.CommitAsync();

            return new JsonResult(new { success = true, redirectUrl = "/Index" });
        }

        public class RegisterInputModel
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
            public bool AgreeTerms { get; set; }
        }
    }
}