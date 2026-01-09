namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de respuesta para QR
/// </summary>
public class QrUrlResponse
{
    public string Url { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty; // Base64 de la imagen QR
}

