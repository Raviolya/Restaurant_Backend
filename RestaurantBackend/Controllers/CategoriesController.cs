using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantBackend.Data;
using RestaurantBackend.Models;

namespace RestaurantBackend.Controllers
{
    /// <summary>
    /// Управление категориями меню
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public CategoriesController(RestaurantDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить все категории меню
        /// </summary>
        /// <response code="200">Возвращает список категорий</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CategoryMenuItemModel>>> GetCategories()
        {
            return await _context.CategoryMenuItems.ToListAsync();
        }

        /// <summary>
        /// Создать новую категорию
        /// </summary>
        /// <param name="name">Название категории</param>
        /// <response code="201">Категория успешно создана</response>
        /// <response code="400">Некорректные данные</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CategoryMenuItemModel>> CreateCategory(string name)
        {
            var category = new CategoryMenuItemModel
            {
                Id = Guid.NewGuid(),
                Name = name
            };

            _context.CategoryMenuItems.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategories), category);
        }
    }
}