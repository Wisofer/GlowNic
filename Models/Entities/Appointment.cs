namespace GlowNic.Models.Entities;

/// <summary>
/// Cita agendada por un cliente
/// </summary>
public class Appointment
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public int? EmployeeId { get; set; } // Opcional: si la cita es atendida por un trabajador
    public int? ServiceId { get; set; } // Opcional: el cliente puede no saber qué servicio quiere
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public Barber Barber { get; set; } = null!;
    public Employee? Employee { get; set; } // Opcional: trabajador que atiende
    public Service? Service { get; set; } // Opcional
    public Transaction? Transaction { get; set; } // Ingreso generado por esta cita
}

/// <summary>
/// Estados de una cita
/// </summary>
public enum AppointmentStatus
{
    Pending = 1,    // Pendiente de confirmación
    Confirmed = 2,  // Confirmada (aceptada por el salón)
    Completed = 3,  // Completada (cita realizada)
    Cancelled = 4   // Cancelada
}

