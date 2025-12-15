using Farmer_MP.Models;

using Microsoft.EntityFrameworkCore;
namespace Farmer_MP.Data
{....
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>
options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Farmer> Farmers { get; set; }
        public DbSet<Buyer> Buyers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure inheritance using TPH (Table Per Hierarchy)
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<Admin>("Admin")
                .HasValue<Farmer>("Farmer")
                .HasValue<Buyer>("Buyer");
            modelBuilder.Entity<User>()
                .Property(u => u.OTP)
                .IsRequired(false);  // Make OTP optional

            modelBuilder.Entity<User>()
                .Property(u => u.OTPExpiry)
                .IsRequired(false);

            // Configure relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Farmer) // Farmer is actually a User
                .WithMany() // No navigation property in User for Products
                .HasForeignKey(p => p.UserID) // Link Product to UserID
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            base.OnModelCreating(modelBuilder);
            // Prevent cascading deletes

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductID)
                .OnDelete(DeleteBehavior.Cascade); // Allow cascading deletes

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Buyer)
                .WithMany()
                .HasForeignKey(r => r.BuyerID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

            // Configure Messages table relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderID)
                .OnDelete(DeleteBehavior.Restrict); // Disable cascading deletes for SenderID

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverID)
                .OnDelete(DeleteBehavior.Restrict); // Disable cascading deletes for ReceiverID

            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.User)
                .WithMany()
                .HasForeignKey(st => st.UserID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes
        }
    }
}
