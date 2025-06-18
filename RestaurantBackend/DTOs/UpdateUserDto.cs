using System.ComponentModel.DataAnnotations;

namespace RestaurantBackend.DTOs
{
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно быть от 2 до 50 символов")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилия должна быть от 2 до 50 символов")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [StringLength(100, ErrorMessage = "Email слишком длинный")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [RegularExpression(@"^\+?(\d{10,14})$", ErrorMessage = "Телефон должен содержать от 10 до 14 цифр")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "ID роли обязателен")]
        public Guid RoleId { get; set; } // Для изменения роли пользователя администратором
    }
}
