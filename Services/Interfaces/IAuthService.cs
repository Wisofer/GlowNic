using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de autenticación
/// </summary>
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResult> LoginWithResultAsync(LoginRequest request);
    Task<LoginResult> RefreshTokenAsync(RefreshTokenRequest request);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}

