using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using RestaurantBackend.Data; 
using RestaurantBackend.DTOs;
using RestaurantBackend.Models;
using RestaurantBackend.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuItemsController : ControllerBase
    {
        private readonly IMenuItemRepository _menuItemRepository;
        private readonly RestaurantDbContext _dbContext; 

        public MenuItemsController(IMenuItemRepository menuItemRepository, RestaurantDbContext dbContext)
        {
            _menuItemRepository = menuItemRepository;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Получает полный список всех элементов меню.
        /// </summary>
        /// <returns>Список MenuItemResponseDto.</returns>
        [HttpGet]
        [AllowAnonymous] // Доступно всем
        public async Task<ActionResult<IEnumerable<MenuItemResponseDto>>> GetAllMenuItems()
        {
            var menuItems = await _menuItemRepository.SearchMenuItemsAsync(null, null, null);

            var menuItemDtos = menuItems.Select(m => new MenuItemResponseDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                CategoryId = m.CategoryId,
                CategoryName = m.Category?.Name ?? "N/A",
                Ingredients = m.Ingredients,
                IsAvailable = m.IsAvailable,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            });

            return Ok(menuItemDtos);
        }

        /// <summary>
        /// Получает элемент меню по его ID.
        /// </summary>
        /// <param name="id">ID элемента меню.</param>
        /// <returns>MenuItemResponseDto или NotFound.</returns>
        [HttpGet("{id}")]
        [AllowAnonymous] // Доступно всем
        public async Task<ActionResult<MenuItemResponseDto>> GetMenuItemById(Guid id)
        {
            var menuItem = await _menuItemRepository.GetMenuItemByIdWithCategoryAsync(id);

            if (menuItem == null)
                return NotFound($"Элемент меню с ID {id} не найден.");

            var menuItemDto = new MenuItemResponseDto
            {
                Id = menuItem.Id,
                Name = menuItem.Name,
                Description = menuItem.Description,
                Price = menuItem.Price,
                CategoryId = menuItem.CategoryId,
                CategoryName = menuItem.Category?.Name ?? "N/A",
                Ingredients = menuItem.Ingredients,
                IsAvailable = menuItem.IsAvailable,
                CreatedAt = menuItem.CreatedAt,
                UpdatedAt = menuItem.UpdatedAt
            };

            return Ok(menuItemDto);
        }

        /// <summary>
        /// Получает элементы меню по ID категории.
        /// </summary>
        /// <param name="categoryId">ID категории.</param>
        /// <returns>Список MenuItemResponseDto, принадлежащих указанной категории.</returns>
        [HttpGet("category/{categoryId}")]
        [AllowAnonymous] // Доступно всем
        public async Task<ActionResult<IEnumerable<MenuItemResponseDto>>> GetMenuItemsByCategoryId(Guid categoryId)
        {
            var category = await _dbContext.CategoryMenuItems.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound($"Категория с ID {categoryId} не найдена.");
            }

            var menuItems = await _menuItemRepository.GetMenuItemsByCategoryAsync(categoryId);

            var menuItemDtos = menuItems.Select(m => new MenuItemResponseDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                CategoryId = m.CategoryId,
                CategoryName = m.Category?.Name ?? "N/A",
                Ingredients = m.Ingredients,
                IsAvailable = m.IsAvailable,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            });

            return Ok(menuItemDtos);
        }

        /// <summary>
        /// Ищет элементы меню по названию, описанию, категории или статусу доступности.
        /// </summary>
        /// <param name="searchTerm">Термин для поиска (название, описание). Необязательный.</param>
        /// <param name="categoryId">ID категории для фильтрации. Необязательный.</param>
        /// <param name="isAvailable">Статус доступности для фильтрации (true/false). Необязательный.</param>
        /// <returns>Список MenuItemResponseDto, соответствующих критериям поиска.</returns>
        [HttpGet("search")]
        [AllowAnonymous] // Доступно всем
        public async Task<ActionResult<IEnumerable<MenuItemResponseDto>>> SearchMenuItems(
            [FromQuery] string? searchTerm,
            [FromQuery] Guid? categoryId,
            [FromQuery] bool? isAvailable)
        {
            var menuItems = await _menuItemRepository.SearchMenuItemsAsync(searchTerm, categoryId, isAvailable);

            var menuItemDtos = menuItems.Select(m => new MenuItemResponseDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                CategoryId = m.CategoryId,
                CategoryName = m.Category?.Name ?? "N/A",
                Ingredients = m.Ingredients,
                IsAvailable = m.IsAvailable,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            });

            return Ok(menuItemDtos);
        }

        /// <summary>
        /// Добавляет новый элемент меню. Доступно только администраторам.
        /// </summary>
        /// <param name="createDto">Данные для создания элемента меню.</param>
        /// <returns>Созданный MenuItemResponseDto.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<MenuItemResponseDto>> AddMenuItem([FromBody] CreateMenuItemDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _dbContext.CategoryMenuItems.FindAsync(createDto.CategoryId);
            if (category == null)
            {
                return BadRequest($"Категория с ID {createDto.CategoryId} не найдена.");
            }

            var menuItem = new MenuItemModel
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                Price = createDto.Price,
                CategoryId = createDto.CategoryId,
                Ingredients = createDto.Ingredients,
                IsAvailable = createDto.IsAvailable,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _menuItemRepository.AddAsync(menuItem);
                await _menuItemRepository.SaveChangesAsync();

                var createdMenuItemWithCategory = await _menuItemRepository.GetMenuItemByIdWithCategoryAsync(menuItem.Id);

                return CreatedAtAction(nameof(GetMenuItemById), new { id = createdMenuItemWithCategory.Id }, new MenuItemResponseDto
                {
                    Id = createdMenuItemWithCategory.Id,
                    Name = createdMenuItemWithCategory.Name,
                    Description = createdMenuItemWithCategory.Description,
                    Price = createdMenuItemWithCategory.Price,
                    CategoryId = createdMenuItemWithCategory.CategoryId,
                    CategoryName = createdMenuItemWithCategory.Category?.Name ?? "N/A",
                    Ingredients = createdMenuItemWithCategory.Ingredients,
                    IsAvailable = createdMenuItemWithCategory.IsAvailable,
                    CreatedAt = createdMenuItemWithCategory.CreatedAt,
                    UpdatedAt = createdMenuItemWithCategory.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении элемента меню: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при добавлении элемента меню: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет существующий элемент меню по его ID. Доступно только администраторам.
        /// </summary>
        /// <param name="id">ID элемента меню для обновления.</param>
        /// <param name="updateDto">Данные для обновления элемента меню.</param>
        /// <returns>Обновленный MenuItemResponseDto или NotFound/BadRequest.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<MenuItemResponseDto>> UpdateMenuItem(Guid id, [FromBody] UpdateMenuItemDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var menuItemToUpdate = await _menuItemRepository.GetMenuItemByIdWithCategoryAsync(id);
            if (menuItemToUpdate == null)
                return NotFound($"Элемент меню с ID {id} не найден.");

            if (menuItemToUpdate.CategoryId != updateDto.CategoryId)
            {
                var newCategory = await _dbContext.CategoryMenuItems.FindAsync(updateDto.CategoryId);
                if (newCategory == null)
                {
                    return BadRequest($"Новая категория с ID {updateDto.CategoryId} не найдена.");
                }
            }

            menuItemToUpdate.Name = updateDto.Name;
            menuItemToUpdate.Description = updateDto.Description;
            menuItemToUpdate.Price = updateDto.Price;
            menuItemToUpdate.CategoryId = updateDto.CategoryId;
            menuItemToUpdate.Ingredients = updateDto.Ingredients;
            menuItemToUpdate.IsAvailable = updateDto.IsAvailable;
            menuItemToUpdate.UpdatedAt = DateTime.UtcNow;

            try
            {
                _menuItemRepository.Update(menuItemToUpdate);
                await _menuItemRepository.SaveChangesAsync();

                var updatedMenuItemWithCategory = await _menuItemRepository.GetMenuItemByIdWithCategoryAsync(menuItemToUpdate.Id);

                return Ok(new MenuItemResponseDto
                {
                    Id = updatedMenuItemWithCategory.Id,
                    Name = updatedMenuItemWithCategory.Name,
                    Description = updatedMenuItemWithCategory.Description,
                    Price = updatedMenuItemWithCategory.Price,
                    CategoryId = updatedMenuItemWithCategory.CategoryId,
                    CategoryName = updatedMenuItemWithCategory.Category?.Name ?? "N/A",
                    Ingredients = updatedMenuItemWithCategory.Ingredients,
                    IsAvailable = updatedMenuItemWithCategory.IsAvailable,
                    CreatedAt = updatedMenuItemWithCategory.CreatedAt,
                    UpdatedAt = updatedMenuItemWithCategory.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении элемента меню: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при обновлении элемента меню: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаляет элемент меню по его ID. Доступно только администраторам.
        /// </summary>
        /// <param name="id">ID элемента меню для удаления.</param>
        /// <returns>NoContent или NotFound.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> DeleteMenuItem(Guid id)
        {
            var menuItemToDelete = await _menuItemRepository.GetByIdAsync(id);
            if (menuItemToDelete == null)
                return NotFound($"Элемент меню с ID {id} не найден.");

            try
            {
                _menuItemRepository.Remove(menuItemToDelete);
                await _menuItemRepository.SaveChangesAsync();

                return NoContent(); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении элемента меню: {ex.Message}");
                return StatusCode(500, $"Произошла ошибка при удалении элемента меню: {ex.Message}");
            }
        }
    }
}
