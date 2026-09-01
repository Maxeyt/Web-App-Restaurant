using System.ComponentModel.DataAnnotations;

namespace MaxFood.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [RegularExpression(@"^[а-яА-ЯёЁ\-]+$", ErrorMessage = "Имя должно содержать только русские буквы")]
        [StringLength(50, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [RegularExpression(@"^[а-яА-ЯёЁ\-]+$", ErrorMessage = "Фамилия должна содержать только русские буквы")]
        [StringLength(50, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Неверный формат номера телефона")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Неверный формат электронной почты")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтвердите пароль")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Role { get; set; } = "user";

        public bool AgreeTerms { get; set; }
    }
}