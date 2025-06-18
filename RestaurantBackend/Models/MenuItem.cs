
namespace RestaurantBackend.Models;

public class MenuItemModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public CategoryMenuItemModel Category { get; set; }
    public string[] Ingredients { get; set; } = [];
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}