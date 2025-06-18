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
    /// Реализация репозитория для работы с заказами.
    /// </summary>
    public class OrderRepository : Repository<OrderModel>, IOrderRepository
    {
        public OrderRepository(RestaurantDbContext context) : base(context) { }

        /// <summary>
        /// Получает заказ по его ID, включая все элементы заказа, связанные с ними меню и пользователя.
        /// </summary>
        public async Task<OrderModel?> GetOrderByIdWithDetailsAsync(Guid orderId)
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi.Category)
                .Include(o => o.User) 
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        /// <summary>
        /// Получает все заказы для конкретного пользователя, включая элементы заказа и меню.
        /// </summary>
        public async Task<IEnumerable<OrderModel>> GetUserOrdersWithDetailsAsync(Guid userId)
        {
            return await _dbSet
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi.Category) 
                .Include(o => o.User) 
                .ToListAsync();
        }

        /// <summary>
        /// Получает все заказы с деталями для административного просмотра.
        /// </summary>
        public async Task<IEnumerable<OrderModel>> GetAllOrdersWithDetailsAsync()
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi.Category)
                .Include(o => o.User) 
                .ToListAsync();
        }

        /// <summary>
        /// Получает заказы по статусу, включая элементы заказа, меню и пользователя.
        /// </summary>
        public async Task<IEnumerable<OrderModel>> GetOrdersByStatusWithDetailsAsync(string status)
        {
            return await _dbSet
                .Where(o => o.Status == status)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi.Category) 
                .Include(o => o.User) 
                .ToListAsync();
        }
    }
}
