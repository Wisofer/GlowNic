using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de servicios del salón
/// </summary>
public interface IServiceService
{
    Task<List<ServiceDto>> GetBarberServicesAsync(int barberId);
    Task<ServiceDto> CreateServiceAsync(int barberId, CreateServiceRequest request);
    Task<ServiceDto?> GetServiceByIdAsync(int id);
    Task<bool> UpdateServiceAsync(int barberId, int id, CreateServiceRequest request);
    Task<bool> DeleteServiceAsync(int barberId, int id);
}

