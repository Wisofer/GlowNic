namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// Respuesta con URL de WhatsApp para notificar al cliente
/// </summary>
public class WhatsAppUrlResponse
{
    public string Url { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

