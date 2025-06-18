using RestaurantBackend.Models;

namespace RestaurantBackend.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<UserModel>
    {
        Task<UserModel> GetByEmailAsync(string email);
        Task<UserModel> GetByPhoneAsync(string phone);
        Task<bool> UserExists(string email, string phone);

        Task<UserModel?> GetUserByRefreshToken(string refreshToken);

        /// <summary>
        /// Асинхронно получает пользователя по его ID, включая связанную роль.
        /// </summary>
        /// <param name="id">ID пользователя.</param>
        /// <returns>UserModel или null, если не найден.</returns>
        Task<UserModel?> GetUserByIdWithRoleAsync(Guid id);

        /// <summary>
        /// Асинхронно ищет пользователей по email, имени или телефону.
        /// </summary>
        /// <param name="searchTerm">Термин для поиска по email, имени или телефону (опционально).</param>
        /// <param name="roleId">ID роли для фильтрации (опционально).</param>
        /// <returns>Коллекция UserModel, соответствующих критериям поиска.</returns>
        Task<IEnumerable<UserModel>> SearchUsersAsync(string? searchTerm, Guid? roleId);
    }
}