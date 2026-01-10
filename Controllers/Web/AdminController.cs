using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GlowNic.Utils;
using GlowNic.Services.Interfaces;
using GlowNic.Models.DTOs.Requests;
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

    public AdminController(
        IDashboardService dashboardService, 
        IBarberService barberService,
        IFinanceService financeService,
        IServiceService serviceService,
        IAppointmentService appointmentService,
        IEmployeeService employeeService,
        IReportService reportService)
    {
        _dashboardService = dashboardService;
        _barberService = barberService;
        _financeService = financeService;
        _serviceService = serviceService;
        _appointmentService = appointmentService;
        _employeeService = employeeService;
        _reportService = reportService;
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
}

public class UpdateBarberStatusRequest
{
    public bool IsActive { get; set; }
}

public class SaveThemeRequest
{
    public string Theme { get; set; } = string.Empty;
}

