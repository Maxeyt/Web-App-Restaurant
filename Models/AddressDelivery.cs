using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("addressdeliveries")]
    public class AddressDelivery
    {
        [Key]
        public int AddressDeliveryId { get; set; }

        [Required]
        public int AppUserId { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; } = string.Empty;

        [StringLength(10)]
        public string? Entrance { get; set; }

        public int? Floor { get; set; }

        [StringLength(10)]
        public string? Apartment { get; set; }

        [StringLength(20)]
        public string? Domofon { get; set; }

        [StringLength(255)]
        public string? Comment { get; set; }

        public bool IsDefault { get; set; } = false;

        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        [NotMapped]
        public string FullAddress
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(City)) parts.Add(City);
                if (!string.IsNullOrEmpty(Address)) parts.Add(Address);

                var details = new List<string>();
                if (!string.IsNullOrEmpty(Apartment)) details.Add($"кв./офис {Apartment}");
                if (!string.IsNullOrEmpty(Entrance)) details.Add($"подъезд {Entrance}");
                if (Floor.HasValue && Floor > 0) details.Add($"этаж {Floor}");
                if (!string.IsNullOrEmpty(Domofon)) details.Add($"домофон {Domofon}");

                var address = string.Join(", ", parts);
                return address + (details.Any() ? ", " + string.Join(", ", details) : "");
            }
        }
    }
}