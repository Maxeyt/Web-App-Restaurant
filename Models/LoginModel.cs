using System.ComponentModel.DataAnnotations;

namespace MaxFood.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Введите телефон или email")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public string Role { get; set; } = "user";

        public string? ReturnUrl { get; set; }
    }
}