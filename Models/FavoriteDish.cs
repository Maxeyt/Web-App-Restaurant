// Models/FavoriteDish.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("FavoriteDishes")]
    public class FavoriteDish
    {
        [Key]
        public int FavoriteDishId { get; set; }

        public int AppUserId { get; set; }

        public int DishVariantId { get; set; }   // ← ключевое изменение: ссылка на вариант

        public DateTime AddedDate { get; set; } = DateTime.Now;

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        [ForeignKey("DishVariantId")]
        public virtual DishVariant? DishVariant { get; set; }
    }
}