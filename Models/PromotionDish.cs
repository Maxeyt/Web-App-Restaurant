using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("promotiondishes")]
    public class PromotionDish
    {
        [Key]
        public int PromotionDishId { get; set; }

        public int PromotionId { get; set; }

        public int DishVariantId { get; set; }  // ← изменено с DishId на DishVariantId

        [ForeignKey("PromotionId")]
        public virtual Promotion? Promotion { get; set; }

        [ForeignKey("DishVariantId")]
        public virtual DishVariant? DishVariant { get; set; }  // ← изменено с Dish на DishVariant
    }
}