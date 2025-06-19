using System.ComponentModel.DataAnnotations;

namespace RestaurantBackend.DTOs
{
    public class UpdateCurrentUserDto
    {
        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Телефон обязателен.")]
        [Phone(ErrorMessage = "Некорректный формат телефона.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Имя обязательно.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно содержать от 2 до 100 символов.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Фамилия должна содержать от 2 до 100 символов.")]
        public string LastName { get; set; }
    }
}