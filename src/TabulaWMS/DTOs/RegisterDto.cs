using System.ComponentModel.DataAnnotations;

namespace TabulaWMS.DTOs;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
    public required string Password { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string Role { get; set; } // e.g., "WarehouseOperator", "InventoryManager"
}