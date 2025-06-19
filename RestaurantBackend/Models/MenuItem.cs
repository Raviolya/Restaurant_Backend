using System;
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema; 

namespace RestaurantBackend.Models;

public class MenuItemModel
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    [Column(TypeName = "decimal(18,2)")] 
    public decimal Price { get; set; }

    public Guid CategoryId { get; set; }
    public CategoryMenuItemModel Category { get; set; }

    [Required]
    public string[] Ingredients { get; set; } = [];

    public bool IsAvailable { get; set; }

    [MaxLength(500)] 
    public string? ImageUrl { get; set; } 

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
