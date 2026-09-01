using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("promotions")]
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }

        [Required]
        [StringLength(100)]
        public required string PromotionName { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercent { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public bool IsActive { get; set; } = true;

        // НОВЫЕ ПОЛЯ
        [StringLength(20)]
        public string? IconColor { get; set; }

        [StringLength(10)]
        public string? IconEmoji { get; set; }

        [StringLength(500)]
        public string? IconImagePath { get; set; }

        [StringLength(20)]
        public string? DiscountType { get; set; } = "percent";

        [StringLength(50)]
        public string? PromocodeValue { get; set; }

        public virtual ICollection<PromotionDish> PromotionDishes { get; set; } = new List<PromotionDish>();
        public virtual ICollection<PromotionCategory> PromotionCategories { get; set; } = new List<PromotionCategory>();
    }
}