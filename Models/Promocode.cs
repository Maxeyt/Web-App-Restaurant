using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("Promocodes")]
    public class Promocode
    {
        [Key]
        public int PromocodeId { get; set; }
        [Required]
        [StringLength(50)]
        public required string PromoCode { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int? MaxUses { get; set; }
        public int CurrentUses { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}