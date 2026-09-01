using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("faqs")]
    public class FAQ
    {
        [Key]
        public int FAQId { get; set; }

        [Required]
        [StringLength(500)]
        public required string Question { get; set; }

        [Required]
        [StringLength(1000)]
        public required string Answer { get; set; }

        public int? DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string? Category { get; set; }
    }
}