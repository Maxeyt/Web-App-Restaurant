using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("Supports")]
    public class Support
    {
        [Key]
        public int SupportId { get; set; }

        [Required]
        public int AppUserId { get; set; }

        public int? HelperId { get; set; }

        public int? AssignedHelperId { get; set; }

        public Guid? ChatGuid { get; set; }

        [Required]
        [StringLength(2000)]
        public string Text { get; set; } = string.Empty;

        public bool IsFromUser { get; set; } = true;

        public bool IsFavorite { get; set; } = false;

        public bool IsRead { get; set; } = false;

        public bool IsSupport { get; set; } = false;

        [StringLength(20)]
        public string ChatType { get; set; } = "personal";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        [ForeignKey("HelperId")]
        public virtual AppUser? Helper { get; set; }

        [ForeignKey("AssignedHelperId")]
        public virtual AppUser? AssignedHelper { get; set; }
    }
}