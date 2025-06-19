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
    public class OrdersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public OrdersControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetMyOrders_Unauthorized_Returns401()
        {
            var response = await _client.GetAsync("/api/orders/my-orders");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_InvalidModel_ReturnsBadRequest()
        {
            // Сначала регистрируем пользователя
            var userDto = new CreateUserDto
            {
                FirstName = "Order",
                LastName = "User",
                Email = $"orderuser.{System.Guid.NewGuid()}@example.com",
                Phone = $"+123456789{Random.Shared.Next(1000, 9999)}",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                DateOfBirth = System.DateTime.UtcNow.AddYears(-20)
            };
            var registerContent = new StringContent(JsonConvert.SerializeObject(userDto), Encoding.UTF8, "application/json");
            var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            // Логинимся
            var loginDto = new LoginDto { Email = userDto.Email, Password = userDto.Password };
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginDto), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();
            var loginJson = await loginResponse.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(loginJson);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);

            // Пробуем создать заказ с невалидной моделью
            var orderDto = new CreateOrderDto { Items = null };
            var content = new StringContent(JsonConvert.SerializeObject(orderDto), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/orders", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
} 