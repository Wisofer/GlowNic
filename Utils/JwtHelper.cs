using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using GlowNic.Models.Entities;

namespace GlowNic.Utils;

/// <summary>
/// Helper para generar y validar tokens JWT
/// </summary>
public static class JwtHelper
{
    /// <summary>
    /// Genera un token JWT para un usuario
    /// </summary>
    public static string GenerateToken(User user, string secretKey, string issuer, string audience, int expirationMinutes)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("UserId", user.Id.ToString())
        };

        // Si es salón, agregar el ID del salón
        if (user.Barber != null)
        {
            claims.Add(new Claim("BarberId", user.Barber.Id.ToString()));
        }

        // Si es trabajador, agregar el ID del trabajador y del salón dueño
        if (user.Employee != null)
        {
            claims.Add(new Claim("EmployeeId", user.Employee.Id.ToString()));
            claims.Add(new Claim("OwnerBarberId", user.Employee.OwnerBarberId.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Obtiene el ID del usuario desde los claims
    /// </summary>
    public static int? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Obtiene el ID del salón desde los claims
    /// </summary>
    public static int? GetBarberId(ClaimsPrincipal user)
    {
        var barberIdClaim = user.FindFirst("BarberId")?.Value;
        return int.TryParse(barberIdClaim, out var barberId) ? barberId : null;
    }

    /// <summary>
    /// Obtiene el rol del usuario desde los claims
    /// </summary>
    public static string GetRole(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Obtiene el ID del trabajador desde los claims
    /// </summary>
    public static int? GetEmployeeId(ClaimsPrincipal user)
    {
        var employeeIdClaim = user.FindFirst("EmployeeId")?.Value;
        return int.TryParse(employeeIdClaim, out var employeeId) ? employeeId : null;
    }

    /// <summary>
    /// Obtiene el ID del salón dueño desde los claims (para trabajadores)
    /// </summary>
    public static int? GetOwnerBarberId(ClaimsPrincipal user)
    {
        var ownerBarberIdClaim = user.FindFirst("OwnerBarberId")?.Value;
        return int.TryParse(ownerBarberIdClaim, out var ownerBarberId) ? ownerBarberId : null;
    }

    /// <summary>
    /// Genera un refresh token JWT para un usuario
    /// </summary>
    public static string GenerateRefreshToken(User user, string secretKey, string issuer, string audience, int expirationDays)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("UserId", user.Id.ToString()),
            new Claim("TokenType", "RefreshToken") // Marca especial para identificar refresh tokens
        };

        // Si es salón, agregar el ID del salón
        if (user.Barber != null)
        {
            claims.Add(new Claim("BarberId", user.Barber.Id.ToString()));
        }

        // Si es trabajador, agregar el ID del trabajador y del salón dueño
        if (user.Employee != null)
        {
            claims.Add(new Claim("EmployeeId", user.Employee.Id.ToString()));
            claims.Add(new Claim("OwnerBarberId", user.Employee.OwnerBarberId.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Valida y decodifica un refresh token JWT
    /// </summary>
    public static ClaimsPrincipal? ValidateRefreshToken(string token, string secretKey, string issuer, string audience)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            // Verificar que es un refresh token
            var tokenType = principal.FindFirst("TokenType")?.Value;
            if (tokenType != "RefreshToken")
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}

