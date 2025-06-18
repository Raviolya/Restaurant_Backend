using Microsoft.EntityFrameworkCore;
using RestaurantBackend.Data;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantBackend.Repositories
{
    /// <summary>
    /// Реализация репозитория для работы с элементами меню (MenuItemModel).
    /// </summary>
    public class MenuItemRepository : Repository<MenuItemModel>, IMenuItemRepository
    {
        public MenuItemRepository(RestaurantDbContext context) : base(context) { }

        /// <summary>
        /// Асинхронно получает элемент меню по его ID, включая связанную категорию.
        /// </summary>
        /// <param name="id">ID элемента меню.</param>
        /// <returns>MenuItemModel или null, если не найден.</returns>
        public async Task<MenuItemModel?> GetMenuItemByIdWithCategoryAsync(Guid id)
        {
            return await _dbSet
                .Include(m => m.Category) // Включаем связанную категорию
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Асинхронно получает список элементов меню по ID категории. Включает связанную категорию.
        /// </summary>
        /// <param name="categoryId">ID категории.</param>
        /// <returns>Коллекция MenuItemModel, принадлежащих указанной категории.</returns>
        public async Task<IEnumerable<MenuItemModel>> GetMenuItemsByCategoryAsync(Guid categoryId)
        {
            return await _dbSet
                .Include(m => m.Category) // Включаем связанную категорию
                .Where(m => m.CategoryId == categoryId)
                .ToListAsync();
        }

        /// <summary>
        /// Асинхронно ищет элементы меню по названию, описанию, категории или статусу доступности.
        /// </summary>
        /// <param name="searchTerm">Термин для поиска по названию или описанию (опционально).</param>
        /// <param name="categoryId">ID категории для фильтрации (опционально).</param>
        /// <param name="isAvailable">Статус доступности для фильтрации (опционально).</param>
        /// <returns>Коллекция MenuItemModel, соответствующих критериям поиска.</returns>
        public async Task<IEnumerable<MenuItemModel>> SearchMenuItemsAsync(string? searchTerm, Guid? categoryId, bool? isAvailable)
        {
            IQueryable<MenuItemModel> query = _dbSet.Include(m => m.Category); // Всегда включаем категорию

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(lowerSearchTerm) ||
                    (m.Description != null && m.Description.ToLower().Contains(lowerSearchTerm)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            if (isAvailable.HasValue)
            {
                query = query.Where(m => m.IsAvailable == isAvailable.Value);
            }

            return await query.ToListAsync();
        }
    }
}
