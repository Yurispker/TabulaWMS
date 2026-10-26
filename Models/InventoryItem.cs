namespace TabulaWMS.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;

        // Optional fields
        public string? Description { get; set; }
    }
}