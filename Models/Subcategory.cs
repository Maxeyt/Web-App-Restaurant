using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("subcategories")]
    public class Subcategory
    {
        [Key]
        public int SubcategoryId { get; set; }

        public int CategoryId { get; set; }

        public string SubcategoryName { get; set; } = string.Empty;

        public int? DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}