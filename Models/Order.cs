using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int AppUserId { get; set; }

        public int CartId { get; set; }

        public int? PointPickupId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; } = "Ожидает";

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime? PickupTime { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        // НОВЫЕ СВОЙСТВА (отсутствовали)
        public int? SelectedPickupPointId { get; set; }

        [StringLength(500)]
        public string? DeliveryAddress { get; set; }

        [StringLength(500)]
        public string? DeliveryComment { get; set; }

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        [ForeignKey("CartId")]
        public virtual Cart? Cart { get; set; }

        [ForeignKey("PointPickupId")]
        public virtual PointPickup? PointPickup { get; set; }
    }
}