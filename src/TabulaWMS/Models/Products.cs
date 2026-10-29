using System.ComponentModel.DataAnnotations;

namespace TabulaWMS.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item name is required.")]
        [StringLength(50, ErrorMessage = "Name cannot be longer than 50 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "SKU is required.")]
        [RegularExpression(@"^[a-zA-Z0-9\-_]+$",
        ErrorMessage = "SKU can only contain letters, numbers, hyphens, and underscores. No spaces or special symbols.")]
        [StringLength(10, ErrorMessage = "SKU cannot be longer than 10 characters.")]
        public string SKU { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0.")]
        public int Quantity { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }


        // Optional fields
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Location cannot be longer than 50 characters.")]
        public string? Location { get; set; }

        [Range(0.0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0.")]
        public decimal Price { get; set; } = 0.0m;
    }
}