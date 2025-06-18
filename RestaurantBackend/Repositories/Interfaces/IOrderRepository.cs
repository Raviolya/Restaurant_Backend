using RestaurantBackend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantBackend.Repositories.Interfaces
{
    /// <summary>
    /// Интерфейс репозитория для работы с заказами.
    /// </summary>
    public interface IOrderRepository : IRepository<OrderModel>
    {
        /// <summary>
        /// Получает заказ по его ID, включая все элементы заказа и связанные с ними меню.
        /// </summary>
        /// <param name="orderId">ID заказа.</param>
        /// <returns>OrderModel или null.</returns>
        Task<OrderModel?> GetOrderByIdWithDetailsAsync(Guid orderId);

        /// <summary>
        /// Получает все заказы для конкретного пользователя, включая элементы заказа.
        /// </summary>
        /// <param name="userId">ID пользователя.</param>
        /// <returns>Коллекция OrderModel.</returns>
        Task<IEnumerable<OrderModel>> GetUserOrdersWithDetailsAsync(Guid userId);

        /// <summary>
        /// Получает все заказы с деталями для административного просмотра (включая пользователя и элементы меню).
        /// </summary>
        /// <returns>Коллекция OrderModel.</returns>
        Task<IEnumerable<OrderModel>> GetAllOrdersWithDetailsAsync();

        /// <summary>
        /// Получает заказы по статусу, включая элементы заказа.
        /// </summary>
        /// <param name="status">Статус заказа.</param>
        /// <returns>Коллекция OrderModel.</returns>
        Task<IEnumerable<OrderModel>> GetOrdersByStatusWithDetailsAsync(string status);
    }
}
