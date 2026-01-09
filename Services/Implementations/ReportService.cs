using GlowNic.Data;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para generar reportes de empleados
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeAppointmentsReportDto> GetEmployeeAppointmentsReportAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null)
    {
        var query = _context.Appointments
            .Include(a => a.Employee)
            .Include(a => a.Transaction)
            .Where(a => a.BarberId == barberId);

        // Filtrar por fecha si se proporciona
        if (startDate.HasValue)
        {
            var start = DateOnly.FromDateTime(startDate.Value);
            query = query.Where(a => a.Date >= start);
        }

        if (endDate.HasValue)
        {
            var end = DateOnly.FromDateTime(endDate.Value);
            query = query.Where(a => a.Date <= end);
        }

        // Filtrar por empleado si se proporciona
        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId);
        }

        var appointments = await query.ToListAsync();

        // Obtener ingresos de citas completadas desde Transactions
        var completedAppointmentIds = appointments
            .Where(a => a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.Confirmed)
            .Select(a => a.Id)
            .ToList();

        var appointmentIncomes = await _context.Transactions
            .Where(t => t.BarberId == barberId && 
                       t.Type == TransactionType.Income && 
                       t.AppointmentId.HasValue &&
                       completedAppointmentIds.Contains(t.AppointmentId.Value))
            .GroupBy(t => t.AppointmentId)
            .Select(g => new { AppointmentId = g.Key!.Value, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        var incomeByAppointment = appointmentIncomes.ToDictionary(x => x.AppointmentId, x => x.Total);

        // Agrupar por empleado
        var grouped = appointments
            .GroupBy(a => new { a.EmployeeId, EmployeeName = a.Employee != null ? a.Employee.Name : "Barbero (Dueño)" })
            .ToList();

        var byEmployee = new List<EmployeeAppointmentStatsDto>();
        foreach (var group in grouped)
        {
            var completed = group.Count(a => a.Status == AppointmentStatus.Completed);
            var pending = group.Count(a => a.Status == AppointmentStatus.Pending);
            var confirmed = group.Count(a => a.Status == AppointmentStatus.Confirmed);
            var cancelled = group.Count(a => a.Status == AppointmentStatus.Cancelled);
            var total = group.Count();
            
            var totalIncome = group
                .Where(a => (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.Confirmed) &&
                           incomeByAppointment.ContainsKey(a.Id))
                .Sum(a => incomeByAppointment.GetValueOrDefault(a.Id, 0));
            
            var completedOrConfirmed = group
                .Where(a => a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.Confirmed)
                .ToList();
            
            var averagePerAppointment = completedOrConfirmed.Count > 0
                ? completedOrConfirmed
                    .Where(a => incomeByAppointment.ContainsKey(a.Id))
                    .Select(a => incomeByAppointment.GetValueOrDefault(a.Id, 0))
                    .DefaultIfEmpty(0)
                    .Average()
                : 0;

            byEmployee.Add(new EmployeeAppointmentStatsDto
            {
                EmployeeId = group.Key.EmployeeId,
                EmployeeName = group.Key.EmployeeName,
                Completed = completed,
                Pending = pending,
                Confirmed = confirmed,
                Cancelled = cancelled,
                Total = total,
                TotalIncome = totalIncome,
                AveragePerAppointment = averagePerAppointment
            });
        }

        byEmployee = byEmployee.OrderByDescending(e => e.TotalIncome).ToList();

        return new EmployeeAppointmentsReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalAppointments = appointments.Count,
            ByEmployee = byEmployee
        };
    }

    public async Task<EmployeeIncomeReportDto> GetEmployeeIncomeReportAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null)
    {
        var query = _context.Transactions
            .Include(t => t.Employee)
            .Where(t => t.BarberId == barberId && t.Type == TransactionType.Income);

        // Filtrar por fecha si se proporciona
        if (startDate.HasValue)
        {
            query = query.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.Date <= endDate.Value);
        }

        // Filtrar por empleado si se proporciona
        if (employeeId.HasValue)
        {
            query = query.Where(t => t.EmployeeId == employeeId);
        }

        var transactions = await query.ToListAsync();

        // Agrupar por empleado
        var grouped = transactions
            .GroupBy(t => new { t.EmployeeId, EmployeeName = t.Employee != null ? t.Employee.Name : "Barbero (Dueño)" })
            .ToList();

        var byEmployee = new List<EmployeeIncomeStatsDto>();
        foreach (var group in grouped)
        {
            var totalIncome = group.Sum(t => t.Amount);
            var count = group.Count();
            var fromAppointments = group.Where(t => t.AppointmentId.HasValue).Sum(t => t.Amount);
            var manual = group.Where(t => !t.AppointmentId.HasValue).Sum(t => t.Amount);
            var averagePerTransaction = count > 0 ? group.Average(t => t.Amount) : 0;

            byEmployee.Add(new EmployeeIncomeStatsDto
            {
                EmployeeId = group.Key.EmployeeId,
                EmployeeName = group.Key.EmployeeName,
                TotalIncome = totalIncome,
                Count = count,
                FromAppointments = fromAppointments,
                Manual = manual,
                AveragePerTransaction = averagePerTransaction
            });
        }

        byEmployee = byEmployee.OrderByDescending(e => e.TotalIncome).ToList();

        return new EmployeeIncomeReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalIncome = transactions.Sum(t => t.Amount),
            ByEmployee = byEmployee
        };
    }

    public async Task<EmployeeExpensesReportDto> GetEmployeeExpensesReportAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null)
    {
        var query = _context.Transactions
            .Include(t => t.Employee)
            .Where(t => t.BarberId == barberId && t.Type == TransactionType.Expense);

        // Filtrar por fecha si se proporciona
        if (startDate.HasValue)
        {
            query = query.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.Date <= endDate.Value);
        }

        // Filtrar por empleado si se proporciona
        if (employeeId.HasValue)
        {
            query = query.Where(t => t.EmployeeId == employeeId);
        }

        var transactions = await query.ToListAsync();

        // Agrupar por empleado
        var grouped = transactions
            .GroupBy(t => new { t.EmployeeId, EmployeeName = t.Employee != null ? t.Employee.Name : "Barbero (Dueño)" })
            .ToList();

        var byEmployee = new List<EmployeeExpenseStatsDto>();
        foreach (var group in grouped)
        {
            var categories = group
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .GroupBy(t => t.Category!)
                .ToDictionary(cat => cat.Key, cat => cat.Sum(t => t.Amount));

            var totalExpenses = group.Sum(t => t.Amount);
            var count = group.Count();
            var averagePerTransaction = count > 0 ? group.Average(t => t.Amount) : 0;

            byEmployee.Add(new EmployeeExpenseStatsDto
            {
                EmployeeId = group.Key.EmployeeId,
                EmployeeName = group.Key.EmployeeName,
                TotalExpenses = totalExpenses,
                Count = count,
                Categories = categories,
                AveragePerTransaction = averagePerTransaction
            });
        }

        byEmployee = byEmployee.OrderByDescending(e => e.TotalExpenses).ToList();

        return new EmployeeExpensesReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalExpenses = transactions.Sum(t => t.Amount),
            ByEmployee = byEmployee
        };
    }

    public async Task<EmployeeActivityReportDto> GetEmployeeActivityReportAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Obtener todos los empleados del salón
        var employees = await _context.Employees
            .Include(e => e.User)
            .Where(e => e.OwnerBarberId == barberId)
            .ToListAsync();

        var report = new EmployeeActivityReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Employees = new List<EmployeeActivityStatsDto>()
        };

        // Agregar estadísticas del salón (sin employeeId)
        var barberAppointmentsQuery = _context.Appointments
            .Where(a => a.BarberId == barberId && a.EmployeeId == null);

        var barberTransactionsQuery = _context.Transactions
            .Where(t => t.BarberId == barberId && t.EmployeeId == null);

        if (startDate.HasValue)
        {
            var start = DateOnly.FromDateTime(startDate.Value);
            barberAppointmentsQuery = barberAppointmentsQuery.Where(a => a.Date >= start);
            barberTransactionsQuery = barberTransactionsQuery.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var end = DateOnly.FromDateTime(endDate.Value);
            barberAppointmentsQuery = barberAppointmentsQuery.Where(a => a.Date <= end);
            barberTransactionsQuery = barberTransactionsQuery.Where(t => t.Date <= endDate.Value);
        }

        var barberAppointments = await barberAppointmentsQuery.ToListAsync();
        var barberTransactions = await barberTransactionsQuery.ToListAsync();

        var barberIncome = barberTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var barberExpenses = barberTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var barberCompleted = barberAppointments.Count(a => a.Status == AppointmentStatus.Completed);
        var barberPending = barberAppointments.Count(a => a.Status == AppointmentStatus.Pending);

        var barberLastActivity = barberAppointments
            .OrderByDescending(a => a.UpdatedAt)
            .FirstOrDefault()?.UpdatedAt;

        report.Employees.Add(new EmployeeActivityStatsDto
        {
            EmployeeId = null,
            EmployeeName = "Barbero (Dueño)",
            Email = "",
            IsActive = true,
            AppointmentsCompleted = barberCompleted,
            AppointmentsPending = barberPending,
            TotalIncome = barberIncome,
            TotalExpenses = barberExpenses,
            NetContribution = barberIncome - barberExpenses,
            AveragePerAppointment = barberCompleted > 0 ? barberIncome / barberCompleted : 0,
            LastActivity = barberLastActivity
        });

        // Agregar estadísticas de cada empleado
        foreach (var employee in employees)
        {
            var employeeAppointmentsQuery = _context.Appointments
                .Where(a => a.BarberId == barberId && a.EmployeeId == employee.Id);

            var employeeTransactionsQuery = _context.Transactions
                .Where(t => t.BarberId == barberId && t.EmployeeId == employee.Id);

            if (startDate.HasValue)
            {
                var start = DateOnly.FromDateTime(startDate.Value);
                employeeAppointmentsQuery = employeeAppointmentsQuery.Where(a => a.Date >= start);
                employeeTransactionsQuery = employeeTransactionsQuery.Where(t => t.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var end = DateOnly.FromDateTime(endDate.Value);
                employeeAppointmentsQuery = employeeAppointmentsQuery.Where(a => a.Date <= end);
                employeeTransactionsQuery = employeeTransactionsQuery.Where(t => t.Date <= endDate.Value);
            }

            var employeeAppointments = await employeeAppointmentsQuery.ToListAsync();
            var employeeTransactions = await employeeTransactionsQuery.ToListAsync();

            var employeeIncome = employeeTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var employeeExpenses = employeeTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var employeeCompleted = employeeAppointments.Count(a => a.Status == AppointmentStatus.Completed);
            var employeePending = employeeAppointments.Count(a => a.Status == AppointmentStatus.Pending);

            var employeeLastActivity = employeeAppointments
                .OrderByDescending(a => a.UpdatedAt)
                .FirstOrDefault()?.UpdatedAt;

            report.Employees.Add(new EmployeeActivityStatsDto
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                Email = employee.User.Email,
                IsActive = employee.IsActive,
                AppointmentsCompleted = employeeCompleted,
                AppointmentsPending = employeePending,
                TotalIncome = employeeIncome,
                TotalExpenses = employeeExpenses,
                NetContribution = employeeIncome - employeeExpenses,
                AveragePerAppointment = employeeCompleted > 0 ? employeeIncome / employeeCompleted : 0,
                LastActivity = employeeLastActivity
            });
        }

        // Ordenar por contribución neta descendente
        report.Employees = report.Employees.OrderByDescending(e => e.NetContribution).ToList();

        return report;
    }

    public async Task<EmployeeStatsDto> GetEmployeeStatsForDashboardAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var employees = await _context.Employees
            .Where(e => e.OwnerBarberId == barberId)
            .ToListAsync();

        var totalEmployees = employees.Count;
        var activeEmployees = employees.Count(e => e.IsActive);

        // Obtener top 3 empleados por ingresos
        var incomeQuery = _context.Transactions
            .Include(t => t.Employee)
            .Where(t => t.BarberId == barberId && 
                       t.Type == TransactionType.Income && 
                       t.EmployeeId.HasValue);

        if (startDate.HasValue)
        {
            incomeQuery = incomeQuery.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            incomeQuery = incomeQuery.Where(t => t.Date <= endDate.Value);
        }

        var topPerformers = await incomeQuery
            .GroupBy(t => new { t.EmployeeId, EmployeeName = t.Employee!.Name })
            .Select(g => new EmployeePerformanceDto
            {
                EmployeeId = g.Key.EmployeeId!.Value,
                EmployeeName = g.Key.EmployeeName,
                AppointmentsCompleted = _context.Appointments
                    .Count(a => a.BarberId == barberId && 
                               a.EmployeeId == g.Key.EmployeeId && 
                               a.Status == AppointmentStatus.Completed),
                TotalIncome = g.Sum(t => t.Amount),
                AveragePerAppointment = 0 // Se calculará después
            })
            .OrderByDescending(e => e.TotalIncome)
            .Take(3)
            .ToListAsync();

        // Calcular promedio por cita
        foreach (var performer in topPerformers)
        {
            performer.AveragePerAppointment = performer.AppointmentsCompleted > 0
                ? performer.TotalIncome / performer.AppointmentsCompleted
                : 0;
        }

        return new EmployeeStatsDto
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            TopPerformers = topPerformers
        };
    }
}

