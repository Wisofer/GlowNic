using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlowNic.Controllers.Api;

/// <summary>
/// Controlador de rutas del administrador
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IBarberService _barberService;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IBarberService barberService,
        IDashboardService dashboardService,
        ILogger<AdminController> logger)
    {
        _barberService = barberService;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Obtener dashboard del admin
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
    {
        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener todos los salones
    /// </summary>
    [HttpGet("salons")]
    public async Task<ActionResult<List<BarberDto>>> GetSalons([FromQuery] bool? isActive = null)
    {
        try
        {
            var barbers = await _barberService.GetAllBarbersAsync(isActive);
            return Ok(barbers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener salóns");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar estado del salón (activar/desactivar)
    /// </summary>
    [HttpPut("salons/{id}/status")]
    public async Task<ActionResult> UpdateSalonStatus(int id, [FromBody] UpdateBarberStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _barberService.UpdateBarberStatusAsync(id, request.IsActive);
            if (!updated)
                return NotFound(new { message = "Salón no encontrado" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estado del salón {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear nuevo salón
    /// </summary>
    [HttpPost("salons")]
    public async Task<ActionResult<BarberDto>> CreateSalon([FromBody] CreateBarberRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barber = await _barberService.CreateBarberAsync(request);
            return CreatedAtAction(nameof(GetSalons), new { id = barber.Id }, barber);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear salón");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar salón
    /// </summary>
    [HttpDelete("salons/{id}")]
    public async Task<ActionResult> DeleteSalon(int id)
    {
        try
        {
            var deleted = await _barberService.DeleteBarberAsync(id);
            if (!deleted)
                return NotFound(new { message = "Salón no encontrado" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar salón {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

/// <summary>
/// DTO para actualizar estado del salón
/// </summary>
public class UpdateBarberStatusRequest
{
    public bool IsActive { get; set; }
}

