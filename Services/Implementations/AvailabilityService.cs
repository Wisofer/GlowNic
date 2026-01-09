using GlowNic.Data;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para calcular disponibilidad de horarios
/// </summary>
public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _context;
    private readonly IBarberService _barberService;

    public AvailabilityService(ApplicationDbContext context, IBarberService barberService)
    {
        _context = context;
        _barberService = barberService;
    }

    public async Task<AvailabilityResponse> GetAvailabilityAsync(string barberSlug, DateOnly date)
    {
        var barber = await _barberService.GetBarberBySlugAsync(barberSlug);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        var slots = await GetAvailableTimeSlotsAsync(barber.Id, date, 30); // Default 30 minutos

        return new AvailabilityResponse
        {
            Date = date,
            AvailableSlots = slots
        };
    }

    public async Task<List<TimeSlotDto>> GetAvailableTimeSlotsAsync(int barberId, DateOnly date, int serviceDurationMinutes)
    {
        var dayOfWeek = date.DayOfWeek;
        var workingHours = await _context.WorkingHours
            .FirstOrDefaultAsync(wh => wh.BarberId == barberId && wh.DayOfWeek == dayOfWeek && wh.IsActive);

        if (workingHours == null)
            return new List<TimeSlotDto>();

        var slots = new List<TimeSlotDto>();
        var currentTime = workingHours.StartTime;
        var slotDuration = TimeSpan.FromMinutes(serviceDurationMinutes);

        // Obtener citas existentes del día
        var appointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId &&
                       a.Date == date &&
                       a.Status != AppointmentStatus.Cancelled)
            .ToListAsync();

        // Obtener bloqueos del día
        var blockedTimes = await _context.BlockedTimes
            .Where(bt => bt.BarberId == barberId && bt.Date == date)
            .ToListAsync();

        // Generar slots cada 30 minutos
        // El slot completo debe estar dentro del horario de trabajo
        // No generar slots que terminen después del EndTime
        while (currentTime < workingHours.EndTime)
        {
            var endTime = currentTime.Add(slotDuration);
            
            // Si el slot termina después del horario de trabajo, no incluirlo
            if (endTime > workingHours.EndTime)
                break;
            
            var isAvailable = true;

            // Verificar si está bloqueado
            var isBlocked = blockedTimes.Any(bt =>
                (currentTime >= bt.StartTime && currentTime < bt.EndTime) ||
                (endTime > bt.StartTime && endTime <= bt.EndTime) ||
                (currentTime <= bt.StartTime && endTime >= bt.EndTime));

            if (isBlocked)
            {
                isAvailable = false;
            }
            else
            {
                // Verificar si hay conflicto con citas existentes
                var hasConflict = appointments.Any(a =>
                {
                    // Si la cita no tiene servicio, usar duración por defecto de 30 minutos
                    var appointmentDuration = a.Service?.DurationMinutes ?? 30;
                    var appointmentEndTime = a.Time.AddMinutes(appointmentDuration);
                    return (a.Time <= currentTime && appointmentEndTime > currentTime) ||
                           (currentTime <= a.Time && endTime > a.Time);
                });

                if (hasConflict)
                    isAvailable = false;
            }

            slots.Add(new TimeSlotDto
            {
                StartTime = currentTime,
                EndTime = endTime,
                IsAvailable = isAvailable
            });

            // Avanzar 30 minutos
            currentTime = currentTime.AddMinutes(30);
        }

        return slots;
    }
}

