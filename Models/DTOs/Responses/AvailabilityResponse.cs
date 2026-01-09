namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de respuesta para disponibilidad
/// </summary>
public class AvailabilityResponse
{
    public DateOnly Date { get; set; }
    public List<TimeSlotDto> AvailableSlots { get; set; } = new();
}

/// <summary>
/// DTO de slot de tiempo disponible
/// </summary>
public class TimeSlotDto
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; }
}

