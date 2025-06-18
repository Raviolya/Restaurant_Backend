namespace RestaurantBackend.Models;

public class UserModel
{
    public Guid Id { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public Guid RoleId { get; set; }
    public RoleUserModel Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
