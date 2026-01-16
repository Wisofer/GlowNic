namespace GlowNic.Models.Entities;

/// <summary>
/// Plantilla de notificación push
/// </summary>
public class Template
{
    public int Id { get; set; }
    
    /// <summary>
    /// Título de la notificación
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Cuerpo del mensaje
    /// </summary>
    public string Body { get; set; } = string.Empty;
    
    /// <summary>
    /// URL de imagen (opcional)
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Nombre descriptivo de la plantilla (opcional)
    /// </summary>
    public string? Name { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
