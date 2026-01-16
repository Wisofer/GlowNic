using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

public class RegisterDeviceRequest
{
    [Required(ErrorMessage = "El token FCM es requerido")]
    [MaxLength(500, ErrorMessage = "El token FCM no puede exceder 500 caracteres")]
    public string FcmToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "La plataforma es requerida")]
    [MaxLength(50, ErrorMessage = "La plataforma no puede exceder 50 caracteres")]
    public string Platform { get; set; } = string.Empty; // "android", "ios", "web", "unknown"
}
