using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de salóns
/// </summary>
public interface IBarberService
{
    Task<Barber?> GetBarberBySlugAsync(string slug);
    Task<Barber?> GetBarberByIdAsync(int id);
    Task<Barber?> GetBarberByUserIdAsync(int userId);
    Task<BarberPublicDto> GetPublicBarberInfoAsync(string slug);
    Task<BarberDto> GetBarberProfileAsync(int barberId);
    Task<BarberDto> UpdateBarberProfileAsync(int barberId, UpdateBarberProfileRequest request);
    Task<string> GetQrUrlAsync(int barberId);
    Task<List<BarberDto>> GetAllBarbersAsync(bool? isActive = null);
    Task<bool> UpdateBarberStatusAsync(int id, bool isActive);
    Task<bool> DeleteBarberAsync(int id);
    Task<BarberDto> CreateBarberAsync(CreateBarberRequest request);
}

