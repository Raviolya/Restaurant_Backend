using Microsoft.EntityFrameworkCore;
using RestaurantBackend.Models;

namespace RestaurantBackend.Data;

public class RestaurantDbContext : DbContext
{
    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options) { }

    public DbSet<UserModel> Users { get; set; }
    public DbSet<MenuItemModel> MenuItems { get; set; }
    public DbSet<OrderModel> Orders { get; set; }
    public DbSet<OrderItemModel> OrderItems { get; set; }

    public DbSet<CategoryMenuItemModel> CategoryMenuItems { get; set; }

    public DbSet<RoleUserModel> RoleUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Настройка отношений
        modelBuilder.Entity<UserModel>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId);

        modelBuilder.Entity<MenuItemModel>()
            .HasOne(m => m.Category)
            .WithMany()
            .HasForeignKey(m => m.CategoryId);

        modelBuilder.Entity<OrderModel>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId);

        modelBuilder.Entity<OrderItemModel>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId);

        modelBuilder.Entity<OrderItemModel>()
            .HasOne(oi => oi.MenuItem)
            .WithMany()
            .HasForeignKey(oi => oi.MenuItemId);

  
        var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var customerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        modelBuilder.Entity<RoleUserModel>().HasData(
            new RoleUserModel { Id = adminRoleId, Name = "Admin" },
            new RoleUserModel { Id = customerRoleId, Name = "Customer" }
        );

        modelBuilder.Entity<CategoryMenuItemModel>().HasData(
            new CategoryMenuItemModel { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Основные блюда" },
            new CategoryMenuItemModel { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Закуски" },
            new CategoryMenuItemModel { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Напитки" },
            new CategoryMenuItemModel { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Десерты" }
        );

    }
}
