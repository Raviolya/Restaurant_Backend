using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestaurantBackend.DTOs;
using Xunit;

namespace RestaurantBackend.Tests
{
    public class ReportsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ReportsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetSalesReport_InvalidDateRange_ReturnsBadRequest()
        {
            // Логинимся как seeded-админ
            var loginDto = new LoginDto { Email = "admin@example.com", Password = "AdminPassword123!" };
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();
            var loginJson = await loginResponse.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(loginJson);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);

            // Пробуем получить отчёт с некорректным диапазоном дат
            var url = $"/api/reports/sales?startDate=2025-12-31&endDate=2025-01-01";
            var response = await _client.GetAsync(url);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
} 