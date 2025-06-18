using Microsoft.IdentityModel.Tokens;
using RestaurantBackend.DTOs;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;

namespace RestaurantBackend.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> Login(LoginDto loginDto);
        Task<AuthResponse> RefreshToken(string token, string refreshToken);
        Task<bool> RevokeToken(string email);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, ITokenService tokenService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        /// <summary>
        /// Проверяет учетные данные пользователя и выдает токены.
        /// </summary>
        /// <param name="loginDto">Email и пароль пользователя.</param>
        /// <returns>AuthResponse с токенами и информацией о пользователе.</returns>
        /// <exception cref="UnauthorizedAccessException">Если учетные данные недействительны.</exception>
        public async Task<AuthResponse> Login(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Недействительные учетные данные (email или пароль).");
            }

            var token = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
                Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"]));

            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role?.Name ?? "Неизвестно" 
                }
            };
        }

        /// <summary>
        /// Обновляет access и refresh токены с помощью существующего refresh токена.
        /// </summary>
        /// <param name="token">Истекший access токен.</param>
        /// <param name="refreshToken">Текущий refresh токен.</param>
        /// <returns>AuthResponse с новыми токенами.</returns>
        /// <exception cref="SecurityTokenException">Если refresh токен недействителен или истек.</exception>
        public async Task<AuthResponse> RefreshToken(string token, string refreshToken)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(token);
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                throw new SecurityTokenException("Не удалось извлечь email из токена.");
            }

            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null || user.RefreshToken != refreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow) 
                throw new SecurityTokenException("Недействительный или истекший refresh токен. Пожалуйста, войдите снова.");

            var newToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
                Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"]));
            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role?.Name ?? "Неизвестно" 
                }
            };
        }

        /// <summary>
        /// Отзывает refresh токен пользователя, делая его непригодным для дальнейшего использования.
        /// </summary>
        /// <param name="email">Email пользователя, чей токен отзывается.</param>
        /// <returns>True, если токен успешно отозван, False в противном случае.</returns>
        public async Task<bool> RevokeToken(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return false;

           
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userRepository.SaveChangesAsync();

            return true;
        }
    }
}
