using Microsoft.AspNetCore.Mvc;
using GlowNic.Services.Interfaces;
using GlowNic.Models.DTOs.Requests;

namespace GlowNic.Controllers.Web;

/// <summary>
/// Controlador para rutas públicas (sin autenticación)
/// </summary>
public class PublicController : Controller
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
    /// Vista pública del salón para agendar citas
    /// </summary>
    [HttpGet("/b/{slug}")]
    public async Task<IActionResult> BarberProfile(string slug)
    {
        try
        {
            var barber = await _barberService.GetPublicBarberInfoAsync(slug);
            ViewBag.Barber = barber;
            ViewBag.Slug = slug;
            return View();
        }
        catch (KeyNotFoundException)
        {
            return View("NotFound");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener salón {Slug}", slug);
            return View("Error");
        }
    }

    /// <summary>
    /// Obtener disponibilidad para una fecha (AJAX)
    /// </summary>
    [HttpGet("/b/{slug}/availability")]
    public async Task<IActionResult> GetAvailability(string slug, [FromQuery] string? date = null)
    {
        try
        {
            DateOnly? targetDate = null;
            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsedDate))
            {
                targetDate = parsedDate;
            }

            var availability = await _availabilityService.GetAvailabilityAsync(slug, targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow));
            return Json(availability);
        }
        catch (KeyNotFoundException)
        {
            return Json(new { error = "Salón no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener disponibilidad para {Slug}", slug);
            return Json(new { error = "Error al obtener disponibilidad" });
        }
    }

    /// <summary>
    /// Crear cita desde la vista pública (AJAX)
    /// </summary>
    [HttpPost("/b/{slug}/appointment")]
    public async Task<IActionResult> CreateAppointment(string slug, [FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Datos inválidos" });
        }

        try
        {
            // Asegurar que el slug coincida
            request.BarberSlug = slug;
            var appointment = await _appointmentService.CreateAppointmentAsync(request);
            return Json(new { success = true, message = "Cita agendada exitosamente", appointment });
        }
        catch (KeyNotFoundException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cita");
            return Json(new { success = false, message = "Error al agendar la cita. Por favor intenta nuevamente." });
        }
    }
}

