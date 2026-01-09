namespace GlowNic.Models.Entities;

/// <summary>
/// Entidad que representa un salón en el sistema
/// </summary>
public class Barber
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // URL única: juan-perez
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public User User { get; set; } = null!;
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
    public ICollection<BlockedTime> BlockedTimes { get; set; } = new List<BlockedTime>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>(); // Trabajadores del salón
}

