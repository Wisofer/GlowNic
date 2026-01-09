namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO para crear un nuevo salón
/// </summary>
public class CreateBarberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string Phone { get; set; } = string.Empty;
}

