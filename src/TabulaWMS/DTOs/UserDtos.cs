namespace TabulaWMS.DTOs;

public class UpdateRoleDto
{
    public string Role { get; set; } = string.Empty;
}

public class UserResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}