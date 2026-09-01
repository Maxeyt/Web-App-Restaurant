using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("promotioncategories")]
    public class PromotionCategory
    {
        [Key]
        public int PromotionCategoryId { get; set; }

        public int PromotionId { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey("PromotionId")]
        public virtual Promotion? Promotion { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}