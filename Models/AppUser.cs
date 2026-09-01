using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaxFood.Models
{
    [Table("appusers")]
    public class AppUser
    {
        [Key]
        public int AppUserId { get; set; }

        public int ProfileId { get; set; }

        public  int RoleId { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime? LastLoginDate { get; set; }

        public int? SelectedPickupPointId { get; set; }

        [ForeignKey("ProfileId")]
        public virtual UserProfile? UserProfile { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        [ForeignKey("SelectedPickupPointId")]
        public virtual PointPickup? SelectedPickupPoint { get; set; }

        public virtual ICollection<FavoriteDish> FavoriteDishes { get; set; } = new List<FavoriteDish>();
        public virtual ICollection<FavoriteCategory> FavoriteCategories { get; set; } = new List<FavoriteCategory>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<AddressDelivery> AddressDeliveries { get; set; } = new List<AddressDelivery>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public virtual ICollection<Bonus> Bonuses { get; set; } = new List<Bonus>();
    }
}