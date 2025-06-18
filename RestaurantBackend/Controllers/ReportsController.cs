using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantBackend.DTOs;
using RestaurantBackend.Services;
using System;
using System.Threading.Tasks;

namespace RestaurantBackend.Controllers
{
    [Authorize(Roles = "Admin")] // Все отчеты доступны только администраторам
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Генерирует отчет о продажах за указанный период.
        /// </summary>
        /// <param name="startDate">Начальная дата периода (формат YYYY-MM-DD).</param>
        /// <param name="endDate">Конечная дата периода (формат YYYY-MM-DD).</param>
        /// <param name="forceRefresh">Принудительно обновить кэш и пересчитать отчет (по умолчанию false).</param>
        /// <returns>Отчет о продажах SalesReportDto.</returns>
        [HttpGet("sales")]
        public async Task<ActionResult<SalesReportDto>> GetSalesReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] bool forceRefresh = false)
        {
            // Убедитесь, что даты UTC для корректной работы с базой данных и кэшем
            var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var utcEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            if (utcStartDate > utcEndDate)
            {
                return BadRequest("Начальная дата не может быть позже конечной даты.");
            }

            try
            {
                var report = await _reportService.GetSalesReportAsync(utcStartDate, utcEndDate, forceRefresh);
                return Ok(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при генерации отчета о продажах: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при генерации отчета о продажах: {ex.Message}");
            }
        }

        /// <summary>
        /// Генерирует отчет об общей выручке за указанный период.
        /// </summary>
        /// <param name="startDate">Начальная дата периода (формат YYYY-MM-DD).</param>
        /// <param name="endDate">Конечная дата периода (формат YYYY-MM-DD).</param>
        /// <param name="forceRefresh">Принудительно обновить кэш и пересчитать отчет (по умолчанию false).</param>
        /// <returns>Отчет об общей выручке RevenueReportDto.</returns>
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueReportDto>> GetRevenueReport(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] bool forceRefresh = false)
        {
            var utcStartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            var utcEndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            if (utcStartDate > utcEndDate)
            {
                return BadRequest("Начальная дата не может быть позже конечной даты.");
            }

            try
            {
                var report = await _reportService.GetRevenueReportAsync(utcStartDate, utcEndDate, forceRefresh);
                return Ok(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при генерации отчета о выручке: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при генерации отчета о выручке: {ex.Message}");
            }
        }
    }
}
