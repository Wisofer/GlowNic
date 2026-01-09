using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO para actualizar el perfil del salón
/// </summary>
public class UpdateBarberProfileRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "El nombre del negocio no puede exceder 200 caracteres")]
    public string? BusinessName { get; set; }

    [Required(ErrorMessage = "El teléfono es requerido")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Nueva contraseña (opcional). Si se proporciona, se actualizará la contraseña del usuario.
    /// </summary>
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string? Password { get; set; }
}

