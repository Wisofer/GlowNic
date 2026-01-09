namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de salón para respuestas públicas
/// </summary>
public class BarberPublicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public List<ServiceDto> Services { get; set; } = new();
    public List<WorkingHoursDto> WorkingHours { get; set; } = new();
}

/// <summary>
/// DTO de salón para respuestas privadas (salón/admin)
/// </summary>
public class BarberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string QrUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Email { get; set; } // Email del usuario asociado
}

/// <summary>
/// DTO de servicio
/// </summary>
public class ServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO de horarios laborales
/// </summary>
public class WorkingHoursDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; }
}

