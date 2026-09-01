using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int AppUserId { get; set; }

        [Required(ErrorMessage = "Номер карты обязателен")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Номер карты должен содержать ровно 16 цифр")]
        [RegularExpression(@"^[0-9]{16}$", ErrorMessage = "Номер карты должен содержать только цифры")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя держателя карты обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя держателя от 2 до 100 символов")]
        [RegularExpression(@"^[A-Z\s]+$", ErrorMessage = "Только заглавные латинские буквы и пробелы")]
        public string CardHolder { get; set; } = string.Empty;

        [Required(ErrorMessage = "Срок действия обязателен")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "Формат: ММ/ГГ")]
        [RegularExpression(@"^(0[1-9]|1[0-2])/[0-9]{2}$", ErrorMessage = "Введите срок в формате ММ/ГГ (например 12/25)")]
        public string ExpiryDate { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }
    }
}