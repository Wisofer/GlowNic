namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de dashboard del salón
/// </summary>
public class BarberDashboardDto
{
    public BarberDto Barber { get; set; } = null!;
    public TodayStatsDto Today { get; set; } = null!;
    public PeriodStatsDto ThisWeek { get; set; } = null!;
    public PeriodStatsDto ThisMonth { get; set; } = null!;
    public List<AppointmentDto> RecentAppointments { get; set; } = new();
    public List<AppointmentDto> UpcomingAppointments { get; set; } = new();
    public EmployeeStatsDto? EmployeeStats { get; set; } // Nuevo: Estadísticas de empleados
}

/// <summary>
/// DTO de estadísticas del día
/// </summary>
public class TodayStatsDto
{
    public int Appointments { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
}

/// <summary>
/// DTO de estadísticas de período
/// </summary>
public class PeriodStatsDto
{
    public int Appointments { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public int UniqueClients { get; set; } // Clientes únicos atendidos
    public decimal AveragePerClient { get; set; } // Promedio de ingresos por cliente
}

/// <summary>
/// DTO de dashboard del admin
/// </summary>
public class AdminDashboardDto
{
    public int TotalBarbers { get; set; }
    public int ActiveBarbers { get; set; }
    public int InactiveBarbers { get; set; }
    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<BarberSummaryDto> RecentBarbers { get; set; } = new();
}

/// <summary>
/// DTO de resumen de salón para admin
/// </summary>
public class BarberSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
}

