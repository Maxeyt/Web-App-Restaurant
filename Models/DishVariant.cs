using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("dishvariants")]
    public class DishVariant
    {
        [Key]
        public int DishVariantId { get; set; }

        public int DishId { get; set; }

        [StringLength(50)]
        public string SizeName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(8,2)")]
        public decimal Weight { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int? Calories { get; set; }

        public bool IsAvailable { get; set; } = true;

        [ForeignKey("DishId")]
        public virtual Dish? Dish { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<FavoriteDish> FavoriteDishes { get; set; } = new List<FavoriteDish>();
        public virtual ICollection<PromotionDish> PromotionDishes { get; set; } = new List<PromotionDish>();
    }
}