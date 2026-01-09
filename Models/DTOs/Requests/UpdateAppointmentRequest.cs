using GlowNic.Models.Entities;

namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO para actualizar una cita
/// </summary>
public class UpdateAppointmentRequest
{
    public AppointmentStatus? Status { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? Time { get; set; }
    public int? ServiceId { get; set; } // Opcional: permite agregar/actualizar servicio al aceptar la cita (legacy)
    public int[]? ServiceIds { get; set; } // Opcional: permite agregar múltiples servicios al completar la cita
}

