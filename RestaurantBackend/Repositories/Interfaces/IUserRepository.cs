using RestaurantBackend.Models;

namespace RestaurantBackend.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<UserModel>
    {
        Task<UserModel> GetByEmailAsync(string email);
        Task<UserModel> GetByPhoneAsync(string phone);
        Task<bool> UserExists(string email, string phone);

        Task<UserModel?> GetUserByRefreshToken(string refreshToken);
    }
}