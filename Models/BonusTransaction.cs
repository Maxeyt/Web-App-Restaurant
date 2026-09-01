using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("BonusTransactions")]
    public class BonusTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int AppUserId { get; set; }

        public int? OrderId { get; set; }

        public int? PromocodeId { get; set; }

        public int? CategoryId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty;  // 'accrual', 'cashback', 'promo', 'spend'

        [StringLength(255)]
        public string? Source { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("PromocodeId")]
        public virtual Promocode? Promocode { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}