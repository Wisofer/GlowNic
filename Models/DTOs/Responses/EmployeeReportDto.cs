namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de reporte de citas por empleado
/// </summary>
public class EmployeeAppointmentsReportDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TotalAppointments { get; set; }
    public List<EmployeeAppointmentStatsDto> ByEmployee { get; set; } = new();
}

/// <summary>
/// DTO de estadísticas de citas por empleado
/// </summary>
public class EmployeeAppointmentStatsDto
{
    public int? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Cancelled { get; set; }
    public int Total { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal AveragePerAppointment { get; set; }
}

/// <summary>
/// DTO de reporte de ingresos por empleado
/// </summary>
public class EmployeeIncomeReportDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal TotalIncome { get; set; }
    public List<EmployeeIncomeStatsDto> ByEmployee { get; set; } = new();
}

/// <summary>
/// DTO de estadísticas de ingresos por empleado
/// </summary>
public class EmployeeIncomeStatsDto
{
    public int? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public int Count { get; set; }
    public decimal FromAppointments { get; set; }
    public decimal Manual { get; set; }
    public decimal AveragePerTransaction { get; set; }
}

/// <summary>
/// DTO de reporte de egresos por empleado
/// </summary>
public class EmployeeExpensesReportDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal TotalExpenses { get; set; }
    public List<EmployeeExpenseStatsDto> ByEmployee { get; set; } = new();
}

/// <summary>
/// DTO de estadísticas de egresos por empleado
/// </summary>
public class EmployeeExpenseStatsDto
{
    public int? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalExpenses { get; set; }
    public int Count { get; set; }
    public Dictionary<string, decimal> Categories { get; set; } = new();
    public decimal AveragePerTransaction { get; set; }
}

/// <summary>
/// DTO de reporte general de actividad de empleados
/// </summary>
public class EmployeeActivityReportDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<EmployeeActivityStatsDto> Employees { get; set; } = new();
}

/// <summary>
/// DTO de estadísticas de actividad por empleado
/// </summary>
public class EmployeeActivityStatsDto
{
    public int? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int AppointmentsCompleted { get; set; }
    public int AppointmentsPending { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetContribution { get; set; }
    public decimal AveragePerAppointment { get; set; }
    public DateTime? LastActivity { get; set; }
}

/// <summary>
/// DTO de estadísticas de empleados para dashboard
/// </summary>
public class EmployeeStatsDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public List<EmployeePerformanceDto> TopPerformers { get; set; } = new();
}

/// <summary>
/// DTO de rendimiento de empleado
/// </summary>
public class EmployeePerformanceDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int AppointmentsCompleted { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal AveragePerAppointment { get; set; }
}


