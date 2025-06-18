namespace RestaurantBackend.Models;

public class OrderItemModel
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required OrderModel Order { get; set; }
    public Guid MenuItemId { get; set; }
    public required MenuItemModel MenuItem { get; set; }
    public int Quantity { get; set; }
    public string[] ExcludedIngredients { get; set; } = [];
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}