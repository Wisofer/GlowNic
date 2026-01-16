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
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<PushNotificationController> _logger;

    public PushNotificationController(
        ApplicationDbContext context,
        IPushNotificationService pushNotificationService,
        ILogger<PushNotificationController> logger)
    {
        _context = context;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    private async Task<int> GetUserIdAsync()
    {
        var userId = JwtHelper.GetUserId(User);
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("Usuario no identificado");
        return userId.Value;
    }

    /// <summary>
    /// Registrar dispositivo para recibir notificaciones push
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
            _logger.LogError(ex, "Error al obtener dispositivo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener todos los dispositivos del usuario
    /// </summary>
    [HttpGet("devices")]
    public async Task<ActionResult<List<DeviceDto>>> GetDevices()
    {
        try
        {
            var userId = await GetUserIdAsync();
            var devices = await _context.Devices
                .Where(d => d.UserId == userId)
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
            _logger.LogError(ex, "Error al eliminar dispositivo");
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

            // Buscar dispositivo del usuario (cualquiera de sus dispositivos)
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (device == null)
                return NotFound(new { message = "No se encontró ningún dispositivo para actualizar" });

            // Actualizar token
            device.FcmToken = request.FcmToken;
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
    /// Actualizar token FCM de un dispositivo específico
    /// </summary>
    [HttpPut("devices/{id}/token")]
    public async Task<ActionResult<DeviceDto>> UpdateDeviceToken(int id, [FromBody] UpdateDeviceTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = await GetUserIdAsync();
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (device == null)
                return NotFound(new { message = "Dispositivo no encontrado" });

            // Actualizar token
            device.FcmToken = request.FcmToken;
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
    /// Obtener logs de notificaciones del usuario
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<List<NotificationLogDto>>> GetNotificationLogs(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
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
    /// Obtener log de notificación por ID
    /// </summary>
    [HttpGet("logs/{id}")]
    public async Task<ActionResult<NotificationLogDto>> GetNotificationLog(int id)
    {
        try
        {
            var userId = await GetUserIdAsync();
            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.Id == id && nl.UserId == userId);

            if (log == null)
                return NotFound(new { message = "Log de notificación no encontrado" });

            return Ok(new NotificationLogDto
            {
                Id = log.Id,
                Status = log.Status,
                Payload = log.Payload,
                SentAt = log.SentAt,
                DeviceId = log.DeviceId,
                TemplateId = log.TemplateId,
                UserId = log.UserId,
                CreatedAt = log.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener log de notificación");
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
                return NotFound(new { message = "Log de notificación no encontrado" });

            // Marcar como leída
            log.Status = "opened";
            log.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificación marcada como leída", id = log.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar notificación como leída");
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

            if (!logs.Any())
                return Ok(new { message = "No hay notificaciones pendientes de marcar como leídas", count = 0 });

            foreach (var log in logs)
            {
                log.Status = "opened";
                log.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Todas las notificaciones fueron marcadas como leídas", count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar todas las notificaciones como leídas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar log de notificación
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
                return NotFound(new { message = "Log de notificación no encontrado" });

            _context.NotificationLogs.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificación eliminada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar log de notificación");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}
