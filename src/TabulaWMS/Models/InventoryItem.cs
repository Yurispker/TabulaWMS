using System.ComponentModel.DataAnnotations;

namespace TabulaWMS.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Item name is required.")]
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "SKU is required.")]
        public string SKU { get; set; } = string.Empty;
        [Required(ErrorMessage = "Quantity is required.")]
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;

        // Optional fields
        public string? Description { get; set; }
    }
}