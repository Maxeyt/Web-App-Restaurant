using System.ComponentModel.DataAnnotations;

namespace MaxFood.Models
{
    public class PointPickup
    {
        [Key]
        public int PointPickupId { get; set; }

        [Required]
        [StringLength(100)]
        public required string PointName { get; set; }

        [Required]
        [StringLength(255)]
        public required string Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? WorkHours { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Широта точки самовывоза (для Яндекс.Карт)
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Долгота точки самовывоза (для Яндекс.Карт)
        /// </summary>
        public decimal? Longitude { get; set; }
    }
}