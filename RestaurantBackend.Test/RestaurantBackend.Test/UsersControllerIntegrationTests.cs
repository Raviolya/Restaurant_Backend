using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using RestaurantBackend.DTOs;
using Xunit;

namespace RestaurantBackend.Tests
{
    public class UsersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public UsersControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetUsers_Unauthorized_Returns401()
        {
            var response = await _client.GetAsync("/api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RegisterAdmin_InvalidAge_ReturnsBadRequest()
        {
            // Логинимся как seeded-админ
            var loginDto = new LoginDto { Email = "testadmin@example.com", Password = "AdminPassword123!" };
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();
            var loginJson = await loginResponse.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(loginJson);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);

            // Пробуем зарегистрировать админа с некорректным возрастом
            var userDto = new CreateUserDto
            {
                FirstName = "Admin",
                LastName = "Test",
                Email = $"admin.{System.Guid.NewGuid()}@example.com",
                Phone = $"+123456789{Random.Shared.Next(1000, 9999)}",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                DateOfBirth = System.DateTime.UtcNow.AddYears(-5)
            };
            var content = new StringContent(JsonConvert.SerializeObject(userDto), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/users/register-admin", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
} 