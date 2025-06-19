using System; // Добавлено для Guid, DateTime

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для отображения информации об элементе меню.
    /// </summary>
    public class MenuItemResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; 
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty; 
        public string[] Ingredients { get; set; } = [];
        public bool IsAvailable { get; set; }
        public string? ImageUrl { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
