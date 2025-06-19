using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RestaurantBackend.Data;
using RestaurantBackend.DTOs;
using Xunit;

namespace RestaurantBackend.Tests
{
    public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private readonly IServiceScope _scope;
        private readonly RestaurantDbContext _dbContext;

        public AuthIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        }

        public void Dispose()
        {
            _dbContext.Users.RemoveRange(_dbContext.Users);
            _dbContext.SaveChanges();
            _scope.Dispose();
        }

        [Fact]
        public async Task CheckRolesExist()
        {
            var customerRole = await _dbContext.RoleUsers.FirstOrDefaultAsync(r => r.Name == "Customer");
            Assert.NotNull(customerRole);
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), customerRole.Id);
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsCreated()
        {
            var userDto = new CreateUserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = $"john.doe.{Guid.NewGuid()}@example.com",
                Phone = $"+123456789{Random.Shared.Next(1000, 9999)}", // 12 цифр
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                DateOfBirth = DateTime.UtcNow.AddYears(-20)
            };

            var content = new StringContent(JsonConvert.SerializeObject(userDto), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/auth/register", content);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Assert.Fail($"Register failed with status {response.StatusCode}: {errorContent}");
            }

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            var responseDto = JsonConvert.DeserializeObject<UserResponseDto>(responseString);
            Assert.Equal(userDto.Email, responseDto?.Email);
            Assert.Equal("Customer", responseDto?.Role);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            var userDto = new CreateUserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = $"john.doe.{Guid.NewGuid()}@example.com",
                Phone = $"+123456789{Random.Shared.Next(1000, 9999)}", // 12 цифр
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                DateOfBirth = DateTime.UtcNow.AddYears(-20)
            };

            var registerContent = new StringContent(JsonConvert.SerializeObject(userDto), Encoding.UTF8, "application/json");
            var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);

            if (registerResponse.StatusCode != HttpStatusCode.Created)
            {
                var errorContent = await registerResponse.Content.ReadAsStringAsync();
                Assert.Fail($"Register in Login test failed with status {registerResponse.StatusCode}: {errorContent}");
            }

            Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

            var loginDto = new LoginDto
            {
                Email = userDto.Email,
                Password = "Password123!"
            };

            var loginContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/auth/login", loginContent);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Assert.Fail($"Login failed with status {response.StatusCode}: {errorContent}");
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseString);
            Assert.NotNull(authResponse?.Token);
            Assert.NotNull(authResponse?.RefreshToken);
        }
    }
}