using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using RestaurantBackend.Data;
using RestaurantBackend.DTOs;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using System.Collections.Generic; 
using System.Linq;
using System.Security.Claims; 

namespace RestaurantBackend.Controllers
{
    
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

        /// <summary>
        /// Получает список всех пользователей. Доступно только администраторам.
        /// </summary>
        /// <returns>Список UserResponseDto.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            var users = await _userRepository.GetAllAsync(); 
            var usersWithRoles = await _userRepository.SearchUsersAsync(null, null);

            var userDtos = usersWithRoles.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Role = u.Role?.Name ?? "Unknown" // Безопасный доступ к имени роли
            });

            return Ok(userDtos);
        }


        /// <summary>
        /// Обновляет данные текущего аутентифицированного пользователя.
        /// Доступно только аутентифицированным пользователям для обновления своих данных.
        /// </summary>
        /// <param name="updateDto">Данные для обновления пользователя.</param>
        /// <returns>Обновленный UserResponseDto или NotFound/BadRequest.</returns>
        [HttpPut("current")]
        [Authorize] 
        public async Task<ActionResult<CurrentUserProfileDto>> UpdateCurrentUser([FromBody] UpdateCurrentUserDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized("Не удалось определить ID текущего пользователя.");
            }

            var userToUpdate = await _userRepository.GetUserByIdWithRoleAsync(currentUserId);
            if (userToUpdate == null)
            {
                return NotFound("Информация о текущем пользователе не найдена.");
            }

            var existingUserWithEmail = await _userRepository.GetByEmailAsync(updateDto.Email);
            if (existingUserWithEmail != null && existingUserWithEmail.Id != currentUserId)
            {
                return Conflict("Пользователь с таким email уже существует.");
            }

            var existingUserWithPhone = await _userRepository.GetByPhoneAsync(updateDto.Phone);
            if (existingUserWithPhone != null && existingUserWithPhone.Id != currentUserId)
            {
                return Conflict("Пользователь с таким телефоном уже существует.");
            }

            userToUpdate.Email = updateDto.Email;
            userToUpdate.Phone = updateDto.Phone;
            userToUpdate.Name = $"{updateDto.FirstName} {updateDto.LastName}";
            userToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                _userRepository.Update(userToUpdate);
                await _userRepository.SaveChangesAsync();

                var updatedUserDto = new CurrentUserProfileDto
                {
                    Id = userToUpdate.Id,
                    Email = userToUpdate.Email,
                    Name = userToUpdate.Name,
                    Role = userToUpdate.Role?.Name ?? "Unknown",
                    Phone = userToUpdate.Phone
                };

                return Ok(updatedUserDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении текущего пользователя: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при обновлении пользователя: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает информацию о текущем аутентифицированном пользователе.
        /// Доступно аутентифицированным пользователям.
        /// </summary>
        /// <returns>CurrentUserProfileDto текущего пользователя.</returns>
        [HttpGet("current")]
        [Authorize]
        public async Task<ActionResult<CurrentUserProfileDto>> GetCurrentUser() 
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не удалось определить ID текущего пользователя.");
            }

            var user = await _userRepository.GetUserByIdWithRoleAsync(userId);

            if (user == null)
            {
                return NotFound("Информация о текущем пользователе не найдена.");
            }

            var userDto = new CurrentUserProfileDto 
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role?.Name ?? "Unknown",
                Phone = user.Phone 
            };

            return Ok(userDto);
        }

        /// <summary>
        /// Регистрирует нового администратора. Доступно только существующим администраторам.
        /// </summary>
        /// <param name="userDto">Данные для создания нового администратора.</param>
        /// <returns>Созданный UserResponseDto.</returns>
        [HttpPost("register-admin")]
        [Authorize(Roles = "Admin")]
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
                return StatusCode(500, "Роль 'Admin' не найдена в базе данных. Пожалуйста, убедитесь, что роли инициализированы.");
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
                Console.WriteLine($"Ошибка при создании администратора: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при создании администратора: {ex.Message}");
            }
        }


        /// <summary>
        /// Получает пользователя по его ID. Доступно только администраторам.
        /// </summary>
        /// <param name="id">ID пользователя.</param>
        /// <returns>UserResponseDto или NotFound.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserResponseDto>> GetUserById(Guid id)
        {
            var user = await _userRepository.GetUserByIdWithRoleAsync(id); 

            if (user == null)
                return NotFound($"Пользователь с ID {id} не найден.");

            var userDto = new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role?.Name ?? "Unknown" 
            };

            return Ok(userDto);
        }

        /// <summary>
        /// Ищет пользователей по email, имени или телефону, с возможностью фильтрации по роли.
        /// Доступно только администраторам.
        /// </summary>
        /// <param name="searchTerm">Термин для поиска (email, имя, телефон). Необязательный.</param>
        /// <param name="roleId">ID роли для фильтрации. Необязательный.</param>
        /// <returns>Список UserResponseDto, соответствующих критериям поиска.</returns>
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> SearchUsers(
            [FromQuery] string? searchTerm,
            [FromQuery] Guid? roleId)
        {
            var users = await _userRepository.SearchUsersAsync(searchTerm, roleId);

            var userDtos = users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Role = u.Role?.Name ?? "Unknown" // Безопасный доступ к имени роли
            });

            return Ok(userDtos);
        }

        /// <summary>
        /// Обновляет данные пользователя по его ID. Доступно только администраторам.
        /// </summary>
        /// <param name="id">ID пользователя для обновления.</param>
        /// <param name="userDto">Данные для обновления пользователя.</param>
        /// <returns>Обновленный UserResponseDto или NotFound/BadRequest.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userToUpdate = await _userRepository.GetUserByIdWithRoleAsync(id);
            if (userToUpdate == null)
                return NotFound($"Пользователь с ID {id} не найден.");

            var existingUserWithEmail = await _userRepository.GetByEmailAsync(userDto.Email);
            if (existingUserWithEmail != null && existingUserWithEmail.Id != id)
                return Conflict("Пользователь с таким email уже существует.");

            var existingUserWithPhone = await _userRepository.GetByPhoneAsync(userDto.Phone);
            if (existingUserWithPhone != null && existingUserWithPhone.Id != id)
                return Conflict("Пользователь с таким телефоном уже существует.");

            var newRole = await _dbContext.RoleUsers.FirstOrDefaultAsync(r => r.Id == userDto.RoleId);
            if (newRole == null)
                return BadRequest("Указанная роль не существует.");

            // Обновляем поля пользователя
            userToUpdate.Email = userDto.Email;
            userToUpdate.Phone = userDto.Phone;
            userToUpdate.Name = $"{userDto.FirstName} {userDto.LastName}";
            userToUpdate.RoleId = newRole.Id;
            userToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                _userRepository.Update(userToUpdate); 
                await _userRepository.SaveChangesAsync();

                var updatedUserDto = new UserResponseDto
                {
                    Id = userToUpdate.Id,
                    Email = userToUpdate.Email,
                    Name = userToUpdate.Name,
                    Role = newRole.Name
                };

                return Ok(updatedUserDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении пользователя: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при обновлении пользователя: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаляет пользователя по его ID. Доступно только администраторам.
        /// </summary>
        /// <param name="id">ID пользователя для удаления.</param>
        /// <returns>NoContent или NotFound.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            var userToDelete = await _userRepository.GetByIdAsync(id); 
            if (userToDelete == null)
                return NotFound($"Пользователь с ID {id} не найден.");

            try
            {
         
                if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value == id.ToString())
                {
                    return BadRequest("Вы не можете удалить свой собственный аккаунт.");
                }

                _userRepository.Remove(userToDelete); 
                await _userRepository.SaveChangesAsync();

                return NoContent(); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении пользователя: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при удалении пользователя: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет пароль пользователя. Доступно только администраторам.
        /// </summary>
        /// <param name="id">ID пользователя, чей пароль нужно обновить.</param>
        /// <param name="passwordUpdateDto">DTO с новым паролем.</param>
        /// <returns>Ok или NotFound/BadRequest.</returns>
        [HttpPut("{id}/password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserPassword(Guid id, [FromBody] PasswordUpdateDto passwordUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound($"Пользователь с ID {id} не найден.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordUpdateDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
                return Ok("Пароль пользователя успешно обновлен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении пароля пользователя: {ex.Message}");
                return StatusCode(500, "Произошла ошибка при обновлении пароля пользователя.");
            }
        }
    }
}
