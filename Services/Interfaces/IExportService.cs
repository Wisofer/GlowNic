namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de exportación de datos
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exportar reporte de citas
    /// </summary>
    Task<byte[]> ExportAppointmentsAsync(int barberId, DateOnly? startDate, DateOnly? endDate, string format);

    /// <summary>
    /// Exportar reporte financiero
    /// </summary>
    Task<byte[]> ExportFinancesAsync(int barberId, DateOnly? startDate, DateOnly? endDate, string format);

    /// <summary>
    /// Exportar reporte de clientes
    /// </summary>
    Task<byte[]> ExportClientsAsync(int barberId, string format);

    /// <summary>
    /// Crear backup completo de datos del salón
    /// </summary>
    Task<byte[]> ExportBackupAsync(int barberId);
}

