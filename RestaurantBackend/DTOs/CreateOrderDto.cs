using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace RestaurantBackend.DTOs
{
    /// <summary>
    /// DTO для создания нового заказа.
    /// </summary>
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Список элементов заказа не может быть пустым")]
        [MinLength(1, ErrorMessage = "Заказ должен содержать хотя бы один элемент")]
        public List<OrderItemCreateDto> Items { get; set; } = [];
    }
}
