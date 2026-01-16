using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

public class UpdateDeviceTokenRequest
{
    [Required(ErrorMessage = "El token FCM es requerido")]
    [MaxLength(500, ErrorMessage = "El token FCM no puede exceder 500 caracteres")]
    public string FcmToken { get; set; } = string.Empty;
}
