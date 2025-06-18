using System;
using System.Threading.Tasks;

namespace RestaurantBackend.Services
{
    /// <summary>
    /// Интерфейс для сервиса кэширования.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Получает данные из кэша.
        /// </summary>
        /// <typeparam name="T">Тип данных.</typeparam>
        /// <param name="key">Ключ кэша.</param>
        /// <returns>Объект T из кэша или null, если не найден.</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Устанавливает данные в кэш.
        /// </summary>
        /// <typeparam name="T">Тип данных.</typeparam>
        /// <param name="key">Ключ кэша.</param>
        /// <param name="value">Значение для кэширования.</param>
        /// <param name="expiration">Срок действия кэша (опционально, по умолчанию 5 минут).</param>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Удаляет данные из кэша.
        /// </summary>
        /// <param name="key">Ключ кэша.</param>
        /// <returns>True, если успешно удалено, иначе False.</returns>
        Task<bool> RemoveAsync(string key);
    }
}
