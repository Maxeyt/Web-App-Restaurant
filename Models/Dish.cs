using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("dishes")]
    public class Dish
    {
        [Key]
        public int DishId { get; set; }

        public int CategoryId { get; set; }

        public int? SubcategoryId { get; set; }

        [Required(ErrorMessage = "Название блюда обязательно")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Название должно быть от 1 до 50 символов")]
        public string DishName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Краткое описание не может превышать 200 символов")]
        public string? ShortDescription { get; set; }

        [StringLength(2000)]
        public string? FullDescription { get; set; }

        [StringLength(1000)]
        public string? Ingredients { get; set; }

        [StringLength(500)]
        public string? NutritionalValue { get; set; }

        public int? Calories { get; set; }

        public int? PreparationTime { get; set; }

        public string? ImagePath { get; set; }

        public bool IsAvailable { get; set; } = true;

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("SubcategoryId")]
        public virtual Subcategory? Subcategory { get; set; }

        public virtual ICollection<DishVariant> DishVariants { get; set; } = new List<DishVariant>();
    }
}