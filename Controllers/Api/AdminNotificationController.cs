using GlowNic.Data;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Controllers.Api;

/// <summary>
/// Controlador para administración de notificaciones (solo Admin)
/// </summary>
[ApiController]
[Route("api/admin/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminNotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<AdminNotificationController> _logger;

    public AdminNotificationController(
        ApplicationDbContext context,
        IPushNotificationService pushNotificationService,
        ILogger<AdminNotificationController> logger)
    {
        _context = context;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Crear plantilla de notificación
    /// </summary>
    [HttpPost("templates")]
    public async Task<ActionResult<TemplateDto>> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var template = new Template
            {
                Title = request.Title,
                Body = request.Body,
                ImageUrl = request.ImageUrl,
                Name = request.Name
            };

            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, new TemplateDto
            {
                Id = template.Id,
                Title = template.Title,
                Body = template.Body,
                ImageUrl = template.ImageUrl,
                Name = template.Name,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear plantilla");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener todas las plantillas
    /// </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<List<TemplateDto>>> GetTemplates()
    {
        try
        {
            var templates = await _context.Templates
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TemplateDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Body = t.Body,
                    ImageUrl = t.ImageUrl,
                    Name = t.Name,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener plantillas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener plantilla por ID
    /// </summary>
    [HttpGet("templates/{id}")]
    public async Task<ActionResult<TemplateDto>> GetTemplate(int id)
    {
        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null)
                return NotFound(new { message = "Plantilla no encontrada" });

            return Ok(new TemplateDto
            {
                Id = template.Id,
                Title = template.Title,
                Body = template.Body,
                ImageUrl = template.ImageUrl,
                Name = template.Name,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener plantilla");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar plantilla
    /// </summary>
    [HttpPut("templates/{id}")]
    public async Task<ActionResult<TemplateDto>> UpdateTemplate(int id, [FromBody] CreateTemplateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null)
                return NotFound(new { message = "Plantilla no encontrada" });

            template.Title = request.Title;
            template.Body = request.Body;
            template.ImageUrl = request.ImageUrl;
            template.Name = request.Name;
            template.UpdatedAt = DateTime.UtcNow;

            _context.Templates.Update(template);
            await _context.SaveChangesAsync();

            return Ok(new TemplateDto
            {
                Id = template.Id,
                Title = template.Title,
                Body = template.Body,
                ImageUrl = template.ImageUrl,
                Name = template.Name,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar plantilla");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar plantilla
    /// </summary>
    [HttpDelete("templates/{id}")]
    public async Task<ActionResult> DeleteTemplate(int id)
    {
        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null)
                return NotFound(new { message = "Plantilla no encontrada" });

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar plantilla");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Enviar notificación manual
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            Template? template = null;

            // Si se proporciona TemplateId, obtener la plantilla
            if (request.TemplateId.HasValue)
            {
                template = await _context.Templates.FindAsync(request.TemplateId.Value);
                if (template == null)
                    return NotFound(new { message = "Plantilla no encontrada" });
            }
            // Si no, crear template temporal con Title y Body
            else if (!string.IsNullOrWhiteSpace(request.Title) && !string.IsNullOrWhiteSpace(request.Body))
            {
                template = new Template
                {
                    Title = request.Title,
                    Body = request.Body,
                    ImageUrl = request.ImageUrl,
                    Name = "Notificación manual"
                };
                _context.Templates.Add(template);
                await _context.SaveChangesAsync();
            }
            else
            {
                return BadRequest(new { message = "Debe proporcionar TemplateId o Title y Body" });
            }

            // Obtener dispositivos
            List<Device> devices;
            if (request.UserIds != null && request.UserIds.Any())
            {
                // Dispositivos de usuarios específicos
                devices = await _context.Devices
                    .Where(d => request.UserIds.Contains(d.UserId) && !string.IsNullOrWhiteSpace(d.FcmToken))
                    .ToListAsync();
            }
            else
            {
                // Todos los dispositivos
                devices = await _context.Devices
                    .Where(d => !string.IsNullOrWhiteSpace(d.FcmToken))
                    .ToListAsync();
            }

            if (!devices.Any())
                return BadRequest(new { message = "No hay dispositivos registrados para enviar notificaciones" });

            // Enviar notificación
            await _pushNotificationService.SendPushNotificationAsync(
                template,
                devices,
                request.ExtraData,
                request.DataOnly);

            return Ok(new SendNotificationResponse
            {
                Success = true,
                Message = "Notificación enviada exitosamente",
                UserCount = devices.Select(d => d.UserId).Distinct().Count(),
                SentCount = devices.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener lista de usuarios con dispositivos registrados
    /// </summary>
    [HttpGet("users-with-devices")]
    public async Task<ActionResult> GetUsersWithDevices()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.Barber)
                .Where(u => _context.Devices.Any(d => d.UserId == u.Id))
                .Select(u => new
                {
                    userId = u.Id,
                    email = u.Email,
                    role = u.Role.ToString(),
                    deviceCount = _context.Devices.Count(d => d.UserId == u.Id),
                    barberName = u.Barber != null ? u.Barber.Name : null
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios con dispositivos");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}
