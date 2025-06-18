using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace RestaurantBackend.Services
{
    /// <summary>
    /// Реализация сервиса кэширования с использованием Redis.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly TimeSpan _defaultCacheExpiration;

        public RedisCacheService(IConnectionMultiplexer redis, IConfiguration configuration)
        {
            _database = redis.GetDatabase();
            // Получаем срок действия кэша из конфигурации или устанавливаем значение по умолчанию
            _defaultCacheExpiration = TimeSpan.FromMinutes(
                Convert.ToDouble(configuration["Redis:CacheExpirationMinutes"] ?? "5"));
        }

        /// <summary>
        /// Получает данные из кэша.
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            var cachedValue = await _database.StringGetAsync(key);
            if (cachedValue.IsNullOrEmpty)
            {
                return default;
            }
            // Десериализуем JSON из Redis в объект
            return JsonSerializer.Deserialize<T>(cachedValue!);
        }

        /// <summary>
        /// Устанавливает данные в кэш.
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var jsonValue = JsonSerializer.Serialize(value);
            // Устанавливаем значение в Redis с указанным или дефолтным сроком действия
            await _database.StringSetAsync(key, jsonValue, expiration ?? _defaultCacheExpiration);
        }

        /// <summary>
        /// Удаляет данные из кэша.
        /// </summary>
        public async Task<bool> RemoveAsync(string key)
        {
            return await _database.KeyDeleteAsync(key);
        }
    }
}
