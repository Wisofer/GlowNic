using GlowNic.Data;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para gestión de citas
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IBarberService _barberService;
    private readonly IAvailabilityService _availabilityService;
    private readonly IFinanceService _financeService;

    public AppointmentService(
        ApplicationDbContext context,
        IBarberService barberService,
        IAvailabilityService availabilityService,
        IFinanceService financeService)
    {
        _context = context;
        _barberService = barberService;
        _availabilityService = availabilityService;
        _financeService = financeService;
    }

    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        // Validar que barberSlug esté presente (para creación pública)
        if (string.IsNullOrEmpty(request.BarberSlug))
            throw new InvalidOperationException("El slug del salón es requerido para creación pública");

        // Validar que el salón existe
        var barber = await _barberService.GetBarberBySlugAsync(request.BarberSlug);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        // Validar servicios si se proporcionan
        List<Service> selectedServices = new List<Service>();
        int durationMinutes = 30; // Duración por defecto si no hay servicios
        
        if (request.ServiceIds != null && request.ServiceIds.Length > 0)
        {
            // Validar que todos los servicios pertenezcan al salón y estén activos
            selectedServices = await _context.Services
                .Where(s => request.ServiceIds.Contains(s.Id) && s.BarberId == barber.Id && s.IsActive)
                .ToListAsync();
            
            if (selectedServices.Count != request.ServiceIds.Length)
                throw new KeyNotFoundException("Uno o más servicios no encontrados o no pertenecen al salón");
            
            // Calcular duración total sumando todos los servicios
            durationMinutes = selectedServices.Sum(s => s.DurationMinutes);
        }

        // Validar disponibilidad (usar duración total de servicios o 30 min por defecto)
        var isAvailable = await ValidateAppointmentAvailabilityAsync(
            barber.Id, request.Date, request.Time, durationMinutes);
        if (!isAvailable)
            throw new InvalidOperationException("El horario no está disponible");

        // Validar que no sea en el pasado
        var appointmentDateTime = request.Date.ToDateTime(request.Time);
        if (appointmentDateTime < DateTime.Now)
            throw new InvalidOperationException("No se pueden crear citas en el pasado");

        // Crear la cita (ServiceId será el primero o null)
        var appointment = new Appointment
        {
            BarberId = barber.Id,
            ServiceId = selectedServices.FirstOrDefault()?.Id, // Primer servicio o null
            ClientName = request.ClientName,
            ClientPhone = request.ClientPhone,
            Date = request.Date,
            Time = request.Time,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Guardar los servicios seleccionados en la tabla intermedia
        if (selectedServices.Count > 0)
        {
            foreach (var service in selectedServices)
            {
                var appointmentService = new AppointmentServiceEntity
                {
                    AppointmentId = appointment.Id,
                    ServiceId = service.Id
                };
                _context.AppointmentServices.Add(appointmentService);
            }
            await _context.SaveChangesAsync();
        }

        return await GetAppointmentByIdAsync(appointment.Id) ?? throw new Exception("Error al crear la cita");
    }

    public async Task<List<AppointmentDto>> GetBarberAppointmentsAsync(int barberId, DateOnly? date = null, AppointmentStatus? status = null)
    {
        var query = _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Include(a => a.Employee)
            .Where(a => a.BarberId == barberId);

        if (date.HasValue)
            query = query.Where(a => a.Date == date.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var appointments = await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Time)
            .ToListAsync();

        // Obtener todos los servicios para cada cita desde la tabla intermedia
        var appointmentIds = appointments.Select(a => a.Id).ToList();
        var appointmentServices = await _context.AppointmentServices
            .Include(aps => aps.Service)
            .Where(aps => appointmentIds.Contains(aps.AppointmentId))
            .ToListAsync();

        return appointments.Select(a =>
        {
            var services = appointmentServices
                .Where(aps => aps.AppointmentId == a.Id)
                .Select(aps => new ServiceDto
                {
                    Id = aps.Service.Id,
                    Name = aps.Service.Name,
                    Price = aps.Service.Price,
                    DurationMinutes = aps.Service.DurationMinutes,
                    IsActive = aps.Service.IsActive
                })
                .ToList();

            return new AppointmentDto
            {
                Id = a.Id,
                BarberId = a.BarberId,
                BarberName = a.Barber.Name,
                EmployeeId = a.EmployeeId,
                EmployeeName = a.Employee != null ? a.Employee.Name : null,
                ServiceId = a.ServiceId, // Primer servicio (compatibilidad)
                ServiceName = a.Service != null ? a.Service.Name : null,
                ServicePrice = a.Service != null ? a.Service.Price : null,
                Services = services, // Todos los servicios
                ClientName = a.ClientName,
                ClientPhone = a.ClientPhone,
                Date = a.Date,
                Time = a.Time,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            };
        }).ToList();
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return null;

        // Obtener todos los servicios desde la tabla intermedia
        var services = await _context.AppointmentServices
            .Include(aps => aps.Service)
            .Where(aps => aps.AppointmentId == id)
            .Select(aps => new ServiceDto
            {
                Id = aps.Service.Id,
                Name = aps.Service.Name,
                Price = aps.Service.Price,
                DurationMinutes = aps.Service.DurationMinutes,
                IsActive = aps.Service.IsActive
            })
            .ToListAsync();

        return new AppointmentDto
        {
            Id = appointment.Id,
            BarberId = appointment.BarberId,
            BarberName = appointment.Barber.Name,
            EmployeeId = appointment.EmployeeId,
            EmployeeName = appointment.Employee != null ? appointment.Employee.Name : null,
            ServiceId = appointment.ServiceId, // Primer servicio (compatibilidad)
            ServiceName = appointment.Service != null ? appointment.Service.Name : null,
            ServicePrice = appointment.Service != null ? appointment.Service.Price : null,
            Services = services, // Todos los servicios
            ClientName = appointment.ClientName,
            ClientPhone = appointment.ClientPhone,
            Date = appointment.Date,
            Time = appointment.Time,
            Status = appointment.Status.ToString(),
            CreatedAt = appointment.CreatedAt
        };
    }

    public async Task<AppointmentDto> UpdateAppointmentAsync(int id, UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
            throw new KeyNotFoundException("Cita no encontrada");

        // Actualizar servicio si se proporciona
        Service? service = null;
        if (request.ServiceId.HasValue)
        {
            service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId.Value && s.BarberId == appointment.BarberId && s.IsActive);
            if (service == null)
                throw new KeyNotFoundException("Servicio no encontrado");
            appointment.ServiceId = request.ServiceId.Value;
        }

        // Obtener el servicio actualizado (el que se asignó o el que ya tenía)
        service = service ?? appointment.Service;

        // Si cambia el estado a Completed, crear ingresos automáticamente
        // SOLO se crean ingresos cuando se completa la cita (no al confirmar)
        if (request.Status.HasValue && 
            request.Status.Value == AppointmentStatus.Completed && 
            appointment.Status != AppointmentStatus.Completed)
        {
            // Guardar cambios antes de verificar AppointmentServices
            await _context.SaveChangesAsync();

            // Obtener todos los servicios asociados a esta cita desde la tabla intermedia
            var appointmentServices = await _context.AppointmentServices
                .Include(aps => aps.Service)
                .Where(aps => aps.AppointmentId == appointment.Id)
                .ToListAsync();

            if (appointmentServices.Count > 0)
            {
                // Crear múltiples ingresos (uno por cada servicio)
                var servicesList = appointmentServices
                    .Select(aps => (aps.ServiceId, aps.Service.Name, aps.Service.Price))
                    .ToList();
                
                await _financeService.CreateMultipleIncomesFromAppointmentAsync(
                    appointment.BarberId,
                    appointment.Id,
                    servicesList,
                    appointment.ClientName);
            }
            else if (service != null)
            {
                // Fallback: si no hay servicios en la tabla intermedia pero hay ServiceId, crear un ingreso
                await _financeService.CreateIncomeFromAppointmentAsync(
                    appointment.BarberId,
                    appointment.Id,
                    service.Price,
                    $"Cita - {service.Name} - {appointment.ClientName}");
            }
            // Si no hay servicios ni ServiceId, no se crea ingreso automático
        }

        // Actualizar campos
        if (request.Status.HasValue)
            appointment.Status = (AppointmentStatus)request.Status.Value;

        if (request.Date.HasValue)
            appointment.Date = request.Date.Value;

        if (request.Time.HasValue)
            appointment.Time = request.Time.Value;

        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(id) ?? throw new Exception("Error al actualizar la cita");
    }

    public async Task<AppointmentDto> UpdateAppointmentForBarberAsync(int barberId, int appointmentId, UpdateAppointmentRequest request, int? employeeId = null)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.BarberId == barberId);
        
        if (appointment == null)
            throw new KeyNotFoundException("Cita no encontrada o no pertenece al salón");

        // Si se proporciona employeeId y la cita no tiene trabajador asignado, asignarlo automáticamente
        // Esto permite que un trabajador acepte una cita pendiente
        if (employeeId.HasValue && appointment.EmployeeId == null)
        {
            appointment.EmployeeId = employeeId.Value;
        }

        // Asignar múltiples servicios si se proporcionan (prioridad sobre ServiceId único)
        if (request.ServiceIds != null && request.ServiceIds.Length > 0)
        {
            // Validar que todos los servicios pertenezcan al salón y estén activos
            var selectedServices = await _context.Services
                .Where(s => request.ServiceIds.Contains(s.Id) && s.BarberId == barberId && s.IsActive)
                .ToListAsync();
            
            if (selectedServices.Count != request.ServiceIds.Length)
                throw new KeyNotFoundException("Uno o más servicios no encontrados o no pertenecen al salón");

            // Eliminar servicios existentes en AppointmentServices para esta cita
            var existingAppointmentServices = await _context.AppointmentServices
                .Where(aps => aps.AppointmentId == appointment.Id)
                .ToListAsync();
            _context.AppointmentServices.RemoveRange(existingAppointmentServices);

            // Agregar los nuevos servicios a AppointmentServices
            foreach (var selectedService in selectedServices)
            {
                var appointmentService = new AppointmentServiceEntity
                {
                    AppointmentId = appointment.Id,
                    ServiceId = selectedService.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AppointmentServices.Add(appointmentService);
            }

            // Actualizar ServiceId con el primer servicio (para compatibilidad)
            appointment.ServiceId = selectedServices.FirstOrDefault()?.Id;
        }
        // Actualizar servicio único si se proporciona (legacy, solo si no hay ServiceIds)
        else if (request.ServiceId.HasValue)
        {
            var singleService = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId.Value && s.BarberId == barberId && s.IsActive);
            if (singleService == null)
                throw new KeyNotFoundException("Servicio no encontrado");
            appointment.ServiceId = request.ServiceId.Value;
        }

        // Obtener el servicio actualizado (el que se asignó o el que ya tenía)
        var currentService = appointment.Service;

        // Si cambia el estado a Completed, crear ingresos automáticamente
        // SOLO se crean ingresos cuando se completa la cita (no al confirmar)
        // Esto permite crear ingresos al completar desde cualquier estado anterior (Pending, Confirmed, etc.)
        if (request.Status.HasValue && 
            request.Status.Value == AppointmentStatus.Completed && 
            appointment.Status != AppointmentStatus.Completed)
        {
            // Guardar cambios antes de verificar AppointmentServices
            // Esto asegura que los servicios agregados en ServiceIds estén guardados
            await _context.SaveChangesAsync();

            // Obtener todos los servicios asociados a esta cita desde la tabla intermedia
            var appointmentServices = await _context.AppointmentServices
                .Include(aps => aps.Service)
                .Where(aps => aps.AppointmentId == appointment.Id)
                .ToListAsync();

            if (appointmentServices.Count > 0)
            {
                // Crear múltiples ingresos (uno por cada servicio)
                var servicesList = appointmentServices
                    .Select(aps => (aps.ServiceId, aps.Service.Name, aps.Service.Price))
                    .ToList();
                
                await _financeService.CreateMultipleIncomesFromAppointmentAsync(
                    appointment.BarberId,
                    appointment.Id,
                    servicesList,
                    appointment.ClientName);
            }
            else if (currentService != null)
            {
                // Fallback: si no hay servicios en la tabla intermedia pero hay ServiceId, crear un ingreso
                await _financeService.CreateIncomeFromAppointmentAsync(
                    appointment.BarberId,
                    appointment.Id,
                    currentService.Price,
                    $"Cita - {currentService.Name} - {appointment.ClientName}");
            }
            // Si no hay servicios ni ServiceId, no se crea ingreso automático
        }

        // Actualizar campos
        if (request.Status.HasValue)
            appointment.Status = (AppointmentStatus)request.Status.Value;

        if (request.Date.HasValue)
            appointment.Date = request.Date.Value;

        if (request.Time.HasValue)
            appointment.Time = request.Time.Value;

        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(appointmentId) ?? throw new Exception("Error al actualizar la cita");
    }

    public async Task<bool> DeleteAppointmentAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return false;

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAppointmentForBarberAsync(int barberId, int appointmentId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.BarberId == barberId);
        
        if (appointment == null)
            return false;

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<AppointmentDto> CreateAppointmentForBarberAsync(int barberId, CreateAppointmentRequest request, int? employeeId = null)
    {
        // Obtener el salón para validar que existe
        var barber = await _barberService.GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        // Validar servicios si se proporcionan
        List<Service> selectedServices = new List<Service>();
        int durationMinutes = 30; // Duración por defecto si no hay servicios
        
        if (request.ServiceIds != null && request.ServiceIds.Length > 0)
        {
            // Validar que todos los servicios pertenezcan al salón y estén activos
            selectedServices = await _context.Services
                .Where(s => request.ServiceIds.Contains(s.Id) && s.BarberId == barberId && s.IsActive)
                .ToListAsync();
            
            if (selectedServices.Count != request.ServiceIds.Length)
                throw new KeyNotFoundException("Uno o más servicios no encontrados o no pertenecen al salón");
            
            // Calcular duración total sumando todos los servicios
            durationMinutes = selectedServices.Sum(s => s.DurationMinutes);
        }

        // Validar disponibilidad (usar duración total de servicios o 30 min por defecto)
        var isAvailable = await ValidateAppointmentAvailabilityAsync(
            barberId, request.Date, request.Time, durationMinutes);
        if (!isAvailable)
            throw new InvalidOperationException("El horario no está disponible");

        // Validar que no sea en el pasado
        var appointmentDateTime = request.Date.ToDateTime(request.Time);
        if (appointmentDateTime < DateTime.Now)
            throw new InvalidOperationException("No se pueden crear citas en el pasado");

        // Crear la cita (ServiceId será el primero o null)
        var appointment = new Appointment
        {
            BarberId = barberId,
            EmployeeId = employeeId, // Opcional: trabajador que crea la cita
            ServiceId = selectedServices.FirstOrDefault()?.Id, // Primer servicio o null
            ClientName = request.ClientName,
            ClientPhone = request.ClientPhone,
            Date = request.Date,
            Time = request.Time,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Guardar los servicios seleccionados en la tabla intermedia
        if (selectedServices.Count > 0)
        {
            foreach (var service in selectedServices)
            {
                var appointmentService = new AppointmentServiceEntity
                {
                    AppointmentId = appointment.Id,
                    ServiceId = service.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AppointmentServices.Add(appointmentService);
            }
            await _context.SaveChangesAsync();
        }

        return await GetAppointmentByIdAsync(appointment.Id) ?? throw new Exception("Error al crear la cita");
    }

    public async Task<bool> ValidateAppointmentAvailabilityAsync(int barberId, DateOnly date, TimeOnly time, int durationMinutes, int? excludeAppointmentId = null)
    {
        // Verificar horarios laborales
        var dayOfWeek = date.DayOfWeek;
        var workingHours = await _context.WorkingHours
            .FirstOrDefaultAsync(wh => wh.BarberId == barberId && wh.DayOfWeek == dayOfWeek && wh.IsActive);

        if (workingHours == null)
            return false;

        // Verificar que el horario esté dentro del rango laboral
        var endTime = time.AddMinutes(durationMinutes);
        if (time < workingHours.StartTime || endTime > workingHours.EndTime)
            return false;

        // Verificar bloqueos temporales
        var isBlocked = await _context.BlockedTimes
            .AnyAsync(bt => bt.BarberId == barberId &&
                           bt.Date == date &&
                           ((time >= bt.StartTime && time < bt.EndTime) ||
                            (endTime > bt.StartTime && endTime <= bt.EndTime) ||
                            (time <= bt.StartTime && endTime >= bt.EndTime)));

        if (isBlocked)
            return false;

        // Verificar que no haya otra cita en el mismo horario
        var existingAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId &&
                       a.Date == date &&
                       a.Id != excludeAppointmentId &&
                       a.Status != AppointmentStatus.Cancelled)
            .ToListAsync();

        var hasConflict = existingAppointments.Any(a =>
        {
            // Si la cita no tiene servicio, usar duración por defecto de 30 minutos
            var appointmentDuration = a.Service?.DurationMinutes ?? 30;
            var appointmentEndTime = a.Time.AddMinutes(appointmentDuration);
            return (a.Time <= time && appointmentEndTime > time) ||
                   (time <= a.Time && endTime > a.Time);
        });

        return !hasConflict;
    }
}

