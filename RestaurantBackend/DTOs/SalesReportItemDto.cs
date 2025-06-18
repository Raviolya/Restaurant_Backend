using System;

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для элемента в отчете о продажах (отдельная позиция меню).
    /// </summary>
    public class SalesReportItemDto
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
