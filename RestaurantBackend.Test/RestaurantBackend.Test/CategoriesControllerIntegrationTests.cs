using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantBackend.Tests
{
    public class CategoriesControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public CategoriesControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCategories_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/categories");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }
} 