using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantBackend.Data;
using RestaurantBackend.DTOs;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;

namespace RestaurantBackend.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly RestaurantDbContext _dbContext; 

        public UsersController(IUserRepository userRepository, RestaurantDbContext dbContext)
        {
            _userRepository = userRepository;
            _dbContext = dbContext; 
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserModel>>> GetUsers()
        {
            return Ok(await _userRepository.GetAllAsync());
        }

        [HttpPost("register-admin")]
        public async Task<ActionResult<UserResponseDto>> RegisterAdmin([FromBody] CreateUserDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (userDto.DateOfBirth > DateTime.Now.AddYears(-10))
                return BadRequest("Регистрация только для лиц старше 10 лет");

            if (await _userRepository.UserExists(userDto.Email, userDto.Phone))
                return Conflict("Пользователь с таким email или телефоном уже существует");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var adminRole = await _dbContext.RoleUsers.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                return StatusCode(500, "Роль 'Admin' не найдена в базе данных.");
            }

            var user = new UserModel
            {
                Id = Guid.NewGuid(),
                Email = userDto.Email,
                Phone = userDto.Phone,
                Name = $"{userDto.FirstName} {userDto.LastName}",
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RoleId = adminRole.Id 
            };

            try
            {
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                
                return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = "Admin" 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка при создании администратора: {ex.Message}");
            }
        }

    }
}