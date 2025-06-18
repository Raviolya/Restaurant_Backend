using RestaurantBackend.DTOs;
using RestaurantBackend.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantBackend.Services
{
    /// <summary>
    /// Сервис для генерации отчетов с использованием кэширования.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly ICacheService _cacheService;

        public ReportService(IReportRepository reportRepository, ICacheService cacheService)
        {
            _reportRepository = reportRepository;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Генерирует отчет о продажах за указанный период с кэшированием.
        /// </summary>
        public async Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate, bool forceRefresh = false)
        {
            var cacheKey = $"SalesReport:{startDate.ToString("yyyyMMdd")}-{endDate.ToString("yyyyMMdd")}";
            SalesReportDto? report = null;
            bool fromCache = false;

            if (!forceRefresh)
            {
                report = await _cacheService.GetAsync<SalesReportDto>(cacheKey);
                if (report != null)
                {
                    fromCache = true;
                    report.FromCache = true; 
                }
            }

            if (report == null)
            {
                var orders = await _reportRepository.GetCompletedAndProcessingOrdersWithDetailsAsync(startDate, endDate);

                var salesItems = orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => new { oi.MenuItemId, MenuItemName = oi.MenuItem.Name, CategoryName = oi.MenuItem.Category.Name }) // Группируем по товару и его категории
                    .Select(g => new SalesReportItemDto
                    {
                        MenuItemId = g.Key.MenuItemId,
                        MenuItemName = g.Key.MenuItemName, 
                        CategoryName = g.Key.CategoryName, 
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToList();

                report = new SalesReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRevenue = salesItems.Sum(item => item.TotalRevenue),
                    TotalItemsSold = salesItems.Sum(item => item.QuantitySold),
                    Items = salesItems,
                    GeneratedAt = DateTime.UtcNow,
                    FromCache = false// Этот отчет только что сгенерирован
                };

                await _cacheService.SetAsync(cacheKey, report); 
            }

            return report;
        }

        /// <summary>
        /// Генерирует отчет об общей выручке за указанный период с кэшированием.
        /// </summary>
        public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime startDate, DateTime endDate, bool forceRefresh = false)
        {
            var cacheKey = $"RevenueReport:{startDate.ToString("yyyyMMdd")}-{endDate.ToString("yyyyMMdd")}";
            RevenueReportDto? report = null;
            bool fromCache = false;

            if (!forceRefresh)
            {
                report = await _cacheService.GetAsync<RevenueReportDto>(cacheKey);
                if (report != null)
                {
                    fromCache = true;
                    report.FromCache = true;
                }
            }

            if (report == null)
            {
                var orders = await _reportRepository.GetCompletedAndProcessingOrdersWithDetailsAsync(startDate, endDate);
                var totalRevenue = orders.Sum(o => o.TotalPrice);

                report = new RevenueReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRevenue = totalRevenue,
                    GeneratedAt = DateTime.UtcNow,
                    FromCache = false
                };

                await _cacheService.SetAsync(cacheKey, report);
            }

            return report;
        }
    }
}
