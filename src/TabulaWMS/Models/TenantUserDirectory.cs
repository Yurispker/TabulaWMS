using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TabulaWMS.Models;

[Index(nameof(Email), IsUnique = true)]
public class TenantUserDirectory
{
    [Key]
    [MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [MaxLength(64)]
    public string TenantId { get; set; } = default!;
}