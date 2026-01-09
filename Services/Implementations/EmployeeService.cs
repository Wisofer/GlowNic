using GlowNic.Data;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using GlowNic.Utils;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para gestión de trabajadores/empleados
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EmployeeDto>> GetEmployeesAsync(int ownerBarberId)
    {
        var employees = await _context.Employees
            .Include(e => e.OwnerBarber)
            .Include(e => e.User)
            .Where(e => e.OwnerBarberId == ownerBarberId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                OwnerBarberId = e.OwnerBarberId,
                OwnerBarberName = e.OwnerBarber.Name,
                Name = e.Name,
                Email = e.User.Email,
                Phone = e.Phone,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();

        return employees;
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId, int ownerBarberId)
    {
        var employee = await _context.Employees
            .Include(e => e.OwnerBarber)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.OwnerBarberId == ownerBarberId);

        if (employee == null)
            return null;

        return new EmployeeDto
        {
            Id = employee.Id,
            OwnerBarberId = employee.OwnerBarberId,
            OwnerBarberName = employee.OwnerBarber.Name,
            Name = employee.Name,
            Email = employee.User.Email,
            Phone = employee.Phone,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(int ownerBarberId, CreateEmployeeRequest request)
    {
        // Verificar que el salón dueño existe
        var ownerBarber = await _context.Barbers.FindAsync(ownerBarberId);
        if (ownerBarber == null)
            throw new KeyNotFoundException("Barbero dueño no encontrado");

        // Verificar que el email no esté en uso
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            throw new InvalidOperationException("El email ya está en uso");

        // Crear usuario con rol Employee
        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            Role = UserRole.Employee,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Crear empleado
        var employee = new Employee
        {
            OwnerBarberId = ownerBarberId,
            UserId = user.Id,
            Name = request.Name,
            Phone = request.Phone,
            IsActive = true
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Cargar relaciones para el DTO
        await _context.Entry(employee)
            .Reference(e => e.OwnerBarber)
            .LoadAsync();
        await _context.Entry(employee)
            .Reference(e => e.User)
            .LoadAsync();

        return new EmployeeDto
        {
            Id = employee.Id,
            OwnerBarberId = employee.OwnerBarberId,
            OwnerBarberName = employee.OwnerBarber.Name,
            Name = employee.Name,
            Email = employee.User.Email,
            Phone = employee.Phone,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<EmployeeDto?> UpdateEmployeeAsync(int employeeId, int ownerBarberId, UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.OwnerBarber)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.OwnerBarberId == ownerBarberId);

        if (employee == null)
            return null;

        employee.Name = request.Name;
        employee.Phone = request.Phone;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        // Actualizar estado del usuario también
        employee.User.IsActive = request.IsActive;
        employee.User.UpdatedAt = DateTime.UtcNow;

        _context.Employees.Update(employee);
        _context.Users.Update(employee.User);
        await _context.SaveChangesAsync();

        return new EmployeeDto
        {
            Id = employee.Id,
            OwnerBarberId = employee.OwnerBarberId,
            OwnerBarberName = employee.OwnerBarber.Name,
            Name = employee.Name,
            Email = employee.User.Email,
            Phone = employee.Phone,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<bool> DeleteEmployeeAsync(int employeeId, int ownerBarberId)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.OwnerBarberId == ownerBarberId);

        if (employee == null)
            return false;

        // Soft delete: desactivar en lugar de eliminar
        employee.IsActive = false;
        employee.User.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.User.UpdatedAt = DateTime.UtcNow;

        _context.Employees.Update(employee);
        _context.Users.Update(employee.User);
        await _context.SaveChangesAsync();

        return true;
    }
}

