using RestaurantBackend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantBackend.Repositories.Interfaces
{
    /// <summary>
    /// Интерфейс репозитория для работы с элементами меню (MenuItemModel).
    /// Расширяет общий репозиторий IRepository для базовых CRUD операций.
    /// </summary>
    public interface IMenuItemRepository : IRepository<MenuItemModel>
    {
        /// <summary>
        /// Асинхронно получает элемент меню по его ID, включая связанную категорию.
        /// </summary>
        /// <param name="id">ID элемента меню.</param>
        /// <returns>MenuItemModel или null, если не найден.</returns>
        Task<MenuItemModel?> GetMenuItemByIdWithCategoryAsync(Guid id);

        /// <summary>
        /// Асинхронно получает список элементов меню по ID категории. Включает связанную категорию.
        /// </summary>
        /// <param name="categoryId">ID категории.</param>
        /// <returns>Коллекция MenuItemModel, принадлежащих указанной категории.</returns>
        Task<IEnumerable<MenuItemModel>> GetMenuItemsByCategoryAsync(Guid categoryId);

        /// <summary>
        /// Асинхронно ищет элементы меню по названию, описанию, категории или статусу доступности.
        /// </summary>
        /// <param name="searchTerm">Термин для поиска по названию или описанию (опционально).</param>
        /// <param name="categoryId">ID категории для фильтрации (опционально).</param>
        /// <param name="isAvailable">Статус доступности для фильтрации (опционально).</param>
        /// <returns>Коллекция MenuItemModel, соответствующих критериям поиска.</returns>
        Task<IEnumerable<MenuItemModel>> SearchMenuItemsAsync(string? searchTerm, Guid? categoryId, bool? isAvailable);
    }
}
