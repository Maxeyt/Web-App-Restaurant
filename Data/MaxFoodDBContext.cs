using Microsoft.EntityFrameworkCore;
using MaxFood.Models;

namespace MaxFood.Data
{
    public class MaxFoodDBContext : DbContext
    {
        public MaxFoodDBContext(DbContextOptions<MaxFoodDBContext> options) : base(options) { }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AddressDelivery> AddressDeliveries { get; set; }
        public DbSet<PointPickup> PointPickups { get; set; }
        public DbSet<Bonus> Bonuses { get; set; }
        public DbSet<BonusTransaction> BonusTransactions { get; set; }
        public DbSet<Promocode> Promocodes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<Support> Supports { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Cashback> Cashbacks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<DishVariant> DishVariants { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }  // ← ДОБАВЛЕНО
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionDish> PromotionDishes { get; set; }
        public DbSet<PromotionCategory> PromotionCategories { get; set; }
        public DbSet<FavoriteDish> FavoriteDishes { get; set; }
        public DbSet<FavoriteCategory> FavoriteCategories { get; set; }
        public DbSet<ReviewDish> ReviewDishes { get; set; }
        public DbSet<ReviewRestaurant> ReviewRestaurants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== Roles ==========
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.RoleName).IsUnique();
            });

            // ========== UserProfiles ==========
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("UserProfiles");
                entity.HasKey(e => e.ProfileId);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // ========== AppUsers ==========
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.ToTable("AppUsers");
                entity.HasKey(e => e.AppUserId);
                entity.HasOne(e => e.UserProfile)
                    .WithOne()
                    .HasForeignKey<AppUser>(e => e.ProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Role)
                    .WithMany()
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.SelectedPickupPoint)
                    .WithMany()
                    .HasForeignKey(e => e.SelectedPickupPointId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ========== OrderItems ==========
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");
                entity.HasKey(e => e.OrderItemId);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Dish)
                    .WithMany()
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== Dishes ==========
            modelBuilder.Entity<Dish>(entity =>
            {
                entity.ToTable("Dishes");
                entity.HasKey(e => e.DishId);
                entity.Property(e => e.DishName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.IsAvailable).HasDefaultValue(true);
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Subcategory)
                    .WithMany(e => e.Dishes)
                    .HasForeignKey(e => e.SubcategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ========== DishVariants ==========
            modelBuilder.Entity<DishVariant>(entity =>
            {
                entity.ToTable("DishVariants");
                entity.HasKey(e => e.DishVariantId);
                entity.Property(e => e.SizeName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Weight).HasColumnType("decimal(8,2)");
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
                entity.Property(e => e.IsAvailable).HasDefaultValue(true);
                entity.HasOne(e => e.Dish)
                    .WithMany(e => e.DishVariants)
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== Carts ==========
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("Carts");
                entity.HasKey(e => e.CartId);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.AppUser)
                    .WithMany(e => e.Carts)
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== CartItems ==========
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasKey(e => e.CartItemId);
                entity.Property(e => e.DishName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SizeName).HasMaxLength(50);
                entity.Property(e => e.PriceAtAdd).HasColumnType("decimal(10,2)");
                entity.HasOne(e => e.Cart)
                    .WithMany(e => e.CartItems)
                    .HasForeignKey(e => e.CartId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.DishVariant)
                    .WithMany(e => e.CartItems)
                    .HasForeignKey(e => e.DishVariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== Orders ==========
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DeliveryAddress).HasMaxLength(500);
                entity.Property(e => e.DeliveryComment).HasMaxLength(500);
                entity.HasOne(e => e.AppUser)
                    .WithMany(e => e.Orders)
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Cart)
                    .WithMany()
                    .HasForeignKey(e => e.CartId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.PointPickup)
                    .WithMany()
                    .HasForeignKey(e => e.PointPickupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== AddressDeliveries ==========
            modelBuilder.Entity<AddressDelivery>(entity =>
            {
                entity.ToTable("AddressDeliveries");
                entity.HasKey(e => e.AddressDeliveryId);
                entity.HasOne(e => e.AppUser)
                    .WithMany(e => e.AddressDeliveries)
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== Bonuses ==========
            modelBuilder.Entity<Bonus>(entity =>
            {
                entity.ToTable("Bonuses");
                entity.HasKey(e => e.BonusId);
                entity.Property(e => e.BonusAmount).HasColumnType("decimal(10,2)");
                entity.HasOne(e => e.AppUser)
                    .WithMany(e => e.Bonuses)
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ========== BonusTransactions ==========
            modelBuilder.Entity<BonusTransaction>(entity =>
            {
                entity.ToTable("BonusTransactions");
                entity.HasKey(e => e.TransactionId);
                entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
                entity.HasOne(e => e.AppUser).WithMany().HasForeignKey(e => e.AppUserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Promocode).WithMany().HasForeignKey(e => e.PromocodeId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
            });

            // ========== Cashbacks ==========
            modelBuilder.Entity<Cashback>(entity =>
            {
                entity.ToTable("Cashbacks");
                entity.HasKey(e => e.CashbackId);
                entity.Property(e => e.CashbackPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.HasOne(e => e.AppUser)
                    .WithMany()
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== Notifications ==========
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.NotificationId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
                entity.Property(e => e.LongMessage).HasMaxLength(2000);
                entity.Property(e => e.IsRead).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.AppUser)
                    .WithMany(e => e.Notifications)
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== NotificationTemplates ==========
            modelBuilder.Entity<NotificationTemplate>(entity =>
            {
                entity.ToTable("NotificationTemplates");
                entity.HasKey(e => e.TemplateId);
                entity.Property(e => e.TemplateType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DefaultTitle).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DefaultShortMessage).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DefaultLongMessage).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.IconClass).HasMaxLength(50);
                entity.Property(e => e.BadgeColor).HasMaxLength(20);
                entity.HasIndex(e => e.TemplateType).IsUnique();
            });

            // ========== FAQs ==========
            modelBuilder.Entity<FAQ>(entity =>
            {
                entity.ToTable("FAQs");
                entity.HasKey(e => e.FAQId);
            });

            // ========== Promocodes ==========
            modelBuilder.Entity<Promocode>(entity =>
            {
                entity.ToTable("Promocodes");
                entity.HasKey(e => e.PromocodeId);
                entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5,2)");
                entity.HasIndex(e => e.PromoCode).IsUnique();
            });

            // ========== PointPickups ==========
            modelBuilder.Entity<PointPickup>(entity =>
            {
                entity.ToTable("PointPickups");
                entity.HasKey(e => e.PointPickupId);
            });

            // ========== Supports ==========
            modelBuilder.Entity<Support>(entity =>
            {
                entity.ToTable("Supports");
                entity.HasKey(e => e.SupportId);
                entity.HasOne(e => e.AppUser)
                    .WithMany()
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== Payments ==========
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(e => e.PaymentId);
                entity.HasOne(e => e.AppUser)
                    .WithMany()
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== Promotions ==========
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("Promotions");
                entity.HasKey(e => e.PromotionId);
                entity.Property(e => e.PromotionName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5,2)");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // ========== PromotionDishes ==========
            modelBuilder.Entity<PromotionDish>(entity =>
            {
                entity.ToTable("PromotionDishes");
                entity.HasKey(e => e.PromotionDishId);
                entity.HasOne(e => e.Promotion)
                    .WithMany(e => e.PromotionDishes)
                    .HasForeignKey(e => e.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.DishVariant)
                    .WithMany(e => e.PromotionDishes)
                    .HasForeignKey(e => e.DishVariantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== PromotionCategories ==========
            modelBuilder.Entity<PromotionCategory>(entity =>
            {
                entity.ToTable("PromotionCategories");
                entity.HasKey(e => e.PromotionCategoryId);
                entity.HasOne(e => e.Promotion)
                    .WithMany(e => e.PromotionCategories)
                    .HasForeignKey(e => e.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== FavoriteDishes ==========
            modelBuilder.Entity<FavoriteDish>(entity =>
            {
                entity.ToTable("FavoriteDishes");
                entity.HasKey(e => e.FavoriteDishId);
                entity.HasIndex(e => new { e.AppUserId, e.DishVariantId }).IsUnique();
                entity.HasOne(e => e.AppUser)
                    .WithMany(e => e.FavoriteDishes)
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.DishVariant)
                    .WithMany(e => e.FavoriteDishes)
                    .HasForeignKey(e => e.DishVariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== FavoriteCategories ==========
            modelBuilder.Entity<FavoriteCategory>(entity =>
            {
                entity.ToTable("FavoriteCategories");
                entity.HasKey(e => e.FavoriteCategoryId);
                entity.HasIndex(e => new { e.AppUserId, e.CategoryId }).IsUnique();
                entity.HasOne(e => e.AppUser)
                    .WithMany(e => e.FavoriteCategories)
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== ReviewDishes ==========
            modelBuilder.Entity<ReviewDish>(entity =>
            {
                entity.ToTable("ReviewDishes");
                entity.HasKey(e => e.ReviewDishId);
                entity.HasOne(e => e.AppUser)
                    .WithMany()
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Dish)
                    .WithMany()
                    .HasForeignKey(e => e.DishId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== ReviewRestaurants ==========
            modelBuilder.Entity<ReviewRestaurant>(entity =>
            {
                entity.ToTable("ReviewRestaurants");
                entity.HasKey(e => e.ReviewRestaurantId);
                entity.HasOne(e => e.AppUser)
                    .WithMany()
                    .HasForeignKey(e => e.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}