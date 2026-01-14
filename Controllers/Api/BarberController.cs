using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using GlowNic.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlowNic.Controllers.Api;

/// <summary>
/// Controlador de rutas del salón
/// </summary>
[ApiController]
[Route("api/salon")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Barber")]
public class BarberController : ControllerBase
{
    private readonly IBarberService _barberService;
    private readonly IAppointmentService _appointmentService;
    private readonly IServiceService _serviceService;
    private readonly IFinanceService _financeService;
    private readonly IDashboardService _dashboardService;
    private readonly IAuthService _authService;
    private readonly IWorkingHoursService _workingHoursService;
    private readonly IExportService _exportService;
    private readonly IHelpSupportService _helpSupportService;
    private readonly IEmployeeService _employeeService;
    private readonly IReportService _reportService;
    private readonly ILogger<BarberController> _logger;

    public BarberController(
        IBarberService barberService,
        IAppointmentService appointmentService,
        IServiceService serviceService,
        IFinanceService financeService,
        IDashboardService dashboardService,
        IAuthService authService,
        IWorkingHoursService workingHoursService,
        IExportService exportService,
        IHelpSupportService helpSupportService,
        IEmployeeService employeeService,
        IReportService reportService,
        ILogger<BarberController> logger)
    {
        _barberService = barberService;
        _appointmentService = appointmentService;
        _serviceService = serviceService;
        _financeService = financeService;
        _dashboardService = dashboardService;
        _authService = authService;
        _workingHoursService = workingHoursService;
        _exportService = exportService;
        _helpSupportService = helpSupportService;
        _employeeService = employeeService;
        _reportService = reportService;
        _logger = logger;
    }

    private async Task<int> GetBarberIdAsync()
    {
        var barberId = JwtHelper.GetBarberId(User);
        if (!barberId.HasValue)
            throw new UnauthorizedAccessException("Salón no identificado");
        
        // Verificar que el salón existe y está activo en la base de datos
        var barber = await _barberService.GetBarberByIdAsync(barberId.Value);
        if (barber == null)
            throw new UnauthorizedAccessException("Salón no encontrado o fue eliminado");
        
        if (!barber.IsActive)
            throw new UnauthorizedAccessException("Salón desactivado");
        
        return barberId.Value;
    }

    /// <summary>
    /// Obtener dashboard del salón
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<BarberDashboardDto>> GetDashboard()
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var dashboard = await _dashboardService.GetBarberDashboardAsync(barberId);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener perfil del salón
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<BarberDto>> GetProfile()
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var profile = await _barberService.GetBarberProfileAsync(barberId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Salón no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener perfil");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar perfil del salón
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<BarberDto>> UpdateProfile([FromBody] UpdateBarberProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var profile = await _barberService.UpdateBarberProfileAsync(barberId, request);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Salón no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar perfil");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener URL del QR
    /// </summary>
    [HttpGet("qr-url")]
    public async Task<ActionResult<QrUrlResponse>> GetQrUrl()
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var url = await _barberService.GetQrUrlAsync(barberId);
            var qrCode = QrHelper.GenerateQrCodeBase64(url);

            return Ok(new QrUrlResponse
            {
                Url = url,
                QrCode = qrCode
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Salón no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener QR");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener citas del salón
    /// </summary>
    /// <param name="date">Fecha específica (opcional). Si no se envía, devuelve todas las citas</param>
    /// <param name="status">Estado de la cita (opcional): Pending, Confirmed, Completed, Cancelled</param>
    /// <param name="startDate">Fecha de inicio para rango (opcional, formato: YYYY-MM-DD)</param>
    /// <param name="endDate">Fecha de fin para rango (opcional, formato: YYYY-MM-DD)</param>
    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments(
        [FromQuery] DateOnly? date = null, 
        [FromQuery] AppointmentStatus? status = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            
            // Si se proporciona un rango de fechas, obtener todas las citas en ese rango
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (DateOnly.TryParse(startDate, out var start) && DateOnly.TryParse(endDate, out var end))
                {
                    var allAppointments = await _appointmentService.GetBarberAppointmentsAsync(barberId, null, status);
                    var filteredAppointments = allAppointments
                        .Where(a => a.Date >= start && a.Date <= end)
                        .ToList();
                    return Ok(filteredAppointments);
                }
            }
            
            // Comportamiento original: fecha específica o todas las citas
            var appointments = await _appointmentService.GetBarberAppointmentsAsync(barberId, date, status);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener citas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener historial completo de citas del salón (todas las fechas)
    /// </summary>
    /// <param name="status">Estado de la cita (opcional): Pending, Confirmed, Completed, Cancelled</param>
    [HttpGet("appointments/history")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointmentsHistory([FromQuery] AppointmentStatus? status = null)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            // Pasar null como date para obtener todas las citas
            var appointments = await _appointmentService.GetBarberAppointmentsAsync(barberId, null, status);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de citas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear cita manual (el salón puede crear citas sin necesidad de barberSlug)
    /// </summary>
    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        // El salón no necesita pasar barberSlug, se usa su barberId del token
        // Limpiar cualquier error de validación de BarberSlug antes de validar
        if (ModelState.ContainsKey("BarberSlug"))
            ModelState.Remove("BarberSlug");
        request.BarberSlug = null;

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var appointment = await _appointmentService.CreateAppointmentForBarberAsync(barberId, request);
            return CreatedAtAction(nameof(GetAppointments), null, appointment);
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

    /// <summary>
    /// Actualizar cita (solo del salón autenticado)
    /// </summary>
    [HttpPut("appointments/{id}")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var appointment = await _appointmentService.UpdateAppointmentForBarberAsync(barberId, id, request, null);
            return Ok(appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener URL de WhatsApp para notificar confirmación de cita
    /// </summary>
    [HttpGet("appointments/{id}/whatsapp-url")]
    public async Task<ActionResult<WhatsAppUrlResponse>> GetWhatsAppUrl(int id)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            
            if (appointment == null || appointment.BarberId != barberId)
                return NotFound(new { message = "Cita no encontrada" });

            // Formatear fecha y hora
            var fecha = appointment.Date.ToString("dd/MM/yyyy");
            var hora = appointment.Time.ToString("HH:mm");
            
            // Limpiar número de teléfono (remover espacios, guiones, etc.)
            var phoneNumber = appointment.ClientPhone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            
            // Si no empieza con código de país, agregar 505 (Nicaragua)
            if (!phoneNumber.StartsWith("505"))
            {
                phoneNumber = phoneNumber.StartsWith("+") ? phoneNumber.Substring(1) : phoneNumber;
                if (!phoneNumber.StartsWith("505"))
                    phoneNumber = "505" + phoneNumber;
            }

            // Construir mensaje
            var mensaje = $"Hola {appointment.ClientName}! 👋\n\n" +
                         $"Tu cita del {fecha} a las {hora} ha sido confirmada. " +
                         $"¡Te esperamos! ✂️";

            // Codificar mensaje para URL
            var mensajeCodificado = Uri.EscapeDataString(mensaje);
            
            // Construir URL de WhatsApp
            var whatsappUrl = $"https://wa.me/{phoneNumber}?text={mensajeCodificado}";

            return Ok(new WhatsAppUrlResponse
            {
                Url = whatsappUrl,
                PhoneNumber = phoneNumber,
                Message = mensaje
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Cita no encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar URL de WhatsApp para cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener URL de WhatsApp para notificar rechazo/cancelación de cita
    /// </summary>
    [HttpGet("appointments/{id}/whatsapp-url-reject")]
    public async Task<ActionResult<WhatsAppUrlResponse>> GetWhatsAppUrlReject(int id)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            
            if (appointment == null || appointment.BarberId != barberId)
                return NotFound(new { message = "Cita no encontrada" });

            // Formatear fecha y hora
            var fecha = appointment.Date.ToString("dd/MM/yyyy");
            var hora = appointment.Time.ToString("HH:mm");
            
            // Limpiar número de teléfono (remover espacios, guiones, etc.)
            var phoneNumber = appointment.ClientPhone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            
            // Si no empieza con código de país, agregar 505 (Nicaragua)
            if (!phoneNumber.StartsWith("505"))
            {
                phoneNumber = phoneNumber.StartsWith("+") ? phoneNumber.Substring(1) : phoneNumber;
                if (!phoneNumber.StartsWith("505"))
                    phoneNumber = "505" + phoneNumber;
            }

            // Construir mensaje de disculpa
            var mensaje = $"Hola {appointment.ClientName}! 👋\n\n" +
                         $"Lamentamos informarte que no podemos atenderte el {fecha} a las {hora}. " +
                         $"¿Te gustaría reagendar para otro horario? Estaremos encantados de atenderte. 💅";

            // Codificar mensaje para URL
            var mensajeCodificado = Uri.EscapeDataString(mensaje);
            
            // Construir URL de WhatsApp
            var whatsappUrl = $"https://wa.me/{phoneNumber}?text={mensajeCodificado}";

            return Ok(new WhatsAppUrlResponse
            {
                Url = whatsappUrl,
                PhoneNumber = phoneNumber,
                Message = mensaje
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Cita no encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar URL de WhatsApp de rechazo para cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar cita (solo del salón autenticado)
    /// </summary>
    [HttpDelete("appointments/{id}")]
    public async Task<ActionResult> DeleteAppointment(int id)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var deleted = await _appointmentService.DeleteAppointmentForBarberAsync(barberId, id);
            if (!deleted)
                return NotFound(new { message = "Cita no encontrada o no pertenece al salón" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener servicios del salón
    /// </summary>
    [HttpGet("services")]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var services = await _serviceService.GetBarberServicesAsync(barberId);
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener servicios");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear servicio
    /// </summary>
    [HttpPost("services")]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] CreateServiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var service = await _serviceService.CreateServiceAsync(barberId, request);
            return CreatedAtAction(nameof(GetServices), null, service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear servicio");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar servicio
    /// </summary>
    [HttpPut("services/{id}")]
    public async Task<ActionResult<ServiceDto>> UpdateService(int id, [FromBody] CreateServiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var updated = await _serviceService.UpdateServiceAsync(barberId, id, request);
            
            if (!updated)
                return NotFound(new { message = "Servicio no encontrado o no pertenece al salón" });

            var service = await _serviceService.GetServiceByIdAsync(id);
            return Ok(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar servicio {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar servicio (soft delete)
    /// </summary>
    [HttpDelete("services/{id}")]
    public async Task<ActionResult> DeleteService(int id)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var deleted = await _serviceService.DeleteServiceAsync(barberId, id);
            
            if (!deleted)
                return NotFound(new { message = "Servicio no encontrado o no pertenece al salón" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar servicio {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener resumen financiero
    /// </summary>
    [HttpGet("finances/summary")]
    public async Task<ActionResult<FinanceSummaryDto>> GetFinanceSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var summary = await _financeService.GetFinanceSummaryAsync(barberId, startDate, endDate);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener resumen financiero");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener ingresos
    /// </summary>
    [HttpGet("finances/income")]
    public async Task<ActionResult<TransactionsResponse>> GetIncome([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            
            // Parsear y normalizar fechas desde string para evitar problemas de zona horaria
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;
            
            if (!string.IsNullOrEmpty(startDate))
            {
                if (DateTime.TryParse(startDate, out var start))
                {
                    parsedStartDate = NormalizeDateForFilter(start, isEndDate: false);
                }
            }
            
            if (!string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(endDate, out var end))
                {
                    parsedEndDate = NormalizeDateForFilter(end, isEndDate: true);
                }
            }
            
            var income = await _financeService.GetIncomeAsync(barberId, parsedStartDate, parsedEndDate, page, pageSize);
            return Ok(income);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ingresos");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener egresos
    /// </summary>
    [HttpGet("finances/expenses")]
    public async Task<ActionResult<TransactionsResponse>> GetExpenses([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            
            // Parsear y normalizar fechas desde string para evitar problemas de zona horaria
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;
            
            if (!string.IsNullOrEmpty(startDate))
            {
                if (DateTime.TryParse(startDate, out var start))
                {
                    parsedStartDate = NormalizeDateForFilter(start, isEndDate: false);
                }
            }
            
            if (!string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(endDate, out var end))
                {
                    parsedEndDate = NormalizeDateForFilter(end, isEndDate: true);
                }
            }
            
            var expenses = await _financeService.GetExpensesAsync(barberId, parsedStartDate, parsedEndDate, page, pageSize);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener egresos");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear ingreso manual
    /// </summary>
    [HttpPost("finances/income")]
    public async Task<ActionResult<TransactionDto>> CreateIncome([FromBody] CreateIncomeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var income = await _financeService.CreateIncomeAsync(barberId, request);
            return CreatedAtAction(nameof(GetIncome), null, income);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear ingreso");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear egreso
    /// </summary>
    [HttpPost("finances/expenses")]
    public async Task<ActionResult<TransactionDto>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var expense = await _financeService.CreateExpenseAsync(barberId, request);
            return CreatedAtAction(nameof(GetExpenses), null, expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear egreso");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar egreso
    /// </summary>
    [HttpPut("finances/expenses/{id}")]
    public async Task<ActionResult<TransactionDto>> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var expense = await _financeService.UpdateExpenseAsync(barberId, id, request);
            return Ok(expense);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar egreso {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar egreso
    /// </summary>
    [HttpDelete("finances/expenses/{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var deleted = await _financeService.DeleteExpenseAsync(barberId, id);
            if (!deleted)
                return NotFound(new { message = "Egreso no encontrado o no pertenece al salón" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar egreso {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener categorías predefinidas
    /// </summary>
    [HttpGet("finances/categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        try
        {
            var categories = await _financeService.GetCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categorías");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Cambiar contraseña del salón
    /// </summary>
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = JwtHelper.GetUserId(User);
            if (!userId.HasValue)
                return Unauthorized(new { message = "Usuario no identificado" });

            var success = await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            if (!success)
                return BadRequest(new { message = "La contraseña actual es incorrecta" });

            return Ok(new { message = "Contraseña actualizada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar contraseña");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener todos los horarios de trabajo del salón
    /// </summary>
    [HttpGet("working-hours")]
    public async Task<ActionResult<List<WorkingHoursDto>>> GetWorkingHours()
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var workingHours = await _workingHoursService.GetWorkingHoursAsync(barberId);
            return Ok(workingHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener horarios de trabajo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar o crear horarios de trabajo (upsert)
    /// Si el horario para ese día ya existe, lo actualiza; si no, lo crea
    /// </summary>
    [HttpPut("working-hours")]
    public async Task<ActionResult<List<WorkingHoursDto>>> UpdateWorkingHours([FromBody] UpdateWorkingHoursBatchRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.WorkingHours == null || !request.WorkingHours.Any())
            return BadRequest(new { message = "Debe incluir al menos un horario" });

        try
        {
            var barberId = await GetBarberIdAsync();
            var workingHours = await _workingHoursService.UpdateWorkingHoursAsync(barberId, request.WorkingHours);
            return Ok(workingHours);
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
            _logger.LogError(ex, "Error al actualizar horarios de trabajo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar un horario de trabajo específico
    /// </summary>
    [HttpDelete("working-hours/{id}")]
    public async Task<ActionResult> DeleteWorkingHours(int id)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var deleted = await _workingHoursService.DeleteWorkingHoursAsync(barberId, id);
            
            if (!deleted)
                return NotFound(new { message = "Horario de trabajo no encontrado" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar horario de trabajo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Exportar reporte de citas
    /// </summary>
    [HttpGet("export/appointments")]
    public async Task<ActionResult> ExportAppointments([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] string format = "csv")
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            DateOnly? start = null;
            DateOnly? end = null;

            if (!string.IsNullOrEmpty(startDate) && DateOnly.TryParse(startDate, out var parsedStart))
                start = parsedStart;
            if (!string.IsNullOrEmpty(endDate) && DateOnly.TryParse(endDate, out var parsedEnd))
                end = parsedEnd;

            var fileBytes = await _exportService.ExportAppointmentsAsync(barberId, start, end, format);
            var contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
            var fileName = $"citas_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";

            return File(fileBytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar citas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Exportar reporte financiero
    /// </summary>
    [HttpGet("export/finances")]
    public async Task<ActionResult> ExportFinances([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] string format = "csv")
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            DateOnly? start = null;
            DateOnly? end = null;

            if (!string.IsNullOrEmpty(startDate) && DateOnly.TryParse(startDate, out var parsedStart))
                start = parsedStart;
            if (!string.IsNullOrEmpty(endDate) && DateOnly.TryParse(endDate, out var parsedEnd))
                end = parsedEnd;

            var fileBytes = await _exportService.ExportFinancesAsync(barberId, start, end, format);
            var contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
            var fileName = $"finanzas_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";

            return File(fileBytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar finanzas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Exportar reporte de clientes
    /// </summary>
    [HttpGet("export/clients")]
    public async Task<ActionResult> ExportClients([FromQuery] string format = "csv")
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var fileBytes = await _exportService.ExportClientsAsync(barberId, format);
            var contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
            var fileName = $"clientes_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";

            return File(fileBytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar clientes");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear backup completo de datos
    /// </summary>
    [HttpGet("export/backup")]
    public async Task<ActionResult> ExportBackup()
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var fileBytes = await _exportService.ExportBackupAsync(barberId);
            var fileName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

            return File(fileBytes, "application/json", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear backup");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener información de ayuda y soporte
    /// </summary>
    [HttpGet("help-support")]
    public async Task<ActionResult<HelpSupportDto>> GetHelpSupport()
    {
        try
        {
            var helpSupport = await _helpSupportService.GetHelpSupportAsync();
            return Ok(helpSupport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ayuda y soporte");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #region Trabajadores/Empleados

    /// <summary>
    /// Obtener todos los trabajadores del salón
    /// </summary>
    [HttpGet("employees")]
    public async Task<ActionResult<List<EmployeeDto>>> GetEmployees()
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var employees = await _employeeService.GetEmployeesAsync(barberId);
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener trabajadores");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener un trabajador por ID
    /// </summary>
    [HttpGet("employees/{id}")]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var employee = await _employeeService.GetEmployeeByIdAsync(id, barberId);
            if (employee == null)
                return NotFound(new { message = "Trabajador no encontrado o no pertenece al salón" });
            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener trabajador {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear un nuevo trabajador
    /// </summary>
    [HttpPost("employees")]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var employee = await _employeeService.CreateEmployeeAsync(barberId, request);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
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
            _logger.LogError(ex, "Error al crear trabajador");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar un trabajador
    /// </summary>
    [HttpPut("employees/{id}")]
    public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = await GetBarberIdAsync();
            var employee = await _employeeService.UpdateEmployeeAsync(id, barberId, request);
            if (employee == null)
                return NotFound(new { message = "Trabajador no encontrado o no pertenece al salón" });
            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar trabajador {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar (desactivar) un trabajador
    /// </summary>
    [HttpDelete("employees/{id}")]
    public async Task<ActionResult> DeleteEmployee(int id)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var deleted = await _employeeService.DeleteEmployeeAsync(id, barberId);
            if (!deleted)
                return NotFound(new { message = "Trabajador no encontrado o no pertenece al salón" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar trabajador {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    #region Reportes de Empleados

    /// <summary>
    /// Obtener reporte de citas por empleado
    /// </summary>
    [HttpGet("reports/employees/appointments")]
    public async Task<ActionResult<EmployeeAppointmentsReportDto>> GetEmployeeAppointmentsReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? employeeId = null)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var report = await _reportService.GetEmployeeAppointmentsReportAsync(barberId, startDate, endDate, employeeId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reporte de citas por empleado");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener reporte de ingresos por empleado
    /// </summary>
    [HttpGet("reports/employees/income")]
    public async Task<ActionResult<EmployeeIncomeReportDto>> GetEmployeeIncomeReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? employeeId = null)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var report = await _reportService.GetEmployeeIncomeReportAsync(barberId, startDate, endDate, employeeId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reporte de ingresos por empleado");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener reporte de egresos por empleado
    /// </summary>
    [HttpGet("reports/employees/expenses")]
    public async Task<ActionResult<EmployeeExpensesReportDto>> GetEmployeeExpensesReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? employeeId = null)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var report = await _reportService.GetEmployeeExpensesReportAsync(barberId, startDate, endDate, employeeId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reporte de egresos por empleado");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener reporte general de actividad de empleados
    /// </summary>
    [HttpGet("reports/employees/activity")]
    public async Task<ActionResult<EmployeeActivityReportDto>> GetEmployeeActivityReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var barberId = await GetBarberIdAsync();
            var report = await _reportService.GetEmployeeActivityReportAsync(barberId, startDate, endDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reporte de actividad de empleados");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    /// <summary>
    /// Normaliza una fecha para el filtro, asegurando que esté en UTC y maneje correctamente los formatos
    /// </summary>
    private DateTime NormalizeDateForFilter(DateTime date, bool isEndDate)
    {
        // Si la fecha viene sin hora (00:00:00), normalizar según si es inicio o fin
        if (date.TimeOfDay.TotalSeconds < 1)
        {
            if (isEndDate)
            {
                // Fin del día: 23:59:59.999
                return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999, DateTimeKind.Utc);
            }
            else
            {
                // Inicio del día: 00:00:00
                return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        
        // Si tiene hora específica, crear directamente en UTC (asumir que viene en UTC)
        // Extraer componentes para evitar problemas de zona horaria
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, DateTimeKind.Utc);
    }
}

