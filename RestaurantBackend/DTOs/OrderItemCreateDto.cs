using System.ComponentModel.DataAnnotations;

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для элемента заказа при создании нового заказа.
    /// </summary>
    public class OrderItemCreateDto
    {
        [Required(ErrorMessage = "ID элемента меню обязателен")]
        public Guid MenuItemId { get; set; }

        [Required(ErrorMessage = "Количество обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1")]
        public int Quantity { get; set; }

        public string[] ExcludedIngredients { get; set; } = []; 
    }
}
