using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("Bonuses")]
    public class Bonus
    {
        [Key]
        public int BonusId { get; set; }

        [Required]
        public int AppUserId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal BonusAmount { get; set; } = 0;

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;

        public int? OrderId { get; set; }

        [StringLength(255)]
        public string? Source { get; set; }

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}