namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de información de ayuda y soporte
/// </summary>
public class HelpSupportDto
{
    public ContactInfoDto Contact { get; set; } = new();
    public List<FaqDto> Faqs { get; set; } = new();
}

/// <summary>
/// DTO de información de contacto
/// </summary>
public class ContactInfoDto
{
    public string Email { get; set; } = string.Empty;
    public List<string> Phones { get; set; } = new();
    public string? Website { get; set; }
}

/// <summary>
/// DTO de pregunta frecuente
/// </summary>
public class FaqDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int Order { get; set; }
}

