namespace GlowNic.Models.Entities;

/// <summary>
/// Servicios que ofrece un salón (corte, barba, etc.)
/// </summary>
public class Service
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; } // Duración en minutos (30, 60, etc.)
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public Barber Barber { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

