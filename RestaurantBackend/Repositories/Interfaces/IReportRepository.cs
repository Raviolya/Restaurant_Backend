using RestaurantBackend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantBackend.Repositories.Interfaces
{
    /// <summary>
    /// Интерфейс репозитория для получения данных, необходимых для отчетов.
    /// </summary>
    public interface IReportRepository
    {
        /// <summary>
        /// Получает все завершенные или находящиеся в процессе заказы за определенный период,
        /// включая элементы заказа и связанные с ними элементы меню и их категории.
        /// </summary>
        /// <param name="startDate">Начальная дата периода (UTC).</param>
        /// <param name="endDate">Конечная дата периода (UTC).</param>
        /// <returns>Коллекция OrderModel с деталями.</returns>
        Task<IEnumerable<OrderModel>> GetCompletedAndProcessingOrdersWithDetailsAsync(DateTime startDate, DateTime endDate);
    }
}
