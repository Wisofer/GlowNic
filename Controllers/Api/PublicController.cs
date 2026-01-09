using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GlowNic.Controllers.Api;

/// <summary>
/// Controlador de rutas públicas (sin autenticación)
/// </summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly IBarberService _barberService;
    private readonly IAvailabilityService _availabilityService;
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        IBarberService barberService,
        IAvailabilityService availabilityService,
        IAppointmentService appointmentService,
        ILogger<PublicController> logger)
    {
        _barberService = barberService;
        _availabilityService = availabilityService;
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Obtener información pública del salón
    /// </summary>
    [HttpGet("salons/{slug}")]
    public async Task<ActionResult<BarberPublicDto>> GetSalon(string slug)
    {
        try
        {
            var barber = await _barberService.GetPublicBarberInfoAsync(slug);
            return Ok(barber);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Salón no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener salón {Slug}", slug);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener disponibilidad del salón para una fecha
    /// </summary>
    [HttpGet("salons/{slug}/availability")]
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability(string slug, [FromQuery] DateOnly? date = null)
    {
        try
        {
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var availability = await _availabilityService.GetAvailabilityAsync(slug, targetDate);
            return Ok(availability);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Salón no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener disponibilidad para {Slug}", slug);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear una cita (público, sin autenticación)
    /// </summary>
    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(request);
            return CreatedAtAction(nameof(GetSalon), new { slug = request.BarberSlug }, appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cita");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

