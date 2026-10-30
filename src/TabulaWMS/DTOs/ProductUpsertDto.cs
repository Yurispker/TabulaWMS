using System.ComponentModel.DataAnnotations;

namespace TabulaWMS.Models;

public class ProductUpsertDto
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\-_./]+$", ErrorMessage = "SKU can only contain letters, numbers, hyphens, and underscores.")]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [StringLength(150, ErrorMessage = "Name cannot be longer than 50 characters.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
    public int? Quantity { get; set; }

    [Range(0.0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0.")]
    public decimal Price { get; set; } = 0.0m;
}