namespace TabulaWMS.Models.DTOs;

using System.ComponentModel.DataAnnotations;

public class CreateTenantDto
{
    [Required]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tenant ID must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-z0-9_]+$", ErrorMessage = "Tenant ID must contain only lowercase letters, numbers, and underscores (no spaces or special characters).")]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string AdminPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AdminName { get; set; } = string.Empty;
}