using System;

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для отображения информации об элементе заказа в ответе.
    /// </summary>
    public class OrderItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string[] ExcludedIngredients { get; set; } = [];
        public decimal Price { get; set; } 
    }
}
