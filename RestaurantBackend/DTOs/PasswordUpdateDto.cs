using System.ComponentModel.DataAnnotations;

namespace RestaurantBackend.DTOs
{
    public class PasswordUpdateDto
    {
        [Required(ErrorMessage = "Новый пароль обязателен")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 100 символов")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$",
            ErrorMessage = "Пароль должен содержать буквы (хотя бы одна строчная и одна заглавная), цифры и специальные символы")]
        public string NewPassword { get; set; }

        [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
        public string ConfirmNewPassword { get; set; }
    }
}
