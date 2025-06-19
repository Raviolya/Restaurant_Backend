using System.ComponentModel.DataAnnotations;

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для создания нового элемента меню.
    /// </summary>
    public class CreateMenuItemDto
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 100 символов")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Описание не может превышать 500 символов")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Range(0.01, 100000.00, ErrorMessage = "Цена должна быть между 0.01 и 100000.00")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "ID категории обязателен")]
        public Guid CategoryId { get; set; }

        public string[] Ingredients { get; set; } = []; 

        public bool IsAvailable { get; set; } = true; 

        [StringLength(500, ErrorMessage = "URL картинки не может превышать 500 символов")]
        [Url(ErrorMessage = "Некорректный формат URL картинки")]
        public string? ImageUrl { get; set; } 
    }
}
