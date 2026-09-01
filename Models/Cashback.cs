using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("Cashbacks")]
    public class Cashback
    {
        [Key]
        public int CashbackId { get; set; }

        [Required]
        public int AppUserId { get; set; }

        public int CategoryId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CashbackPercent { get; set; } = 0;  // ← БЫЛО Percent, СТАЛО CashbackPercent

        public bool IsActive { get; set; } = true;

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}