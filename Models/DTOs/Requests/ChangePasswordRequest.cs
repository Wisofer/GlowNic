using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO para cambiar contraseña
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres")]
    public string NewPassword { get; set; } = string.Empty;
}

