using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO para crear una cita (público o salón)
/// </summary>
public class CreateAppointmentRequest
{
    // Opcional: solo requerido para creación pública, no para salón autenticado
    public string? BarberSlug { get; set; }

    // Opcional: el cliente puede seleccionar uno o varios servicios
    public int[]? ServiceIds { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es requerido")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string ClientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono del cliente es requerido")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string ClientPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es requerida")]
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "La hora es requerida")]
    public TimeOnly Time { get; set; }
}

