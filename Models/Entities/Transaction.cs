namespace GlowNic.Models.Entities;

/// <summary>
/// Transacciones financieras (ingresos y egresos) del salón
/// </summary>
public class Transaction
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public int? EmployeeId { get; set; } // Opcional: si el ingreso/egreso es de un trabajador
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; } // Service, Rent, Utilities, etc.
    public DateTime Date { get; set; }
    public int? AppointmentId { get; set; } // Si es ingreso de una cita
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones
    public Barber Barber { get; set; } = null!;
    public Employee? Employee { get; set; } // Opcional: trabajador que generó la transacción
    public Appointment? Appointment { get; set; }
}

/// <summary>
/// Tipo de transacción
/// </summary>
public enum TransactionType
{
    Income = 1,   // Ingreso
    Expense = 2   // Egreso
}

