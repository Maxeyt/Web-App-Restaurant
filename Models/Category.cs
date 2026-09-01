using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 50 символов")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Описание не может превышать 200 символов")]
        public string? Description { get; set; }

        public int? DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}