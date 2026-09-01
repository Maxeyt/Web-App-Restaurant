using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("UserProfiles")]
    public class UserProfile
    {
        [Key]
        public int ProfileId { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Имя должно быть от 1 до 50 символов")]
        [RegularExpression(@"^[а-яА-ЯёЁ\-]+$", ErrorMessage = "Имя должно содержать только русские буквы")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Фамилия должна быть от 1 до 50 символов")]
        [RegularExpression(@"^[а-яА-ЯёЁ\-]+$", ErrorMessage = "Фамилия должна содержать только русские буквы")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обязателен")]
        [StringLength(20, MinimumLength = 11, ErrorMessage = "Телефон должен быть от 11 до 20 символов")]
        [RegularExpression(@"^(\+7|8)[0-9]{10}$", ErrorMessage = "Введите номер в формате +71234567890 или 81234567890")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [StringLength(100, ErrorMessage = "Email не может превышать 100 символов")]
        [EmailAddress(ErrorMessage = "Введите корректный email")]
        public string Email { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}