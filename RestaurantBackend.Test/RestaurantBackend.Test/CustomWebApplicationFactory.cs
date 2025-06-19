using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantBackend;
using RestaurantBackend.Data;
using RestaurantBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace RestaurantBackend.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTests");

            builder.ConfigureServices(services =>
            {
                // Удаляем все регистрации DbContext
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions) ||
                                d.ServiceType == typeof(DbContextOptions<RestaurantDbContext>) ||
                                d.ServiceType == typeof(RestaurantDbContext))
                    .ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Добавляем InMemory DbContext
                var dbName = $"TestDb_{Guid.NewGuid()}";
                services.AddDbContext<RestaurantDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                // Инициализация данных
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();

                // Асинхронно добавляем роли и пользователя-администратора
                Task.Run(async () =>
                {
                    await dbContext.RoleUsers.AddAsync(new RoleUserModel
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        Name = "Admin"
                    });
                    await dbContext.RoleUsers.AddAsync(new RoleUserModel
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        Name = "Customer"
                    });
                    await dbContext.SaveChangesAsync();

                    // Seed admin user
                    var adminUser = new UserModel
                    {
                        Id = Guid.NewGuid(),
                        Email = "testadmin@example.com",
                        Phone = "+10000000000",
                        Name = "Test Admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123!"),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001") // Admin
                    };
                    await dbContext.Users.AddAsync(adminUser);
                    await dbContext.SaveChangesAsync();

                    // Отладочный вывод
                    var roles = await dbContext.RoleUsers.ToListAsync();
                    Console.WriteLine($"Roles in DB: {string.Join(", ", roles.Select(r => $"{r.Name} (ID: {r.Id})"))}");
                }).GetAwaiter().GetResult();
            });

            // Добавляем тестовую конфигурацию
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var testConfiguration = new Dictionary<string, string?>
                {
                    { "Jwt:Key", "YourSecretKey1234567890abcdef12345678" }, // тот же ключ, что и в тестах и TokenService
                    { "Jwt:Issuer", "TestIssuer" },
                    { "Jwt:Audience", "TestAudience" },
                    { "Jwt:RefreshTokenExpirationDays", "7" },
                    { "Jwt:AccessTokenExpirationMinutes", "60" },
                    { "RedisConnection", "localhost:6379" },
                    { "Jwt:CookieDomain", "localhost" }
                };

                config.AddInMemoryCollection(testConfiguration);
            });
        }
    }
}