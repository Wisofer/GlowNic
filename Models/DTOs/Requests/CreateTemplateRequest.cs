using System.ComponentModel.DataAnnotations;

namespace GlowNic.Models.DTOs.Requests;

public class CreateTemplateRequest
{
    [Required(ErrorMessage = "El título es requerido")]
    [MaxLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El cuerpo del mensaje es requerido")]
    [MaxLength(500, ErrorMessage = "El cuerpo no puede exceder 500 caracteres")]
    public string Body { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La URL de imagen no puede exceder 500 caracteres")]
    public string? ImageUrl { get; set; }

    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string? Name { get; set; }
}
