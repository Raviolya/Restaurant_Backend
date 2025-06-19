using RestaurantBackend.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantBackend.Services
{
    /// <summary>
    /// Интерфейс сервиса для управления заказами.
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Создает новый заказ для пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя, создающего заказ.</param>
        /// <param name="orderDto">DTO с элементами заказа.</param>
        /// <returns>OrderResponseDto созданного заказа.</returns>
        Task<OrderResponseDto> CreateOrderAsync(Guid userId, CreateOrderDto orderDto);

        /// <summary>
        /// Получает детальную информацию о заказе по его ID.
        /// </summary>
        /// <param name="orderId">ID заказа.</param>
        /// <returns>OrderResponseDto заказа.</returns>
        Task<OrderResponseDto?> GetOrderDetailsAsync(Guid orderId);

        /// <summary>
        /// Получает все заказы для указанного пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <returns>Коллекция OrderResponseDto.</returns>
        Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(Guid userId);

        /// <summary>
        /// Получает все заказы для административного просмотра.
        /// </summary>
        /// <returns>Коллекция OrderResponseDto.</returns>
        Task<IEnumerable<OrderResponseDto>> GetAllOrdersForAdminAsync();

        /// <summary>
        /// Обновляет статус заказа.
        /// </summary>
        /// <param name="orderId">ID заказа.</param>
        /// <param name="newStatus">Новый статус заказа (например, "Pending", "Preparing", "Completed", "Cancelled").</param>
        /// <returns>Обновленный OrderResponseDto.</returns>
        Task<OrderResponseDto?> UpdateOrderStatusAsync(Guid orderId, string newStatus);

        /// <summary>
        /// Удаляет заказ по его ID.
        /// </summary>
        /// <param name="orderId">ID заказа.</param>
        /// <returns>True, если заказ успешно удален, иначе False.</returns>
        Task<bool> DeleteOrderAsync(Guid orderId);

        Task<IEnumerable<OrderResponseDto>> GetPendingOrdersAsync();

    }
}
