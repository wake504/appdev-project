using Microsoft.EntityFrameworkCore;
using Appdev_Group_8.Models;

namespace Appdev_Group_8.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<Claim> Claims => Set<Claim>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // example: enforce unique category name
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.CategoryName)
                .IsUnique(false);

            // configure cascade delete behavior explicitly as preferred
            modelBuilder.Entity<Item>()
                .HasOne(i => i.ReportingUser)
                .WithMany(u => u.ReportedItems)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Item>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Claim>()
                .HasOne(cl => cl.Item)
                .WithMany(i => i.Claims)
                .HasForeignKey(cl => cl.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Claim>()
                .HasOne(cl => cl.ClaimingUser)
                .WithMany(u => u.Claims)
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}


