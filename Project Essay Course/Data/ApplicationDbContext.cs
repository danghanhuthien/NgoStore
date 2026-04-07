using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project_Essay_Course.Models;
using System.Reflection.Emit;

namespace Project_Essay_Course.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Lookbook> Lookbooks { get; set; }
        public DbSet<LookbookItem> LookbookItems { get; set; }
        // Thêm đoạn này vào ApplicationDbContext.cs
        // Override OnModelCreating để config relationships

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // QUAN TRỌNG: gọi base trước (Identity cần)

            // ── Category: self-referencing ───────────────────────────────
            builder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // không xóa parent nếu còn con

            // ── Product → Category ───────────────────────────────────────
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // không xóa category nếu còn product

            // ── ProductImage → Product (cascade) ─────────────────────────
            builder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // xóa product → xóa ảnh

            // ── ProductVariant → Product (cascade) ───────────────────────
            builder.Entity<ProductVariant>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // xóa product → xóa variants

            // ── Product: Slug unique index ────────────────────────────────
            builder.Entity<Product>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            // ── Category: Slug unique index ───────────────────────────────
            builder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            // ── Decimal precision ─────────────────────────────────────────
            builder.Entity<Product>()
                .Property(p => p.BasePrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<ProductVariant>()
                .Property(v => v.PriceOverride)
                .HasColumnType("decimal(18,2)");
            builder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(c => c.Variant)
                .WithMany()
                .HasForeignKey(c => c.VariantId)
                .OnDelete(DeleteBehavior.NoAction); // Không cascade từ variant

            // Index để query nhanh theo UserId
            builder.Entity<CartItem>()
                .HasIndex(c => c.UserId);

            // Unique: mỗi user chỉ có 1 dòng cho mỗi product+variant combo
            builder.Entity<CartItem>()
                .HasIndex(c => new { c.UserId, c.ProductId, c.VariantId })
                .IsUnique();

            builder.Entity<Order>()
    .HasIndex(o => o.OrderCode)
    .IsUnique();

            builder.Entity<OrderItem>()
                .HasOne(i => i.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal precision:
            builder.Entity<Order>().Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
            builder.Entity<Order>().Property(o => o.ShippingFee).HasColumnType("decimal(18,2)");
            builder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Entity<OrderItem>().Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Entity<Lookbook>()
                .HasIndex(l => l.Slug)
                .IsUnique();
        }

    }
}
