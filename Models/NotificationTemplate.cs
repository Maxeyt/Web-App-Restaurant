using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("NotificationTemplates")]
    public class NotificationTemplate
    {
        [Key]
        public int TemplateId { get; set; }

        [Required]
        [StringLength(50)]
        public string TemplateType { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DefaultTitle { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string DefaultShortMessage { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string DefaultLongMessage { get; set; } = string.Empty;

        [StringLength(50)]
        public string? IconClass { get; set; }

        [StringLength(20)]
        public string? BadgeColor { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;
    }
}