using GlowNic.Data;
using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;
using GlowNic.Models.Entities;
using GlowNic.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace GlowNic.Services.Implementations;

/// <summary>
/// Servicio para gestión de finanzas
/// </summary>
public class FinanceService : IFinanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FinanceService> _logger;

    public FinanceService(ApplicationDbContext context, ILogger<FinanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FinanceSummaryDto> GetFinanceSummaryAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var query = _context.Transactions.Where(t => t.BarberId == barberId);
        query = ApplyDateFilters(query, startDate, endDate);

        var allTransactions = await query.ToListAsync();
        var monthTransactions = await _context.Transactions
            .Where(t => t.BarberId == barberId && t.Date >= startOfMonth && t.Date <= endOfMonth)
            .ToListAsync();

        return new FinanceSummaryDto
        {
            TotalIncome = allTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            TotalExpenses = allTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            NetProfit = allTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount) -
                       allTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            IncomeThisMonth = monthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            ExpensesThisMonth = monthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            ProfitThisMonth = monthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount) -
                             monthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
        };
    }

    public async Task<TransactionsResponse> GetIncomeAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Transactions
            .Include(t => t.Employee)
            .Where(t => t.BarberId == barberId && t.Type == TransactionType.Income);

        query = ApplyDateFilters(query, startDate, endDate);

        var total = await query.SumAsync(t => t.Amount);
        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                Description = t.Description,
                Category = t.Category,
                Date = t.Date,
                AppointmentId = t.AppointmentId,
                EmployeeId = t.EmployeeId,
                EmployeeName = t.Employee != null ? t.Employee.Name : null
            })
            .ToListAsync();

        return new TransactionsResponse
        {
            Total = total,
            Items = items
        };
    }

    public async Task<TransactionsResponse> GetExpensesAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Transactions
            .Include(t => t.Employee)
            .Where(t => t.BarberId == barberId && t.Type == TransactionType.Expense);

        query = ApplyDateFilters(query, startDate, endDate);

        var total = await query.SumAsync(t => t.Amount);
        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                Description = t.Description,
                Category = t.Category,
                Date = t.Date,
                AppointmentId = t.AppointmentId,
                EmployeeId = t.EmployeeId,
                EmployeeName = t.Employee != null ? t.Employee.Name : null
            })
            .ToListAsync();

        return new TransactionsResponse
        {
            Total = total,
            Items = items
        };
    }

    public async Task<TransactionDto> CreateExpenseAsync(int barberId, CreateExpenseRequest request, int? employeeId = null)
    {
        var transaction = new Transaction
        {
            BarberId = barberId,
            EmployeeId = employeeId,
            Type = TransactionType.Expense,
            Amount = request.Amount,
            Description = request.Description,
            Category = request.Category,
            Date = request.Date
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        await LoadEmployeeIfExistsAsync(transaction, employeeId);

        return MapToTransactionDto(transaction);
    }

    public async Task<TransactionDto> UpdateExpenseAsync(int barberId, int expenseId, UpdateExpenseRequest request)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == expenseId && t.BarberId == barberId && t.Type == TransactionType.Expense);

        if (transaction == null)
            throw new KeyNotFoundException("Egreso no encontrado o no pertenece al salón");

        transaction.Amount = request.Amount;
        transaction.Description = request.Description;
        transaction.Category = request.Category;
        transaction.Date = request.Date;

        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();

        return new TransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            Amount = transaction.Amount,
            Description = transaction.Description,
            Category = transaction.Category,
            Date = transaction.Date,
            AppointmentId = transaction.AppointmentId
        };
    }

    public async Task<bool> DeleteExpenseAsync(int barberId, int expenseId)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == expenseId && t.BarberId == barberId && t.Type == TransactionType.Expense);

        if (transaction == null)
            return false;

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<List<string>> GetCategoriesAsync()
    {
        // Categorías predefinidas para ingresos y egresos
        var categories = new List<string>
        {
            "Alquiler",
            "Servicios Públicos",
            "Materiales",
            "Salarios",
            "Marketing",
            "Service",
            "Otros"
        };
        return Task.FromResult(categories);
    }

    public async Task<TransactionDto> CreateIncomeAsync(int barberId, CreateIncomeRequest request, int? employeeId = null)
    {
        var transaction = new Transaction
        {
            BarberId = barberId,
            EmployeeId = employeeId,
            Type = TransactionType.Income,
            Amount = request.Amount,
            Description = request.Description,
            Category = request.Category ?? "Service",
            Date = request.Date,
            AppointmentId = null // Ingreso manual, no viene de cita
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        await LoadEmployeeIfExistsAsync(transaction, employeeId);

        return MapToTransactionDto(transaction);
    }

    public async Task CreateIncomeFromAppointmentAsync(int barberId, int appointmentId, decimal amount, string description)
    {
        // Verificar que no exista ya una transacción para esta cita
        var exists = await _context.Transactions
            .AnyAsync(t => t.BarberId == barberId && t.AppointmentId == appointmentId);

        if (exists)
            return; // Ya existe, no crear duplicado

        var transaction = new Transaction
        {
            BarberId = barberId,
            Type = TransactionType.Income,
            Amount = amount,
            Description = description,
            Category = "Service",
            Date = DateTime.UtcNow,
            AppointmentId = appointmentId
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task CreateMultipleIncomesFromAppointmentAsync(int barberId, int appointmentId, List<(int ServiceId, string ServiceName, decimal Price)> services, string clientName)
    {
        if (services == null || services.Count == 0)
            return;

        // Verificar que no existan transacciones para esta cita con estos servicios
        var existingServiceIds = await _context.Transactions
            .Where(t => t.BarberId == barberId && t.AppointmentId == appointmentId)
            .Select(t => t.Description)
            .ToListAsync();

        // Crear una transacción por cada servicio
        foreach (var (serviceId, serviceName, price) in services)
        {
            var description = $"Cita - {serviceName} - {clientName}";
            
            // Verificar si ya existe una transacción con esta descripción para esta cita
            if (existingServiceIds.Contains(description))
                continue; // Ya existe, no crear duplicado

            var transaction = new Transaction
            {
                BarberId = barberId,
                Type = TransactionType.Income,
                Amount = price,
                Description = description,
                Category = "Service",
                Date = DateTime.UtcNow,
                AppointmentId = appointmentId
            };

            _context.Transactions.Add(transaction);
        }

        await _context.SaveChangesAsync();
    }

    #region Private Helper Methods

    /// <summary>
    /// Aplica filtros de fecha a una consulta de transacciones
    /// </summary>
    private IQueryable<Transaction> ApplyDateFilters(IQueryable<Transaction> query, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            var normalizedStart = NormalizeStartDate(startDate.Value);
            query = query.Where(t => t.Date >= normalizedStart);
        }
        
        if (endDate.HasValue)
        {
            var normalizedEnd = NormalizeEndDate(endDate.Value);
            var nextDayStart = CalculateNextDayStart(normalizedEnd);
            query = query.Where(t => t.Date < nextDayStart);
        }

        return query;
    }

    /// <summary>
    /// Calcula el inicio del día siguiente para usar como límite superior en filtros de fecha
    /// </summary>
    private DateTime CalculateNextDayStart(DateTime endDate)
    {
        var endDateOnly = new DateTime(endDate.Year, endDate.Month, endDate.Day, 0, 0, 0, DateTimeKind.Utc);
        return endDateOnly.AddDays(1);
    }

    /// <summary>
    /// Mapea una entidad Transaction a TransactionDto
    /// </summary>
    private static TransactionDto MapToTransactionDto(Transaction t)
    {
        return new TransactionDto
        {
            Id = t.Id,
            Type = t.Type.ToString(),
            Amount = t.Amount,
            Description = t.Description,
            Category = t.Category,
            Date = t.Date,
            AppointmentId = t.AppointmentId,
            EmployeeId = t.EmployeeId,
            EmployeeName = t.Employee != null ? t.Employee.Name : null
        };
    }

    /// <summary>
    /// Carga el Employee asociado a una transacción si existe
    /// </summary>
    private async Task LoadEmployeeIfExistsAsync(Transaction transaction, int? employeeId)
    {
        if (employeeId.HasValue)
        {
            await _context.Entry(transaction)
                .Reference(t => t.Employee)
                .LoadAsync();
        }
    }

    /// <summary>
    /// Normaliza la fecha de inicio: si viene sin hora, la establece a 00:00:00 del día
    /// </summary>
    private DateTime NormalizeStartDate(DateTime date)
    {
        // Si la fecha tiene hora 00:00:00 (o muy cercana), asumir que es solo fecha
        var timeOfDay = date.TimeOfDay;
        if (timeOfDay.TotalSeconds < 1)
        {
            // Extraer solo la parte de fecha (año, mes, día) en UTC
            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        }
        
        // Si tiene hora específica, crear directamente en UTC sin convertir
        // Esto evita problemas de zona horaria cuando viene sin zona horaria especificada
        // Extraer componentes y crear nuevo DateTime en UTC (sin conversión)
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, DateTimeKind.Utc);
    }

    /// <summary>
    /// Normaliza la fecha de fin: si viene sin hora, la establece a 23:59:59 del día
    /// </summary>
    private DateTime NormalizeEndDate(DateTime date)
    {
        // Si la fecha tiene hora 00:00:00 (o muy cercana), asumir que es solo fecha
        var timeOfDay = date.TimeOfDay;
        if (timeOfDay.TotalSeconds < 1)
        {
            // Extraer solo la parte de fecha y establecer al final del día en UTC
            return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999, DateTimeKind.Utc);
        }
        
        // Si tiene hora específica, crear directamente en UTC sin convertir
        // Esto evita problemas de zona horaria cuando viene sin zona horaria especificada
        // Extraer componentes y crear nuevo DateTime en UTC (sin conversión)
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, DateTimeKind.Utc);
    }

    #endregion
}

