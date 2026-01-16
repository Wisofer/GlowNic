using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GlowNic.Utils;
using GlowNic.Services.Interfaces;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GlowNic.Controllers.Web;

[Authorize]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IBarberService _barberService;
    private readonly IFinanceService _financeService;
    private readonly IServiceService _serviceService;
    private readonly IAppointmentService _appointmentService;
    private readonly IEmployeeService _employeeService;
    private readonly IReportService _reportService;
    private readonly ApplicationDbContext _context;
    private readonly IPushNotificationService _pushNotificationService;

    public AdminController(
        IDashboardService dashboardService, 
        IBarberService barberService,
        IFinanceService financeService,
        IServiceService serviceService,
        IAppointmentService appointmentService,
        IEmployeeService employeeService,
        IReportService reportService,
        ApplicationDbContext context,
        IPushNotificationService pushNotificationService)
    {
        _dashboardService = dashboardService;
        _barberService = barberService;
        _financeService = financeService;
        _serviceService = serviceService;
        _appointmentService = appointmentService;
        _employeeService = employeeService;
        _reportService = reportService;
        _context = context;
        _pushNotificationService = pushNotificationService;
    }

    [HttpGet("admin/dashboard")]
    [HttpGet("admin")]
    public async Task<IActionResult> Dashboard()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }
        
        var dashboard = await _dashboardService.GetAdminDashboardAsync();
        ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
        ViewBag.Dashboard = dashboard;
        
        return View();
    }

    [HttpPost("admin/createsalon")]
    public async Task<IActionResult> CreateSalon([FromBody] CreateBarberRequest? request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null)
        {
            return Json(new { success = false, message = "Datos no recibidos" });
        }

        // Validar campos requeridos
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Json(new { success = false, message = "El email es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Json(new { success = false, message = "La contraseña es requerida" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Json(new { success = false, message = "El nombre es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return Json(new { success = false, message = "El teléfono es requerido" });
        }

        try
        {
            var barber = await _barberService.CreateBarberAsync(request);
            return Json(new { 
                success = true, 
                message = "Salón creado exitosamente", 
                barber = new { 
                    id = barber.Id, 
                    name = barber.Name,
                    businessName = barber.BusinessName,
                    phone = barber.Phone
                } 
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al crear el salón: {ex.Message}" });
        }
    }

    [HttpPut("admin/salons/{id}/status")]
    [HttpPost("admin/salons/{id}/status")]
    public async Task<IActionResult> UpdateSalonStatus(int id, [FromBody] UpdateBarberStatusRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var updated = await _barberService.UpdateBarberStatusAsync(id, request.IsActive);
            if (!updated)
            {
                return Json(new { success = false, message = "Salón no encontrado" });
            }

            return Json(new { success = true, message = "Estado actualizado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar estado: {ex.Message}" });
        }
    }

    [HttpPut("admin/salons/{id}")]
    [HttpPost("admin/salons/{id}/update")]
    public async Task<IActionResult> UpdateSalon(int id, [FromBody] UpdateBarberProfileRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null)
        {
            return Json(new { success = false, message = "Datos no recibidos" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Json(new { success = false, message = "El nombre es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return Json(new { success = false, message = "El teléfono es requerido" });
        }

        try
        {
            var barber = await _barberService.UpdateBarberProfileAsync(id, request);
            return Json(new { 
                success = true, 
                message = "Salón actualizado exitosamente",
                barber = new {
                    id = barber.Id,
                    name = barber.Name,
                    businessName = barber.BusinessName,
                    phone = barber.Phone
                }
            });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, message = "Salón no encontrado" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar salón: {ex.Message}" });
        }
    }

    [HttpDelete("admin/salons/{id}")]
    [HttpPost("admin/salons/{id}/delete")]
    public async Task<IActionResult> DeleteSalon(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var deleted = await _barberService.DeleteBarberAsync(id);
            if (!deleted)
            {
                return Json(new { success = false, message = "Salón no encontrado" });
            }

            return Json(new { success = true, message = "Salón eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al eliminar salón: {ex.Message}" });
        }
    }

    [HttpGet("admin/salons/{id}/dashboard")]
    public async Task<IActionResult> GetSalonDashboard(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var dashboard = await _dashboardService.GetBarberDashboardAsync(id);
            return Json(dashboard);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al obtener dashboard: {ex.Message}" });
        }
    }

    [HttpGet("admin/salons/{id}/finances/summary")]
    public async Task<IActionResult> GetSalonFinanceSummary(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var summary = await _financeService.GetFinanceSummaryAsync(id);
            return Json(summary);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al obtener finanzas: {ex.Message}" });
        }
    }

    [HttpGet("admin/salons/{id}/services")]
    public async Task<IActionResult> GetSalonServices(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var services = await _serviceService.GetBarberServicesAsync(id);
            return Json(services);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al obtener servicios: {ex.Message}" });
        }
    }

    [HttpGet("admin/salons/{id}/appointments")]
    public async Task<IActionResult> GetSalonAppointments(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var appointments = await _appointmentService.GetBarberAppointmentsAsync(id);
            return Json(appointments);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al obtener citas: {ex.Message}" });
        }
    }

    [HttpGet("admin/salons")]
    public async Task<IActionResult> Salons()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            ViewBag.Barbers = dashboard.RecentBarbers;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View("Barbers");
        }
        catch (Exception)
        {
            ViewBag.Barbers = new List<GlowNic.Models.DTOs.Responses.BarberSummaryDto>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View("Barbers");
        }
    }

    [HttpGet("admin/employees")]
    public async Task<IActionResult> Employees()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            // Obtener todos los salóns para el filtro
            var barbers = await _barberService.GetAllBarbersAsync();
            
            // Obtener todos los empleados de todos los salóns
            var allEmployees = new List<GlowNic.Models.DTOs.Responses.EmployeeDto>();
            foreach (var barber in barbers)
            {
                try
                {
                    var employees = await _employeeService.GetEmployeesAsync(barber.Id);
                    allEmployees.AddRange(employees);
                }
                catch
                {
                    // Continuar si hay error con un salón específico
                }
            }

            ViewBag.Employees = allEmployees;
            ViewBag.Barbers = barbers;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
        catch (Exception)
        {
            ViewBag.Employees = new List<GlowNic.Models.DTOs.Responses.EmployeeDto>();
            ViewBag.Barbers = new List<GlowNic.Models.DTOs.Responses.BarberDto>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
    }

    [HttpGet("admin/appointments")]
    public async Task<IActionResult> Appointments()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            // Obtener todos los salóns para el filtro
            var barbers = await _barberService.GetAllBarbersAsync();
            
            // Obtener todas las citas de todos los salóns
            var allAppointments = new List<GlowNic.Models.DTOs.Responses.AppointmentDto>();
            foreach (var barber in barbers)
            {
                try
                {
                    var appointments = await _appointmentService.GetBarberAppointmentsAsync(barber.Id);
                    allAppointments.AddRange(appointments);
                }
                catch
                {
                    // Continuar si hay error con un salón específico
                }
            }

            ViewBag.Appointments = allAppointments.OrderByDescending(a => a.Date).ThenByDescending(a => a.Time).ToList();
            ViewBag.Barbers = barbers;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
        catch (Exception)
        {
            ViewBag.Appointments = new List<GlowNic.Models.DTOs.Responses.AppointmentDto>();
            ViewBag.Barbers = new List<GlowNic.Models.DTOs.Responses.BarberDto>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
    }

    [HttpGet("admin/reports")]
    public async Task<IActionResult> Reports()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            var barbers = await _barberService.GetAllBarbersAsync();
            ViewBag.Barbers = barbers;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
        catch (Exception)
        {
            ViewBag.Barbers = new List<GlowNic.Models.DTOs.Responses.BarberDto>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
    }

    [HttpGet("admin/settings")]
    public IActionResult Settings()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
        return View();
    }

    [HttpPost("admin/settings/theme")]
    public IActionResult SaveTheme([FromBody] SaveThemeRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Theme))
        {
            return Json(new { success = false, message = "Tema no válido" });
        }

        // Validar que el tema sea uno de los permitidos
        var temasPermitidos = new[] { "business", "corporate", "night", "luxury" };
        if (!temasPermitidos.Contains(request.Theme.ToLower()))
        {
            return Json(new { success = false, message = "Tema no válido" });
        }

        try
        {
            // Guardar en sesión (opcional, ya que se guarda en localStorage)
            HttpContext.Session.SetString("Tema", request.Theme);
            
            return Json(new { success = true, message = "Tema guardado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al guardar tema: {ex.Message}" });
        }
    }

    #region API para Gráficos

    [HttpGet("admin/api/charts/income")]
    public async Task<IActionResult> GetIncomeChartData()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { labels = new string[0], values = new decimal[0] });
        }

        try
        {
            var labels = new List<string>();
            var values = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                var startDate = date.Date;
                var endDate = startDate.AddDays(1).AddTicks(-1);

                var income = await _financeService.GetIncomeAsync(0, startDate, endDate, 1, 1000);
                var total = income.Items.Sum(t => t.Amount);

                labels.Add(date.ToString("dd/MM"));
                values.Add(total);
            }

            return Json(new { labels, values });
        }
        catch
        {
            return Json(new { labels = new string[0], values = new decimal[0] });
        }
    }

    [HttpGet("admin/api/charts/appointments-status")]
    public async Task<IActionResult> GetAppointmentsStatusChartData()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { labels = new string[0], values = new int[0] });
        }

        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            return Json(new
            {
                labels = new[] { "Pendientes", "Confirmadas", "Completadas", "Canceladas" },
                values = new[]
                {
                    dashboard.PendingAppointments,
                    dashboard.ConfirmedAppointments,
                    dashboard.TotalAppointments - dashboard.PendingAppointments - dashboard.ConfirmedAppointments - dashboard.CancelledAppointments,
                    dashboard.CancelledAppointments
                }
            });
        }
        catch
        {
            return Json(new { labels = new string[0], values = new int[0] });
        }
    }

    [HttpGet("admin/api/charts/top-salons")]
    public async Task<IActionResult> GetTopSalonsChartData()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { labels = new string[0], values = new decimal[0] });
        }

        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            var topBarbers = dashboard.RecentBarbers
                .OrderByDescending(b => b.TotalRevenue)
                .Take(5)
                .ToList();

            return Json(new
            {
                labels = topBarbers.Select(b => b.Name).ToArray(),
                values = topBarbers.Select(b => b.TotalRevenue).ToArray()
            });
        }
        catch
        {
            return Json(new { labels = new string[0], values = new decimal[0] });
        }
    }

    [HttpGet("admin/api/charts/appointments-by-day")]
    public async Task<IActionResult> GetAppointmentsByDayChartData()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { labels = new string[0], values = new int[0] });
        }

        try
        {
            var labels = new List<string>();
            var values = new List<int>();

            // Obtener todos los salóns
            var barbers = await _barberService.GetAllBarbersAsync();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                var dateOnly = DateOnly.FromDateTime(date);

                int totalCount = 0;
                foreach (var barber in barbers)
                {
                    var appointments = await _appointmentService.GetBarberAppointmentsAsync(barber.Id, dateOnly, null);
                    totalCount += appointments.Count;
                }

                labels.Add(date.ToString("dd/MM"));
                values.Add(totalCount);
            }

            return Json(new { labels, values });
        }
        catch
        {
            return Json(new { labels = new string[0], values = new int[0] });
        }
    }

    #endregion

    #region Notificaciones Push

    [HttpGet("admin/notifications")]
    public IActionResult Notifications()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }
        
        ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
        return View();
    }

    [HttpGet("admin/notifications/templates")]
    public async Task<IActionResult> GetTemplates()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

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

            return Json(templates);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost("admin/notifications/templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Datos inválidos" });
        }

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

            var templateDto = new TemplateDto
            {
                Id = template.Id,
                Title = template.Title,
                Body = template.Body,
                ImageUrl = template.ImageUrl,
                Name = template.Name,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };

            return Json(templateDto);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPut("admin/notifications/templates/{id}")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] CreateTemplateRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Datos inválidos" });
        }

        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Plantilla no encontrada" });
            }

            template.Title = request.Title;
            template.Body = request.Body;
            template.ImageUrl = request.ImageUrl;
            template.Name = request.Name;
            template.UpdatedAt = DateTime.UtcNow;

            _context.Templates.Update(template);
            await _context.SaveChangesAsync();

            var templateDto = new TemplateDto
            {
                Id = template.Id,
                Title = template.Title,
                Body = template.Body,
                ImageUrl = template.ImageUrl,
                Name = template.Name,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };

            return Json(templateDto);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpDelete("admin/notifications/templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Plantilla no encontrada" });
            }

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost("admin/notifications/send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            Template? template = null;

            // Si se proporciona TemplateId, obtener la plantilla
            if (request.TemplateId.HasValue)
            {
                template = await _context.Templates.FindAsync(request.TemplateId.Value);
                if (template == null)
                {
                    return Json(new { success = false, message = "Plantilla no encontrada" });
                }
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
                return Json(new { success = false, message = "Debe proporcionar TemplateId o Title y Body" });
            }

            // Obtener dispositivos
            List<Device> devices;
            if (request.UserIds != null && request.UserIds.Any())
            {
                // Convertir IDs de salones a UserIds
                // request.UserIds contiene IDs de salones (BarberId), necesitamos obtener los UserIds
                var barbers = await _context.Barbers
                    .Where(b => request.UserIds.Contains(b.Id))
                    .Select(b => b.UserId)
                    .ToListAsync();
                
                // Dispositivos de usuarios específicos
                devices = await _context.Devices
                    .Where(d => barbers.Contains(d.UserId) && !string.IsNullOrWhiteSpace(d.FcmToken))
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
            {
                return Json(new { success = false, message = "No hay dispositivos registrados para enviar notificaciones" });
            }

            // Enviar notificación
            await _pushNotificationService.SendPushNotificationAsync(
                template,
                devices,
                request.ExtraData,
                request.DataOnly);

            return Json(new SendNotificationResponse
            {
                Success = true,
                Message = "Notificación enviada exitosamente",
                UserCount = devices.Select(d => d.UserId).Distinct().Count(),
                SentCount = devices.Count
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpGet("admin/notifications/logs")]
    public async Task<IActionResult> GetNotificationLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var logs = await _context.NotificationLogs
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

            return Json(logs);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpGet("admin/notifications/salons")]
    public async Task<IActionResult> GetSalonsForNotifications()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            // Obtener solo salones que tienen dispositivos con token FCM registrado
            var salonsWithDevices = await _context.Barbers
                .Where(b => b.UserId != null && 
                           _context.Devices.Any(d => d.UserId == b.UserId && 
                                                    !string.IsNullOrWhiteSpace(d.FcmToken)))
                .Select(b => new { 
                    id = b.Id, 
                    name = b.Name,
                    businessName = b.BusinessName ?? "Sin nombre de negocio",
                    deviceCount = _context.Devices.Count(d => d.UserId == b.UserId && 
                                                             !string.IsNullOrWhiteSpace(d.FcmToken))
                })
                .OrderBy(s => s.name)
                .ToListAsync();

            return Json(salonsWithDevices);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    #endregion
}

public class UpdateBarberStatusRequest
{
    public bool IsActive { get; set; }
}

public class SaveThemeRequest
{
    public string Theme { get; set; } = string.Empty;
}

