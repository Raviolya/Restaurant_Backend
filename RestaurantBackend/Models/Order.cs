using System;
using System.Collections.Generic;

namespace RestaurantBackend.Models;

public class OrderModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required UserModel User { get; set; }
    public decimal TotalPrice { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemModel> OrderItems { get; set; } = [];
}
