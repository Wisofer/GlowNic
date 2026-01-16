using GlowNic.Data;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using GlowNic.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlowNic.Controllers.Api;

/// <summary>
/// Controlador para gestión de notificaciones push
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PushNotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<PushNotificationController> _logger;

    public PushNotificationController(
        ApplicationDbContext context,
        IPushNotificationService pushService,
        ILogger<PushNotificationController> logger)
    {
        _context = context;
        _pushService = pushService;
        _logger = logger;
    }

    private Task<int> GetUserIdAsync()
    {
        var userId = JwtHelper.GetUserId(User);
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("Usuario no identificado");
        return Task.FromResult(userId.Value);
    }

    #region Templates

    /// <summary>
    /// Obtener todas las plantillas (solo Admin)
    /// </summary>
    [HttpGet("templates")]
    [Authorize(Roles = "Admin")]
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
    /// Obtener plantilla por ID (solo Admin)
    /// </summary>
    [HttpGet("templates/{id}")]
    [Authorize(Roles = "Admin")]
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
            _logger.LogError(ex, "Error al obtener plantilla {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear nueva plantilla (solo Admin)
    /// </summary>
    [HttpPost("templates")]
    [Authorize(Roles = "Admin")]
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
    /// Actualizar plantilla (solo Admin)
    /// </summary>
    [HttpPut("templates/{id}")]
    [Authorize(Roles = "Admin")]
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
            _logger.LogError(ex, "Error al actualizar plantilla {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar plantilla (solo Admin)
    /// </summary>
    [HttpDelete("templates/{id}")]
    [Authorize(Roles = "Admin")]
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
            _logger.LogError(ex, "Error al eliminar plantilla {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    #region Devices

    /// <summary>
    /// Registrar dispositivo para recibir notificaciones
    /// </summary>
    [HttpPost("devices")]
    public async Task<ActionResult<DeviceDto>> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = await GetUserIdAsync();

            // Verificar si el dispositivo ya existe
            var existingDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.FcmToken == request.FcmToken);

            if (existingDevice != null)
            {
                // Actualizar dispositivo existente
                existingDevice.Platform = request.Platform;
                existingDevice.UserId = userId;
                existingDevice.LastActiveAt = DateTime.UtcNow;
                existingDevice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new DeviceDto
                {
                    Id = existingDevice.Id,
                    FcmToken = existingDevice.FcmToken,
                    Platform = existingDevice.Platform,
                    LastActiveAt = existingDevice.LastActiveAt,
                    UserId = existingDevice.UserId,
                    CreatedAt = existingDevice.CreatedAt,
                    UpdatedAt = existingDevice.UpdatedAt
                });
            }

            // Crear nuevo dispositivo
            var device = new Device
            {
                FcmToken = request.FcmToken,
                Platform = request.Platform,
                UserId = userId,
                LastActiveAt = DateTime.UtcNow
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, new DeviceDto
            {
                Id = device.Id,
                FcmToken = device.FcmToken,
                Platform = device.Platform,
                LastActiveAt = device.LastActiveAt,
                UserId = device.UserId,
                CreatedAt = device.CreatedAt,
                UpdatedAt = device.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar dispositivo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener dispositivo por ID
    /// </summary>
    [HttpGet("devices/{id}")]
    public async Task<ActionResult<DeviceDto>> GetDevice(int id)
    {
        try
        {
            var userId = await GetUserIdAsync();
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (device == null)
                return NotFound(new { message = "Dispositivo no encontrado" });

            return Ok(new DeviceDto
            {
                Id = device.Id,
                FcmToken = device.FcmToken,
                Platform = device.Platform,
                LastActiveAt = device.LastActiveAt,
                UserId = device.UserId,
                CreatedAt = device.CreatedAt,
                UpdatedAt = device.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dispositivo {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener todos los dispositivos del usuario actual
    /// </summary>
    [HttpGet("devices")]
    public async Task<ActionResult<List<DeviceDto>>> GetDevices()
    {
        try
        {
            var userId = await GetUserIdAsync();
            var devices = await _context.Devices
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.LastActiveAt)
                .Select(d => new DeviceDto
                {
                    Id = d.Id,
                    FcmToken = d.FcmToken,
                    Platform = d.Platform,
                    LastActiveAt = d.LastActiveAt,
                    UserId = d.UserId,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                })
                .ToListAsync();

            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dispositivos");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar token FCM del dispositivo
    /// </summary>
    [HttpPost("devices/refresh-token")]
    public async Task<ActionResult<DeviceDto>> RefreshDeviceToken([FromBody] UpdateDeviceTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = await GetUserIdAsync();

            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.FcmToken == request.CurrentFcmToken && d.UserId == userId);

            if (device == null)
                return NotFound(new { message = "Dispositivo no encontrado" });

            device.FcmToken = request.NewFcmToken;
            device.Platform = request.Platform;
            device.LastActiveAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new DeviceDto
            {
                Id = device.Id,
                FcmToken = device.FcmToken,
                Platform = device.Platform,
                LastActiveAt = device.LastActiveAt,
                UserId = device.UserId,
                CreatedAt = device.CreatedAt,
                UpdatedAt = device.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar token del dispositivo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar dispositivo
    /// </summary>
    [HttpDelete("devices/{id}")]
    public async Task<ActionResult> DeleteDevice(int id)
    {
        try
        {
            var userId = await GetUserIdAsync();
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (device == null)
                return NotFound(new { message = "Dispositivo no encontrado" });

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar dispositivo {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    #region Send Notifications

    /// <summary>
    /// Enviar notificación push al usuario actual (solo Admin)
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SendNotificationResponse>> SendNotification([FromBody] SendNotificationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var template = await _context.Templates.FindAsync(request.TemplateId);

            if (template == null)
                return NotFound(new { message = "Plantilla no encontrada" });

            var userId = await GetUserIdAsync();

            var devices = await _context.Devices
                .Where(d => d.UserId == userId && !string.IsNullOrWhiteSpace(d.FcmToken))
                .ToListAsync();

            if (!devices.Any())
                return BadRequest(new { message = "No hay dispositivos registrados para este usuario" });

            await _pushService.SendPushNotificationAsync(
                template,
                devices,
                request.ExtraData,
                request.DataOnly);

            return Ok(new SendNotificationResponse
            {
                Success = true,
                Message = "Notificación enviada exitosamente",
                SentCount = devices.Count,
                UserCount = 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    #region Notification Logs

    /// <summary>
    /// Obtener logs de notificaciones del usuario actual
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<List<NotificationLogDto>>> GetNotificationLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = await GetUserIdAsync();

            var logs = await _context.NotificationLogs
                .Where(nl => nl.UserId == userId)
                .OrderByDescending(nl => nl.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(nl => new NotificationLogDto
                {
                    Id = nl.Id,
                    Status = nl.Status,
                    Payload = nl.Payload,
                    SentAt = nl.SentAt,
                    DeviceId = nl.DeviceId,
                    TemplateId = nl.TemplateId,
                    UserId = nl.UserId,
                    CreatedAt = nl.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener logs de notificaciones");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Marcar notificación como leída
    /// </summary>
    [HttpPost("logs/{id}/opened")]
    public async Task<ActionResult> MarkNotificationAsOpened(int id)
    {
        try
        {
            var userId = await GetUserIdAsync();

            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.Id == id && nl.UserId == userId);

            if (log == null)
                return NotFound(new { message = "Notificación no encontrada" });

            log.Status = "opened";
            log.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificación marcada como leída", id = log.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar notificación como leída {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar notificación
    /// </summary>
    [HttpDelete("logs/{id}")]
    public async Task<ActionResult> DeleteNotificationLog(int id)
    {
        try
        {
            var userId = await GetUserIdAsync();

            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.Id == id && nl.UserId == userId);

            if (log == null)
                return NotFound(new { message = "Notificación no encontrada" });

            _context.NotificationLogs.Remove(log);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar notificación {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Marcar todas las notificaciones como leídas
    /// </summary>
    [HttpPost("logs/opened-all")]
    public async Task<ActionResult> MarkAllNotificationsAsOpened()
    {
        try
        {
            var userId = await GetUserIdAsync();

            var logs = await _context.NotificationLogs
                .Where(nl => nl.UserId == userId && nl.Status != "opened")
                .ToListAsync();

            foreach (var log in logs)
            {
                log.Status = "opened";
                log.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{logs.Count} notificaciones marcadas como leídas", count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar todas las notificaciones como leídas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar todas las notificaciones del usuario
    /// </summary>
    [HttpDelete("logs/delete-all")]
    public async Task<ActionResult> DeleteAllNotificationLogs()
    {
        try
        {
            var userId = await GetUserIdAsync();

            var logs = await _context.NotificationLogs
                .Where(nl => nl.UserId == userId)
                .ToListAsync();

            if (!logs.Any())
                return Ok(new { message = "No hay notificaciones para eliminar", count = 0 });

            _context.NotificationLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Todas las notificaciones fueron eliminadas exitosamente", count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar todas las notificaciones");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion
}
