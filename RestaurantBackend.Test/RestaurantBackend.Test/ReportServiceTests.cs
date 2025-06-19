using System;
using System.Threading.Tasks;
using Moq;
using RestaurantBackend.DTOs;
using RestaurantBackend.Repositories.Interfaces;
using RestaurantBackend.Services;
using Xunit;

namespace RestaurantBackend.Tests
{
    public class ReportServiceTests
    {
        [Fact]
        public async Task GetSalesReportAsync_FromCache_ReturnsCachedReport()
        {
            var reportRepoMock = new Mock<IReportRepository>();
            var cacheServiceMock = new Mock<ICacheService>();
            var cachedReport = new SalesReportDto { FromCache = true };
            cacheServiceMock.Setup(c => c.GetAsync<SalesReportDto>(It.IsAny<string>())).ReturnsAsync(cachedReport);
            var service = new ReportService(reportRepoMock.Object, cacheServiceMock.Object);
            var result = await service.GetSalesReportAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
            Assert.True(result.FromCache);
        }
    }
} 