using GlowNic.Models.DTOs.Responses;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de dashboards
/// </summary>
public interface IDashboardService
{
    Task<BarberDashboardDto> GetBarberDashboardAsync(int barberId);
    Task<AdminDashboardDto> GetAdminDashboardAsync();
}

