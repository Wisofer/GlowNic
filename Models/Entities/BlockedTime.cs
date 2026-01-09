namespace GlowNic.Models.Entities;

/// <summary>
/// Horarios bloqueados temporalmente (vacaciones, días libres, etc.)
/// </summary>
public class BlockedTime
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Reason { get; set; } // Motivo del bloqueo
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public Barber Barber { get; set; } = null!;
}

