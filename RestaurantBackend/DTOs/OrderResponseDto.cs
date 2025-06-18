using System;
using System.Collections.Generic;

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для отображения полной информации о заказе.
    /// </summary>
    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty; // Имя пользователя для удобства
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<OrderItemResponseDto> OrderItems { get; set; } = [];
    }
}
