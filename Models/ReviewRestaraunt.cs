using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("ReviewRestaurants")]
    public class ReviewRestaurant
    {
        [Key]
        public int ReviewRestaurantId { get; set; }

        [Required]
        public int AppUserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.Now;

        public bool IsModerated { get; set; } = false;

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }
    }
}