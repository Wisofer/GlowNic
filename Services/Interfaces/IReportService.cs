using GlowNic.Models.DTOs.Responses;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de reportes
/// </summary>
public interface IReportService
{
    Task<EmployeeAppointmentsReportDto> GetEmployeeAppointmentsReportAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null);
    Task<EmployeeIncomeReportDto> GetEmployeeIncomeReportAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null);
    Task<EmployeeExpensesReportDto> GetEmployeeExpensesReportAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null);
    Task<EmployeeActivityReportDto> GetEmployeeActivityReportAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null);
    Task<EmployeeStatsDto> GetEmployeeStatsForDashboardAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null);
}


