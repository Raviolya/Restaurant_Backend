using System;
using System.Collections.Generic;

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для полного отчета о продажах.
    /// </summary>
    public class SalesReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalItemsSold { get; set; }
        public List<SalesReportItemDto> Items { get; set; } = new List<SalesReportItemDto>();
        public DateTime GeneratedAt { get; set; } 
        public bool FromCache { get; set; } 
    }
}
