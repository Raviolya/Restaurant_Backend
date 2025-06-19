using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace RestaurantBackend.Tests
{
    public class MenuItemsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public MenuItemsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllMenuItems_ReturnsOk()
        {
            var response = await _client.GetAsync("/api/menuitems");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetMenuItemById_NotFound()
        {
            var response = await _client.GetAsync($"/api/menuitems/{System.Guid.NewGuid()}");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }
} 