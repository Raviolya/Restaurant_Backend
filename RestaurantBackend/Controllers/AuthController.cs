using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using RestaurantBackend.Data;
using RestaurantBackend.DTOs;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using RestaurantBackend.Services;
using System;
using System.Security.Claims;
using BCrypt.Net;

namespace RestaurantBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly RestaurantDbContext _dbContext;

        public AuthController(IAuthService authService,
                              IConfiguration configuration,
                              IUserRepository userRepository,
                              RestaurantDbContext dbContext)
        {
            _authService = authService;
            _configuration = configuration;
            _userRepository = userRepository;
            _dbContext = dbContext;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserResponseDto>> Register([FromBody] CreateUserDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (userDto.DateOfBirth > DateTime.UtcNow.AddYears(-10))
                return BadRequest("Регистрация только для лиц старше 10 лет");

            if (await _userRepository.UserExists(userDto.Email, userDto.Phone))
                return Conflict("Пользователь с таким email или телефоном уже существует");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var customerRole = await _dbContext.RoleUsers.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole == null)
                return StatusCode(500, "Роль 'Customer' не найдена в базе данных. Пожалуйста, убедитесь, что роли инициализированы.");

            var user = new UserModel
            {
                Id = Guid.NewGuid(),
                Email = userDto.Email,
                Phone = userDto.Phone,
                Name = $"{userDto.FirstName} {userDto.LastName}",
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RoleId = customerRole.Id
            };

            try
            {
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                var authResponse = await _authService.Login(new LoginDto { Email = userDto.Email, Password = userDto.Password });
                SetAuthCookies(authResponse);

                return CreatedAtAction(nameof(Login), new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = "Customer"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при регистрации пользователя: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при регистрации пользователя: {ex.Message}");
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var authResponse = await _authService.Login(loginDto);
                SetAuthCookies(authResponse);
                return Ok(authResponse);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            if (Request?.Cookies == null)
                return BadRequest("Куки отсутствуют.");

            var refreshToken = Request.Cookies["refresh_token"];
            var accessToken = Request.Cookies["access_token"];

            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(accessToken))
                return BadRequest("Недействительные токены. Возможно, они отсутствуют или некорректны.");

            try
            {
                var authResponse = await _authService.RefreshToken(accessToken, refreshToken);
                SetAuthCookies(authResponse);
                return Ok(authResponse);
            }
            catch (SecurityTokenException ex)
            {
                Response?.Cookies.Delete("access_token");
                Response?.Cookies.Delete("refresh_token");
                return Unauthorized($"Недействительный токен обновления: {ex.Message}. Пожалуйста, войдите снова.");
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            if (User?.Identity == null)
                return Unauthorized("Не удалось определить пользователя. Токен некорректен.");

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Не удалось определить пользователя. Токен некорректен или не содержит email.");

            var result = await _authService.RevokeToken(email);

            Response?.Cookies.Delete("access_token");
            Response?.Cookies.Delete("refresh_token");

            if (!result)
                Console.WriteLine($"Ошибка при отзыве токена для пользователя: {email}");

            return Ok("Вы успешно вышли из системы.");
        }

        private void SetAuthCookies(AuthResponse authResponse)
        {
            if (authResponse == null)
                throw new ArgumentNullException(nameof(authResponse));

            if (Response?.Cookies == null)
                throw new InvalidOperationException("Response cookies are not available.");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = authResponse.Expiration,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Domain = _configuration["Jwt:CookieDomain"]
            };

            Response.Cookies.Append("access_token", authResponse.Token, cookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(
                    Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Domain = _configuration["Jwt:CookieDomain"]
            };

            Response.Cookies.Append("refresh_token", authResponse.RefreshToken, refreshCookieOptions);
        }
    }
}