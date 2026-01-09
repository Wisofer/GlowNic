using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO para actualizar horarios de trabajo
/// </summary>
public class UpdateWorkingHoursRequest
{
    [Required(ErrorMessage = "El día de la semana es requerido")]
    [Range(0, 6, ErrorMessage = "El día de la semana debe estar entre 0 (Domingo) y 6 (Sábado)")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required(ErrorMessage = "La hora de inicio es requerida")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "La hora de fin es requerida")]
    public TimeOnly EndTime { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO para actualizar múltiples horarios de trabajo
/// </summary>
public class UpdateWorkingHoursBatchRequest
{
    [Required(ErrorMessage = "La lista de horarios es requerida")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un horario")]
    public List<UpdateWorkingHoursRequest> WorkingHours { get; set; } = new();
}

