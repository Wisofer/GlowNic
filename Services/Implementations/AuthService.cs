using GlowNic.Data;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using GlowNic.Utils;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio de autenticación con JWT
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return null;

        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        // Cargar salón si existe
        if (user.Role == UserRole.Barber)
        {
            await _context.Entry(user)
                .Reference(u => u.Barber)
                .LoadAsync();
            
            // Validar que el salón existe y está activo
            if (user.Barber == null)
            {
                // El usuario tiene rol Barber pero no tiene salón asociado (fue eliminado)
                return null;
            }
            
            if (!user.Barber.IsActive)
            {
                // El salón está desactivado
                return null;
            }
        }

        // Cargar trabajador si existe
        if (user.Role == UserRole.Employee)
        {
            await _context.Entry(user)
                .Reference(u => u.Employee)
                .LoadAsync();
            
            // Validar que el empleado existe y está activo
            if (user.Employee == null)
            {
                // El usuario tiene rol Employee pero no tiene empleado asociado (fue eliminado)
                return null;
            }
            
            if (!user.Employee.IsActive)
            {
                // El empleado está desactivado
                return null;
            }
            
            // Cargar también el salón dueño
            await _context.Entry(user.Employee)
                .Reference(e => e.OwnerBarber)
                .LoadAsync();
            
            // Validar que el salón dueño existe y está activo
            if (user.Employee.OwnerBarber == null || !user.Employee.OwnerBarber.IsActive)
            {
                // El salón dueño no existe o está desactivado
                return null;
            }
        }

        // Generar tokens JWT
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurado");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "GlowNic";
        var audience = _configuration["JwtSettings:Audience"] ?? "GlowNicUsers";
        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60");
        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"] ?? "30");

        var token = JwtHelper.GenerateToken(user, secretKey, issuer, audience, expirationMinutes);
        var refreshToken = JwtHelper.GenerateRefreshToken(user, secretKey, issuer, audience, refreshTokenExpirationDays);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Barber = user.Barber != null ? new BarberDto
                {
                    Id = user.Barber.Id,
                    Name = user.Barber.Name,
                    BusinessName = user.Barber.BusinessName,
                    Phone = user.Barber.Phone,
                    Slug = user.Barber.Slug,
                    IsActive = user.Barber.IsActive,
                    QrUrl = QrHelper.GenerateBarberUrl(user.Barber.Slug, _configuration),
                    CreatedAt = user.Barber.CreatedAt
                } : null
            },
            Role = user.Role.ToString()
        };
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.Barber)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Barber)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
            return false;

        // Verificar contraseña actual
        if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
            return false;

        // Actualizar contraseña
        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<LoginResult> LoginWithResultAsync(LoginRequest request)
    {
        var user = await GetUserByEmailAsync(request.Email);
        
        // Usuario no existe
        if (user == null)
        {
            return LoginResult.ErrorResult("Credenciales inválidas", LoginErrorType.InvalidCredentials);
        }

        // Usuario inactivo
        if (!user.IsActive)
        {
            return LoginResult.ErrorResult("Tu cuenta está desactivada. Contacta al administrador.", LoginErrorType.UserInactive);
        }

        // Contraseña incorrecta
        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            return LoginResult.ErrorResult("Credenciales inválidas", LoginErrorType.InvalidCredentials);
        }

        // Validar salón
        if (user.Role == UserRole.Barber)
        {
            await _context.Entry(user)
                .Reference(u => u.Barber)
                .LoadAsync();
            
            if (user.Barber == null)
            {
                return LoginResult.ErrorResult("Tu cuenta de salón fue eliminada del sistema.", LoginErrorType.BarberDeleted);
            }
            
            if (!user.Barber.IsActive)
            {
                return LoginResult.ErrorResult("Tu cuenta está desactivada. Contacta al administrador para reactivarla.", LoginErrorType.BarberInactive);
            }
        }

        // Validar empleado
        if (user.Role == UserRole.Employee)
        {
            await _context.Entry(user)
                .Reference(u => u.Employee)
                .LoadAsync();
            
            if (user.Employee == null)
            {
                return LoginResult.ErrorResult("Tu cuenta de empleado fue eliminada del sistema.", LoginErrorType.EmployeeDeleted);
            }
            
            if (!user.Employee.IsActive)
            {
                return LoginResult.ErrorResult("Tu cuenta está desactivada. Contacta al administrador para reactivarla.", LoginErrorType.EmployeeInactive);
            }
            
            // Cargar también el salón dueño
            await _context.Entry(user.Employee)
                .Reference(e => e.OwnerBarber)
                .LoadAsync();
            
            if (user.Employee.OwnerBarber == null)
            {
                return LoginResult.ErrorResult("El salón dueño fue eliminado del sistema.", LoginErrorType.OwnerBarberDeleted);
            }
            
            if (!user.Employee.OwnerBarber.IsActive)
            {
                return LoginResult.ErrorResult("El salón dueño está desactivado. Tu cuenta no puede acceder hasta que sea reactivado.", LoginErrorType.OwnerBarberInactive);
            }
        }

        // Generar tokens JWT
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurado");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "GlowNic";
        var audience = _configuration["JwtSettings:Audience"] ?? "GlowNicUsers";
        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60");
        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"] ?? "30");

        var token = JwtHelper.GenerateToken(user, secretKey, issuer, audience, expirationMinutes);
        var refreshToken = JwtHelper.GenerateRefreshToken(user, secretKey, issuer, audience, refreshTokenExpirationDays);

        var response = new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Barber = user.Barber != null ? new BarberDto
                {
                    Id = user.Barber.Id,
                    Name = user.Barber.Name,
                    BusinessName = user.Barber.BusinessName,
                    Phone = user.Barber.Phone,
                    Slug = user.Barber.Slug,
                    IsActive = user.Barber.IsActive,
                    QrUrl = QrHelper.GenerateBarberUrl(user.Barber.Slug, _configuration),
                    CreatedAt = user.Barber.CreatedAt
                } : null
            },
            Role = user.Role.ToString()
        };

        return LoginResult.SuccessResult(response);
    }

    public async Task<LoginResult> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return LoginResult.ErrorResult("Refresh token requerido", LoginErrorType.InvalidCredentials);
        }

        var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurado");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "GlowNic";
        var audience = _configuration["JwtSettings:Audience"] ?? "GlowNicUsers";

        // Validar el refresh token
        var principal = JwtHelper.ValidateRefreshToken(request.RefreshToken, secretKey, issuer, audience);
        if (principal == null)
        {
            return LoginResult.ErrorResult("Refresh token inválido o expirado", LoginErrorType.InvalidCredentials);
        }

        // Obtener el ID del usuario desde el token
        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return LoginResult.ErrorResult("Token inválido", LoginErrorType.InvalidCredentials);
        }

        // Obtener el usuario de la base de datos
        var user = await GetUserByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return LoginResult.ErrorResult("Usuario no encontrado o inactivo", LoginErrorType.UserInactive);
        }

        // Cargar relaciones necesarias
        if (user.Role == UserRole.Barber)
        {
            await _context.Entry(user)
                .Reference(u => u.Barber)
                .LoadAsync();
            
            if (user.Barber == null || !user.Barber.IsActive)
            {
                return LoginResult.ErrorResult("Tu cuenta de salón fue eliminada o está desactivada.", LoginErrorType.BarberDeleted);
            }
        }

        if (user.Role == UserRole.Employee)
        {
            await _context.Entry(user)
                .Reference(u => u.Employee)
                .LoadAsync();
            
            if (user.Employee == null || !user.Employee.IsActive)
            {
                return LoginResult.ErrorResult("Tu cuenta de empleado fue eliminada o está desactivada.", LoginErrorType.EmployeeDeleted);
            }
            
            await _context.Entry(user.Employee)
                .Reference(e => e.OwnerBarber)
                .LoadAsync();
            
            if (user.Employee.OwnerBarber == null || !user.Employee.OwnerBarber.IsActive)
            {
                return LoginResult.ErrorResult("El salón dueño fue eliminado o está desactivado.", LoginErrorType.OwnerBarberDeleted);
            }
        }

        // Generar nuevos tokens
        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60");
        var refreshTokenExpirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationInDays"] ?? "30");

        var newToken = JwtHelper.GenerateToken(user, secretKey, issuer, audience, expirationMinutes);
        var newRefreshToken = JwtHelper.GenerateRefreshToken(user, secretKey, issuer, audience, refreshTokenExpirationDays);

        var response = new LoginResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Barber = user.Barber != null ? new BarberDto
                {
                    Id = user.Barber.Id,
                    Name = user.Barber.Name,
                    BusinessName = user.Barber.BusinessName,
                    Phone = user.Barber.Phone,
                    Slug = user.Barber.Slug,
                    IsActive = user.Barber.IsActive,
                    QrUrl = QrHelper.GenerateBarberUrl(user.Barber.Slug, _configuration),
                    CreatedAt = user.Barber.CreatedAt
                } : null
            },
            Role = user.Role.ToString()
        };

        return LoginResult.SuccessResult(response);
    }
}

