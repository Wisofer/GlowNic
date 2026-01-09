using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para gestionar horarios de trabajo
/// </summary>
public class WorkingHoursService : IWorkingHoursService
{
    private readonly Data.ApplicationDbContext _context;
    private readonly ILogger<WorkingHoursService> _logger;

    public WorkingHoursService(
        Data.ApplicationDbContext context,
        ILogger<WorkingHoursService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<WorkingHoursDto>> GetWorkingHoursAsync(int barberId)
    {
        var workingHours = await _context.WorkingHours
            .Where(wh => wh.BarberId == barberId)
            .OrderBy(wh => wh.DayOfWeek)
            .Select(wh => new WorkingHoursDto
            {
                Id = wh.Id,
                DayOfWeek = wh.DayOfWeek,
                StartTime = wh.StartTime,
                EndTime = wh.EndTime,
                IsActive = wh.IsActive
            })
            .ToListAsync();

        return workingHours;
    }

    public async Task<List<WorkingHoursDto>> UpdateWorkingHoursAsync(int barberId, List<UpdateWorkingHoursRequest> workingHours)
    {
        // Validar que el salón existe
        var barberExists = await _context.Barbers.AnyAsync(b => b.Id == barberId);
        if (!barberExists)
            throw new KeyNotFoundException("Barbero no encontrado");

        // Validar horarios
        foreach (var wh in workingHours)
        {
            if (wh.StartTime >= wh.EndTime)
                throw new InvalidOperationException($"La hora de inicio debe ser menor que la hora de fin para {wh.DayOfWeek}");

            // Validar que no haya duplicados en el request
            var duplicates = workingHours.Count(w => w.DayOfWeek == wh.DayOfWeek);
            if (duplicates > 1)
                throw new InvalidOperationException($"No puede haber múltiples horarios para el mismo día ({wh.DayOfWeek})");
        }

        // Obtener horarios existentes del salón
        var existingWorkingHours = await _context.WorkingHours
            .Where(wh => wh.BarberId == barberId)
            .ToListAsync();

        // Procesar cada horario del request
        foreach (var request in workingHours)
        {
            var existing = existingWorkingHours.FirstOrDefault(wh => wh.DayOfWeek == request.DayOfWeek);

            if (existing != null)
            {
                // Actualizar horario existente
                existing.StartTime = request.StartTime;
                existing.EndTime = request.EndTime;
                existing.IsActive = request.IsActive;
                _context.WorkingHours.Update(existing);
            }
            else
            {
                // Crear nuevo horario
                var newWorkingHours = new WorkingHours
                {
                    BarberId = barberId,
                    DayOfWeek = request.DayOfWeek,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    IsActive = request.IsActive
                };
                _context.WorkingHours.Add(newWorkingHours);
            }
        }

        await _context.SaveChangesAsync();

        // Retornar todos los horarios actualizados
        return await GetWorkingHoursAsync(barberId);
    }

    public async Task<bool> DeleteWorkingHoursAsync(int barberId, int workingHoursId)
    {
        var workingHours = await _context.WorkingHours
            .FirstOrDefaultAsync(wh => wh.Id == workingHoursId && wh.BarberId == barberId);

        if (workingHours == null)
            return false;

        _context.WorkingHours.Remove(workingHours);
        await _context.SaveChangesAsync();

        return true;
    }
}

