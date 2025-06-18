using Microsoft.EntityFrameworkCore;
using RestaurantBackend.Data;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantBackend.Repositories
{
    /// <summary>
    /// Репозиторий для получения данных, необходимых для отчетов.
    /// </summary>
    public class ReportRepository : IReportRepository
    {
        private readonly RestaurantDbContext _context;

        public ReportRepository(RestaurantDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получает все завершенные или находящиеся в процессе заказы за определенный период,
        /// включая элементы заказа и связанные с ними элементы меню и их категории.
        /// </summary>
        /// <param name="startDate">Начальная дата периода (UTC).</param>
        /// <param name="endDate">Конечная дата периода (UTC).</param>
        /// <returns>Коллекция OrderModel с деталями.</returns>
        public async Task<IEnumerable<OrderModel>> GetCompletedAndProcessingOrdersWithDetailsAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate.Kind == DateTimeKind.Local) startDate = startDate.ToUniversalTime();
            if (endDate.Kind == DateTimeKind.Local) endDate = endDate.ToUniversalTime();

            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi.Category) 
                .Where(o => (o.Status == "Completed" || o.Status == "Preparing" || o.Status == "Pending") && 
                            o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();
        }
    }
}
