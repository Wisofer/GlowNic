using GlowNic.Models.DTOs.Responses;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de disponibilidad
/// </summary>
public interface IAvailabilityService
{
    Task<AvailabilityResponse> GetAvailabilityAsync(string barberSlug, DateOnly date);
    Task<List<TimeSlotDto>> GetAvailableTimeSlotsAsync(int barberId, DateOnly date, int serviceDurationMinutes);
}

