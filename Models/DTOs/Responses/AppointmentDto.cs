namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de cita
/// </summary>
public class AppointmentDto
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public string BarberName { get; set; } = string.Empty;
    public int? EmployeeId { get; set; } // Opcional: trabajador que atiende la cita
    public string? EmployeeName { get; set; } // Opcional: nombre del trabajador
    
    // Compatibilidad: primer servicio (mantener para retrocompatibilidad)
    public int? ServiceId { get; set; } // Opcional
    public string? ServiceName { get; set; } // Opcional
    public decimal? ServicePrice { get; set; } // Opcional
    
    // Nuevo: lista de todos los servicios
    public List<ServiceDto> Services { get; set; } = new List<ServiceDto>();
    
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

