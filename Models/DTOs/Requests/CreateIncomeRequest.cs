using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

/// <summary>
/// DTO para crear un ingreso manual
/// </summary>
public class CreateIncomeRequest
{
    [Required(ErrorMessage = "El monto es requerido")]
    [Range(0.01, 999999.99, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "La descripción es requerida")]
    [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "La categoría no puede exceder 100 caracteres")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "La fecha es requerida")]
    public DateTime Date { get; set; }
}

