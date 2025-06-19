using RestaurantBackend.DTOs;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantBackend.Services
{
    /// <summary>
    /// Сервис для управления заказами.
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMenuItemRepository _menuItemRepository;
        private readonly IUserRepository _userRepository; // Для получения имени пользователя

        public OrderService(IOrderRepository orderRepository, IMenuItemRepository menuItemRepository, IUserRepository userRepository)
        {
            _orderRepository = orderRepository;
            _menuItemRepository = menuItemRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Создает новый заказ для пользователя.
        /// </summary>
        /// <param name="userId">ID пользователя, создающего заказ.</param>
        /// <param name="orderDto">DTO с элементами заказа.</param>
        /// <returns>OrderResponseDto созданного заказа.</returns>
        public async Task<OrderResponseDto> CreateOrderAsync(Guid userId, CreateOrderDto orderDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("Пользователь не найден.");
            }

            var order = new OrderModel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = user, // Присваиваем объект пользователя напрямую (EF Core отслеживает)
                Status = "Pending", // Начальный статус заказа
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderItems = new List<OrderItemModel>()
            };

            decimal totalOrderPrice = 0;

            foreach (var itemDto in orderDto.Items)
            {
                var menuItem = await _menuItemRepository.GetMenuItemByIdWithCategoryAsync(itemDto.MenuItemId);
                if (menuItem == null || !menuItem.IsAvailable)
                {
                    throw new ArgumentException($"Элемент меню '{itemDto.MenuItemId}' не найден или недоступен.");
                }

                if (itemDto.Quantity <= 0)
                {
                    throw new ArgumentException($"Количество для элемента меню '{menuItem.Name}' должно быть больше нуля.");
                }

                var orderItem = new OrderItemModel
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Order = order, // Присваиваем объект заказа
                    MenuItemId = menuItem.Id,
                    MenuItem = menuItem, // Присваиваем объект MenuItem напрямую
                    Quantity = itemDto.Quantity,
                    ExcludedIngredients = itemDto.ExcludedIngredients,
                    Price = menuItem.Price, // Цена за единицу берется из MenuItem на момент заказа
                    CreatedAt = DateTime.UtcNow
                };

                order.OrderItems.Add(orderItem);
                totalOrderPrice += orderItem.Quantity * orderItem.Price;
            }

            order.TotalPrice = totalOrderPrice;

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            // Чтобы получить OrderResponseDto, нам нужно заново загрузить заказ с деталями
            // Или же сформировать DTO из уже имеющихся данных, если они достаточны
            var createdOrder = await _orderRepository.GetOrderByIdWithDetailsAsync(order.Id);
            return MapToOrderResponseDto(createdOrder!); // createdOrder не может быть null здесь
        }

        /// <summary>
        /// Получает детальную информацию о заказе по его ID.
        /// </summary>
        public async Task<OrderResponseDto?> GetOrderDetailsAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderByIdWithDetailsAsync(orderId);
            return order == null ? null : MapToOrderResponseDto(order);
        }

        /// <summary>
        /// Получает все заказы для указанного пользователя.
        /// </summary>
        public async Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(Guid userId)
        {
            var orders = await _orderRepository.GetUserOrdersWithDetailsAsync(userId);
            return orders.Select(MapToOrderResponseDto);
        }

        /// <summary>
        /// Получает все заказы для административного просмотра.
        /// </summary>
        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersForAdminAsync()
        {
            var orders = await _orderRepository.GetAllOrdersWithDetailsAsync();
            return orders.Select(MapToOrderResponseDto);
        }

        /// <summary>
        /// Обновляет статус заказа.
        /// </summary>
        public async Task<OrderResponseDto?> UpdateOrderStatusAsync(Guid orderId, string newStatus)
        {
            var order = await _orderRepository.GetOrderByIdWithDetailsAsync(orderId);
            if (order == null)
            {
                return null;
            }

           

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            return MapToOrderResponseDto(order);
        }

        /// <summary>
        /// Удаляет заказ по его ID.
        /// </summary>
        public async Task<bool> DeleteOrderAsync(Guid orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId); 
            if (order == null)
            {
                return false;
            }

            // Если OrderItems каскадно не удаляются при удалении Order,
            // нужно сначала удалить их вручную или настроить Cascade Delete в EF Core.
            // В данном случае, если настроен Cascade Delete (по умолчанию для обязательных отношений),
            // удаление OrderModel должно удалить OrderItemModel.

            _orderRepository.Remove(order);
            await _orderRepository.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Вспомогательный метод для маппинга OrderModel в OrderResponseDto.
        /// </summary>
        /// <param name="order">Исходная OrderModel.</param>
        /// <returns>OrderResponseDto.</returns>
        private OrderResponseDto MapToOrderResponseDto(OrderModel order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.Name ?? "Unknown User", 
                TotalPrice = order.TotalPrice,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    Id = oi.Id,
                    MenuItemId = oi.MenuItemId,
                    MenuItemName = oi.MenuItem?.Name ?? "Unknown Item",
                    Quantity = oi.Quantity,
                    ExcludedIngredients = oi.ExcludedIngredients,
                    Price = oi.Price
                }).ToList()
            };
        }

        public async Task<IEnumerable<OrderResponseDto>> GetPendingOrdersAsync()
        {
            var orders = await _orderRepository.GetOrdersByStatusWithDetailsAsync("Pending");
            return orders.Select(MapToOrderResponseDto);
        }

    }
}
