using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TabulaWMS.Models;

public class User : IdentityUser
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Role { get; set; } = string.Empty;
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public string CreatedBy { get; set; } = string.Empty;
}