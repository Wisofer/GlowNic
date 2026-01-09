using GlowNic.Data;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para dashboards
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IBarberService _barberService;
    private readonly IFinanceService _financeService;
    private readonly IReportService _reportService;

    public DashboardService(ApplicationDbContext context, IBarberService barberService, IFinanceService financeService, IReportService reportService)
    {
        _context = context;
        _barberService = barberService;
        _financeService = financeService;
        _reportService = reportService;
    }

    public async Task<BarberDashboardDto> GetBarberDashboardAsync(int barberId)
    {
        var barber = await _barberService.GetBarberProfileAsync(barberId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // Estadísticas del día
        var todayAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId && a.Date == today)
            .ToListAsync();

        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

        // Obtener ingresos y egresos del día desde transacciones para ser consistente con semana y mes
        var todayIncome = await _context.Transactions
            .Where(t => t.BarberId == barberId && 
                       t.Type == TransactionType.Income &&
                       t.Date >= todayStart && 
                       t.Date <= todayEnd)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var todayExpenses = await _context.Transactions
            .Where(t => t.BarberId == barberId && 
                       t.Type == TransactionType.Expense &&
                       t.Date >= todayStart && 
                       t.Date <= todayEnd)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var todayStats = new TodayStatsDto
        {
            Appointments = todayAppointments.Count,
            Completed = todayAppointments.Count(a => a.Status == AppointmentStatus.Completed),
            Pending = todayAppointments.Count(a => a.Status == AppointmentStatus.Pending),
            Income = todayIncome,
            Expenses = todayExpenses,
            Profit = todayIncome - todayExpenses
        };

        // Estadísticas de la semana
        var weekStart = startOfWeek.ToDateTime(TimeOnly.MinValue);
        var weekEnd = endOfWeek.ToDateTime(TimeOnly.MaxValue);

        var weekAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId && 
                       a.Date >= startOfWeek && 
                       a.Date <= endOfWeek &&
                       (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Completed))
            .ToListAsync();

        var weekFinance = await _financeService.GetFinanceSummaryAsync(barberId, weekStart, weekEnd);

        // Usar ingresos de transacciones para ser consistente con el mes
        var weekIncome = weekFinance.TotalIncome;
        var weekUniqueClients = weekAppointments
            .Select(a => a.ClientPhone)
            .Distinct()
            .Count();
        var weekAveragePerClient = weekUniqueClients > 0 ? weekIncome / weekUniqueClients : 0;

        var weekStats = new PeriodStatsDto
        {
            Appointments = weekAppointments.Count,
            Income = weekIncome,
            Expenses = weekFinance.TotalExpenses, // Usar TotalExpenses del período, no ExpensesThisMonth
            Profit = weekIncome - weekFinance.TotalExpenses,
            UniqueClients = weekUniqueClients,
            AveragePerClient = weekAveragePerClient
        };

        // Estadísticas del mes
        var monthAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId && 
                       a.Date >= startOfMonth && 
                       a.Date <= endOfMonth &&
                       (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Completed))
            .ToListAsync();

        var monthFinance = await _financeService.GetFinanceSummaryAsync(barberId);

        var monthIncome = monthFinance.IncomeThisMonth;
        var monthUniqueClients = monthAppointments
            .Select(a => a.ClientPhone)
            .Distinct()
            .Count();
        var monthAveragePerClient = monthUniqueClients > 0 ? monthIncome / monthUniqueClients : 0;

        var monthStats = new PeriodStatsDto
        {
            Appointments = monthAppointments.Count,
            Income = monthIncome,
            Expenses = monthFinance.ExpensesThisMonth,
            Profit = monthFinance.ProfitThisMonth,
            UniqueClients = monthUniqueClients,
            AveragePerClient = monthAveragePerClient
        };

        // Citas recientes y próximas
        var recentAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Where(a => a.BarberId == barberId && a.Date < today)
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.Time)
            .Take(5)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                BarberId = a.BarberId,
                BarberName = a.Barber.Name,
                ServiceId = a.ServiceId,
                ServiceName = a.Service != null ? a.Service.Name : null,
                ServicePrice = a.Service != null ? a.Service.Price : null,
                ClientName = a.ClientName,
                ClientPhone = a.ClientPhone,
                Date = a.Date,
                Time = a.Time,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var upcomingAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Where(a => a.BarberId == barberId && a.Date >= today)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Time)
            .Take(5)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                BarberId = a.BarberId,
                BarberName = a.Barber.Name,
                ServiceId = a.ServiceId,
                ServiceName = a.Service != null ? a.Service.Name : null,
                ServicePrice = a.Service != null ? a.Service.Price : null,
                ClientName = a.ClientName,
                ClientPhone = a.ClientPhone,
                Date = a.Date,
                Time = a.Time,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        // Obtener estadísticas de empleados para el mes actual
        var employeeStats = await _reportService.GetEmployeeStatsForDashboardAsync(
            barberId,
            startOfMonth.ToDateTime(TimeOnly.MinValue),
            endOfMonth.ToDateTime(TimeOnly.MaxValue));

        return new BarberDashboardDto
        {
            Barber = barber,
            Today = todayStats,
            ThisWeek = weekStats,
            ThisMonth = monthStats,
            RecentAppointments = recentAppointments,
            UpcomingAppointments = upcomingAppointments,
            EmployeeStats = employeeStats
        };
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var totalBarbers = await _context.Barbers.CountAsync();
        var activeBarbers = await _context.Barbers.CountAsync(b => b.IsActive);
        var inactiveBarbers = totalBarbers - activeBarbers;

        var totalAppointments = await _context.Appointments.CountAsync();
        var pendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending);
        var confirmedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Confirmed);
        var cancelledAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled);

        var totalRevenue = await _context.Transactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        // Obtener salóns con sus datos
        var barbers = await _context.Barbers
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync();

        var recentBarbers = barbers.Select(b => new BarberSummaryDto
        {
            Id = b.Id,
            Name = b.Name ?? "Sin nombre",
            BusinessName = b.BusinessName ?? "",
            Phone = b.Phone ?? "",
            Slug = b.Slug ?? "",
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt,
            TotalAppointments = _context.Appointments.Count(a => a.BarberId == b.Id),
            TotalRevenue = _context.Transactions
                .Where(t => t.BarberId == b.Id && t.Type == TransactionType.Income)
                .Sum(t => (decimal?)t.Amount) ?? 0
        }).ToList();

        return new AdminDashboardDto
        {
            TotalBarbers = totalBarbers,
            ActiveBarbers = activeBarbers,
            InactiveBarbers = inactiveBarbers,
            TotalAppointments = totalAppointments,
            PendingAppointments = pendingAppointments,
            ConfirmedAppointments = confirmedAppointments,
            CancelledAppointments = cancelledAppointments,
            TotalRevenue = totalRevenue,
            RecentBarbers = recentBarbers
        };
    }
}

