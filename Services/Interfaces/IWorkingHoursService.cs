using GlowNic.Models.DTOs.Requests;
using GlowNic.Models.DTOs.Responses;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de horarios de trabajo
/// </summary>
public interface IWorkingHoursService
{
    /// <summary>
    /// Obtener todos los horarios de trabajo del salón
    /// </summary>
    Task<List<WorkingHoursDto>> GetWorkingHoursAsync(int barberId);

    /// <summary>
    /// Actualizar o crear horarios de trabajo (upsert)
    /// Si el horario para ese día ya existe, lo actualiza; si no, lo crea
    /// </summary>
    Task<List<WorkingHoursDto>> UpdateWorkingHoursAsync(int barberId, List<UpdateWorkingHoursRequest> workingHours);

    /// <summary>
    /// Eliminar un horario de trabajo específico
    /// </summary>
    Task<bool> DeleteWorkingHoursAsync(int barberId, int workingHoursId);
}

