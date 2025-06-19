using System;
using System.Threading.Tasks;
using Moq;
using RestaurantBackend.DTOs;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using RestaurantBackend.Services;
using Xunit;

namespace RestaurantBackend.Tests
{
    public class OrderServiceTests
    {
        [Fact]
        public async Task CreateOrderAsync_UserNotFound_ThrowsArgumentException()
        {
            var orderRepoMock = new Mock<IOrderRepository>();
            var menuRepoMock = new Mock<IMenuItemRepository>();
            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((UserModel)null);
            var service = new OrderService(orderRepoMock.Object, menuRepoMock.Object, userRepoMock.Object);
            var dto = new CreateOrderDto { Items = new System.Collections.Generic.List<OrderItemCreateDto>() };
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateOrderAsync(Guid.NewGuid(), dto));
        }
    }
} 