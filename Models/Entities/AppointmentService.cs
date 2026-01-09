namespace GlowNic.Models.Entities;

/// <summary>
/// Tabla intermedia para relación muchos-a-muchos entre Appointment y Service
/// Permite que una cita tenga múltiples servicios
/// </summary>
public class AppointmentService
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int ServiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public Appointment Appointment { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

