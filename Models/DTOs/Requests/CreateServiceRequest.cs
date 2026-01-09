using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO para crear un servicio
/// </summary>
public class CreateServiceRequest
{
    [Required(ErrorMessage = "El nombre del servicio es requerido")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es requerido")]
    [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Price { get; set; }

    [Range(15, 480, ErrorMessage = "La duración debe estar entre 15 y 480 minutos")]
    public int? DurationMinutes { get; set; } // Opcional, default 30 minutos
}

