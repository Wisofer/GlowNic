namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO de solicitud para refrescar token
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}


