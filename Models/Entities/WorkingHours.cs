namespace GlowNic.Models.Entities;

/// <summary>
/// Horarios laborales del salón por día de la semana
/// </summary>
public class WorkingHours
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public DayOfWeek DayOfWeek { get; set; } // 0=Domingo, 6=Sábado
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    // Relaciones
    public Barber Barber { get; set; } = null!;
}

