using RestaurantBackend.DTOs;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RestaurantBackend.Services
{
    /// <summary>
    /// Интерфейс сервиса для генерации отчетов.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Генерирует отчет о продажах за указанный период.
        /// </summary>
        /// <param name="startDate">Начальная дата периода (UTC).</param>
        /// <param name="endDate">Конечная дата периода (UTC).</param>
        /// <param name="forceRefresh">Если true, принудительно обновить кэш и пересчитать отчет.</param>
        /// <returns>Отчет о продажах SalesReportDto.</returns>
        Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate, bool forceRefresh = false);

        /// <summary>
        /// Генерирует отчет об общей выручке за указанный период.
        /// </summary>
        /// <param name="startDate">Начальная дата периода (UTC).</param>
        /// <param name="endDate">Конечная дата периода (UTC).</param>
        /// <param name="forceRefresh">Если true, принудительно обновить кэш и пересчитать отчет.</param>
        /// <returns>Отчет об общей выручке RevenueReportDto.</returns>
        Task<RevenueReportDto> GetRevenueReportAsync(DateTime startDate, DateTime endDate, bool forceRefresh = false);
    }
}
