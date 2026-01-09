namespace GlowNic.Models.Entities;

/// <summary>
/// Entidad que representa un trabajador/empleado de un salón (dueño)
/// </summary>
public class Employee
{
    public int Id { get; set; }
    public int OwnerBarberId { get; set; } // Barbero dueño que creó este trabajador
    public int UserId { get; set; } // Usuario asociado (con rol Employee)
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public Barber OwnerBarber { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

