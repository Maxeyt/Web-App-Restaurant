using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("cartitems")]
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        public int CartId { get; set; }

        public int DishVariantId { get; set; }  // ← ссылка на вариант

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PriceAtAdd { get; set; }

        [StringLength(50)]
        public string SizeName { get; set; } = string.Empty;

        [StringLength(100)]
        public string DishName { get; set; } = string.Empty;

        [ForeignKey("CartId")]
        public virtual Cart? Cart { get; set; }

        [ForeignKey("DishVariantId")]
        public virtual DishVariant? DishVariant { get; set; }  // ← ссылка на вариант
    }
}