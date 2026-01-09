using GlowNic.Data;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using GlowNic.Utils;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para gestión de salóns
/// </summary>
public class BarberService : IBarberService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public BarberService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<Barber?> GetBarberBySlugAsync(string slug)
    {
        var barber = await _context.Barbers
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
        
        if (barber != null)
        {
            await _context.Entry(barber)
                .Collection(b => b.Services)
                .Query()
                .Where(s => s.IsActive)
                .LoadAsync();
            
            await _context.Entry(barber)
                .Collection(b => b.WorkingHours)
                .Query()
                .Where(wh => wh.IsActive)
                .LoadAsync();
        }
        
        return barber;
    }

    public async Task<Barber?> GetBarberByIdAsync(int id)
    {
        return await _context.Barbers
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Barber?> GetBarberByUserIdAsync(int userId)
    {
        return await _context.Barbers
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<BarberPublicDto> GetPublicBarberInfoAsync(string slug)
    {
        var barber = await GetBarberBySlugAsync(slug);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        return new BarberPublicDto
        {
            Id = barber.Id,
            Name = barber.Name,
            BusinessName = barber.BusinessName,
            Phone = barber.Phone,
            Slug = barber.Slug,
            Services = barber.Services.Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            }).ToList(),
            WorkingHours = barber.WorkingHours.Select(wh => new WorkingHoursDto
            {
                Id = wh.Id,
                DayOfWeek = wh.DayOfWeek,
                StartTime = wh.StartTime,
                EndTime = wh.EndTime,
                IsActive = wh.IsActive
            }).ToList()
        };
    }

    public async Task<BarberDto> GetBarberProfileAsync(int barberId)
    {
        var barber = await GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        return new BarberDto
        {
            Id = barber.Id,
            Name = barber.Name,
            BusinessName = barber.BusinessName,
            Phone = barber.Phone,
            Slug = barber.Slug,
            IsActive = barber.IsActive,
            QrUrl = QrHelper.GenerateBarberUrl(barber.Slug, _configuration),
            CreatedAt = barber.CreatedAt,
            Email = barber.User?.Email
        };
    }

    public async Task<BarberDto> UpdateBarberProfileAsync(int barberId, UpdateBarberProfileRequest request)
    {
        var barber = await GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        barber.Name = request.Name;
        barber.BusinessName = request.BusinessName;
        barber.Phone = request.Phone;
        barber.UpdatedAt = DateTime.UtcNow;

        // Actualizar contraseña si se proporciona
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var user = await _context.Users.FindAsync(barber.UserId);
            if (user != null)
            {
                user.PasswordHash = PasswordHelper.HashPassword(request.Password);
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return await GetBarberProfileAsync(barberId);
    }

    public async Task<string> GetQrUrlAsync(int barberId)
    {
        var barber = await GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        return QrHelper.GenerateBarberUrl(barber.Slug, _configuration);
    }

    public async Task<List<BarberDto>> GetAllBarbersAsync(bool? isActive = null)
    {
        var query = _context.Barbers.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        var barbers = await query
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.BusinessName,
                b.Phone,
                b.Slug,
                b.IsActive,
                b.CreatedAt
            })
            .ToListAsync();

        return barbers.Select(b => new BarberDto
        {
            Id = b.Id,
            Name = b.Name,
            BusinessName = b.BusinessName,
            Phone = b.Phone,
            Slug = b.Slug,
            IsActive = b.IsActive,
            QrUrl = QrHelper.GenerateBarberUrl(b.Slug, _configuration),
            CreatedAt = b.CreatedAt
        }).ToList();
    }

    public async Task<bool> UpdateBarberStatusAsync(int id, bool isActive)
    {
        var barber = await GetBarberByIdAsync(id);
        if (barber == null)
            return false;

        barber.IsActive = isActive;
        barber.UpdatedAt = DateTime.UtcNow;

        // Si se desactiva el salón, también desactivar todos sus empleados
        if (!isActive)
        {
            var employees = await _context.Employees
                .Where(e => e.OwnerBarberId == id && e.IsActive)
                .ToListAsync();
            
            foreach (var employee in employees)
            {
                employee.IsActive = false;
                employee.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteBarberAsync(int id)
    {
        var barber = await GetBarberByIdAsync(id);
        if (barber == null)
            return false;

        // Obtener el UserId antes de eliminar
        var userId = barber.UserId;

        // 1. Obtener IDs de citas del salón para eliminar relaciones
        var appointmentIds = await _context.Appointments
            .Where(a => a.BarberId == id)
            .Select(a => a.Id)
            .ToListAsync();

        // 2. Eliminar AppointmentServices (relaciones de citas con servicios)
        if (appointmentIds.Any())
        {
            var appointmentServices = await _context.AppointmentServices
                .Where(aps => appointmentIds.Contains(aps.AppointmentId))
                .ToListAsync();
            _context.AppointmentServices.RemoveRange(appointmentServices);
        }

        // 3. Eliminar Transactions (ingresos y egresos)
        var transactions = await _context.Transactions
            .Where(t => t.BarberId == id)
            .ToListAsync();
        _context.Transactions.RemoveRange(transactions);

        // 4. Eliminar Appointments (citas)
        if (appointmentIds.Any())
        {
            var appointments = await _context.Appointments
                .Where(a => a.BarberId == id)
                .ToListAsync();
            _context.Appointments.RemoveRange(appointments);
        }

        // 5. Eliminar Services (servicios)
        var services = await _context.Services
            .Where(s => s.BarberId == id)
            .ToListAsync();
        _context.Services.RemoveRange(services);

        // 6. Eliminar WorkingHours (horarios de trabajo)
        var workingHours = await _context.WorkingHours
            .Where(wh => wh.BarberId == id)
            .ToListAsync();
        _context.WorkingHours.RemoveRange(workingHours);

        // 7. Eliminar BlockedTimes (tiempos bloqueados)
        var blockedTimes = await _context.BlockedTimes
            .Where(bt => bt.BarberId == id)
            .ToListAsync();
        _context.BlockedTimes.RemoveRange(blockedTimes);

        // 8. Eliminar Employees y sus Users asociados
        var employees = await _context.Employees
            .Where(e => e.OwnerBarberId == id)
            .Include(e => e.User)
            .ToListAsync();
        
        foreach (var employee in employees)
        {
            // Eliminar el User del empleado si existe
            if (employee.User != null)
            {
                _context.Users.Remove(employee.User);
            }
            // El Employee se eliminará en cascada, pero lo hacemos explícito
            _context.Employees.Remove(employee);
        }

        // 9. Eliminar el Barber
        _context.Barbers.Remove(barber);

        // 10. Eliminar el User del salón para liberar el email
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
        }

        // Guardar todos los cambios
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<BarberDto> CreateBarberAsync(CreateBarberRequest request)
    {
        // Verificar si el email ya existe
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        
        if (existingUser != null)
            throw new InvalidOperationException("El email ya está registrado");

        // Crear usuario
        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            Role = UserRole.Barber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generar slug único
        var baseSlug = SlugHelper.GenerateSlug(request.Name);
        var slug = baseSlug;
        int counter = 1;
        while (await _context.Barbers.AnyAsync(b => b.Slug == slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        // Crear perfil de salón
        var barber = new Barber
        {
            UserId = user.Id,
            Name = request.Name,
            BusinessName = request.BusinessName,
            Phone = request.Phone,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Barbers.Add(barber);
        await _context.SaveChangesAsync();

        // Crear horarios de trabajo por defecto (Lunes a Viernes, 9:00 - 17:00)
        for (int i = 1; i <= 5; i++) // Lunes a Viernes
        {
            _context.WorkingHours.Add(new WorkingHours
            {
                BarberId = barber.Id,
                DayOfWeek = (DayOfWeek)i,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0),
                IsActive = true
            });
        }
        await _context.SaveChangesAsync();

        return await GetBarberProfileAsync(barber.Id);
    }
}

