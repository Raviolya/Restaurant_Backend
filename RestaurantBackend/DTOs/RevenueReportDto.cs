using System;

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для отчета об общей выручке.
    /// </summary>
    public class RevenueReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool FromCache { get; set; }
    }
}
