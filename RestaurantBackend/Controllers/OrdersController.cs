using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using RestaurantBackend.DTOs;
using RestaurantBackend.Services; 

namespace RestaurantBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Создает новый заказ для текущего аутентифицированного пользователя.
        /// </summary>
        /// <param name="orderDto">Данные для создания заказа.</param>
        /// <returns>Созданный OrderResponseDto.</returns>
        [HttpPost]
        [Authorize] 
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не удалось определить ID пользователя.");
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var order = await _orderService.CreateOrderAsync(userId, orderDto);
                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании заказа: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при создании заказа: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает детальную информацию о заказе по его ID.
        /// Пользователь может просматривать только свои заказы, админ - любые.
        /// </summary>
        /// <param name="id">ID заказа.</param>
        /// <returns>OrderResponseDto заказа.</returns>
        [HttpGet("{id}")]
        [Authorize] 
        public async Task<ActionResult<OrderResponseDto>> GetOrderById(Guid id)
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null)
                return NotFound($"Заказ с ID {id} не найден.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            Guid currentUserId = Guid.Empty;
            Guid.TryParse(userIdClaim?.Value, out currentUserId);

            // Проверка, является ли пользователь владельцем заказа или администратором
            if (order.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid("У вас нет прав для просмотра этого заказа.");
            }

            return Ok(order);
        }

        /// <summary>
        /// Получает все заказы текущего аутентифицированного пользователя.
        /// </summary>
        /// <returns>Список OrderResponseDto.</returns>
        [HttpGet("my-orders")]
        [Authorize] // Доступно аутентифицированным пользователям
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetMyOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не удалось определить ID пользователя.");
            }

            try
            {
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении заказов пользователя: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при получении ваших заказов: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает все заказы в системе. Доступно только администраторам.
        /// </summary>
        /// <returns>Список OrderResponseDto.</returns>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrdersForAdmin()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersForAdminAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении всех заказов (админ): {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при получении всех заказов: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет статус заказа. Доступно только администраторам.
        /// </summary>
        /// <param name="id">ID заказа.</param>
        /// <param name="newStatus">Новый статус заказа (например, "Pending", "Preparing", "Completed", "Cancelled").</param>
        /// <returns>Обновленный OrderResponseDto.</returns>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<OrderResponseDto>> UpdateOrderStatus(Guid id, [FromQuery] string newStatus)
        {
            
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                return BadRequest("Статус не может быть пустым.");
            }

            try
            {
                var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, newStatus);
                if (updatedOrder == null)
                {
                    return NotFound($"Заказ с ID {id} не найден.");
                }
                return Ok(updatedOrder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении статуса заказа: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при обновлении статуса заказа: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаляет заказ по его ID. Доступно только администраторам.
        /// </summary>
        /// <param name="id">ID заказа для удаления.</param>
        /// <returns>NoContent или NotFound.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> DeleteOrder(Guid id)
        {
            try
            {
                var isDeleted = await _orderService.DeleteOrderAsync(id);
                if (!isDeleted)
                {
                    return NotFound($"Заказ с ID {id} не найден.");
                }
                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении заказа: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при удалении заказа: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает все заказы со статусом "Pending". Доступно только администраторам.
        /// </summary>
        /// <returns>Список OrderResponseDto.</returns>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetPendingOrders()
        {
            try
            {
                var orders = await _orderService.GetPendingOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении pending заказов: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при получении pending заказов: {ex.Message}");
            }
        }
    }
}
