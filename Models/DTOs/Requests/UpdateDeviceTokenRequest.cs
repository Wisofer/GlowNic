using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

public class UpdateDeviceTokenRequest
{
    [Required(ErrorMessage = "El nuevo token FCM es requerido")]
    [MaxLength(500, ErrorMessage = "El token FCM no puede exceder 500 caracteres")]
    public string NewFcmToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "El token FCM actual es requerido")]
    [MaxLength(500, ErrorMessage = "El token FCM no puede exceder 500 caracteres")]
    public string CurrentFcmToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "La plataforma es requerida")]
    [MaxLength(50, ErrorMessage = "La plataforma no puede exceder 50 caracteres")]
    public string Platform { get; set; } = string.Empty; // "android", "ios", "web"
}
