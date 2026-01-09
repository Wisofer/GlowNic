using GlowNic.Data;
using GlowNic.Models.Entities;
using GlowNic.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GlowNic.Data;

/// <summary>
/// Inicialización del nuevo sistema (User admin)
/// </summary>
public static class InicializarSistema
{
    public static void CrearAdminUserSiNoExiste(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Verificar si ya existe un usuario admin
            var existeAdmin = context.Users
                .Any(u => u.Email.ToLower() == "admin@barberpro.com" && u.Role == UserRole.Admin);

            if (existeAdmin)
            {
                logger.LogInformation("El usuario admin (User) ya existe en la base de datos.");
                return;
            }

            // Crear usuario admin
            var admin = new User
            {
                Email = "admin@barberpro.com",
                PasswordHash = PasswordHelper.HashPassword("admin123"),
                Role = UserRole.Admin,
                IsActive = true
            };

            context.Users.Add(admin);
            context.SaveChanges(); // Síncrono está bien aquí porque se ejecuta en el scope de inicialización

            logger.LogInformation("Usuario admin (User) creado exitosamente. Email: admin@barberpro.com, Password: admin123");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear el usuario admin (User) en la base de datos.");
        }
    }
}

