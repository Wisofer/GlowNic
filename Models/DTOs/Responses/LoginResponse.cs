using System.Text.Json.Serialization;

namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de respuesta para login
/// </summary>
public class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
    
    [JsonPropertyName("user")]
    public UserDto User { get; set; } = null!;
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// DTO de usuario para respuestas
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public BarberDto? Barber { get; set; }
}

