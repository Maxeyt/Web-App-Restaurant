using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("favoritecategories")]
    public class FavoriteCategory
    {
        [Key]
        public int FavoriteCategoryId { get; set; }

        public int AppUserId { get; set; }

        public int CategoryId { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}