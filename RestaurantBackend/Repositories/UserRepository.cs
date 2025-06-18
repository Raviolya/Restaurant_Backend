using Microsoft.EntityFrameworkCore;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories;
using RestaurantBackend.Repositories.Interfaces;
using System.Linq; 

namespace RestaurantBackend.Data
{
    public class UserRepository : Repository<UserModel>, IUserRepository
    {
        public UserRepository(RestaurantDbContext context) : base(context) { }

        /// <summary>
        /// Асинхронно получает пользователя по его email. Включает связанную роль.
        /// </summary>
        /// <param name="email">Email пользователя.</param>
        /// <returns>UserModel или null, если не найден.</returns>
        public async Task<UserModel?> GetByEmailAsync(string email)
            => await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        /// <summary>
        /// Асинхронно получает пользователя по его номеру телефона.
        /// </summary>
        /// <param name="phone">Номер телефона пользователя.</param>
        /// <returns>UserModel или null, если не найден.</returns>
        public async Task<UserModel?> GetByPhoneAsync(string phone)
            => await _dbSet.FirstOrDefaultAsync(u => u.Phone == phone);

        /// <summary>
        /// Проверяет, существует ли пользователь с заданным email или номером телефона.
        /// </summary>
        /// <param name="email">Email для проверки.</param>
        /// <param name="phone">Номер телефона для проверки.</param>
        /// <returns>True, если пользователь существует, иначе False.</returns>
        public async Task<bool> UserExists(string email, string phone)
            => await _dbSet.AnyAsync(u => u.Email == email || u.Phone == phone);

        /// <summary>
        /// Асинхронно получает пользователя по его refresh токену,
        /// если токен совпадает и не истек. Включает связанную роль.
        /// </summary>
        /// <param name="refreshToken">Refresh токен пользователя.</param>
        /// <returns>UserModel или null, если не найден.</returns>
        public async Task<UserModel?> GetUserByRefreshToken(string refreshToken)
        {
            return await _dbSet
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.RefreshToken == refreshToken &&
                    u.RefreshTokenExpiryTime > DateTime.UtcNow);
        }


        /// <summary>
        /// Асинхронно получает пользователя по его ID, включая связанную роль.
        /// </summary>
        /// <param name="id">ID пользователя.</param>
        /// <returns>UserModel или null, если не найден.</returns>
        public async Task<UserModel?> GetUserByIdWithRoleAsync(Guid id)
        {
            return await _dbSet
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Асинхронно ищет пользователей по email, имени или телефону, с возможностью фильтрации по роли.
        /// </summary>
        /// <param name="searchTerm">Термин для поиска (email, имя, телефон).</param>
        /// <param name="roleId">ID роли для фильтрации.</param>
        /// <returns>Коллекция UserModel, соответствующих критериям поиска.</returns>
        public async Task<IEnumerable<UserModel>> SearchUsersAsync(string? searchTerm, Guid? roleId)
        {
            IQueryable<UserModel> query = _dbSet.Include(u => u.Role); // Всегда включаем роль

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(lowerSearchTerm) ||
                    u.Name.ToLower().Contains(lowerSearchTerm) ||
                    u.Phone.ToLower().Contains(lowerSearchTerm));
            }

            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            return await query.ToListAsync();
        }
    }
}
