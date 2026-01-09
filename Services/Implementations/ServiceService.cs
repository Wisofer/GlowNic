using GlowNic.Data;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para gestión de servicios del salón
/// </summary>
public class ServiceService : IServiceService
{
    private readonly ApplicationDbContext _context;

    public ServiceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceDto>> GetBarberServicesAsync(int barberId)
    {
        return await _context.Services
            .Where(s => s.BarberId == barberId && s.IsActive)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<ServiceDto> CreateServiceAsync(int barberId, CreateServiceRequest request)
    {
        var service = new Models.Entities.Service
        {
            BarberId = barberId,
            Name = request.Name,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes ?? 30, // Default 30 minutos si no se proporciona
            IsActive = true
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive
        };
    }

    public async Task<ServiceDto?> GetServiceByIdAsync(int id)
    {
        return await _context.Services
            .Where(s => s.Id == id)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateServiceAsync(int barberId, int id, CreateServiceRequest request)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == id && s.BarberId == barberId);
        
        if (service == null)
            return false;

        service.Name = request.Name;
        service.Price = request.Price;
        service.DurationMinutes = request.DurationMinutes ?? 30; // Default 30 minutos si no se proporciona

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteServiceAsync(int barberId, int id)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == id && s.BarberId == barberId);
        
        if (service == null)
            return false;

        // Soft delete
        service.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }
}

