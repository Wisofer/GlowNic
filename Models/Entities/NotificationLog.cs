namespace GlowNic.Models.Entities;

/// <summary>
/// Registro de notificaciones push enviadas
/// </summary>
public class NotificationLog
{
    public int Id { get; set; }
    
    /// <summary>
    /// Estado: "sent", "opened", "failed"
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Payload JSON enviado (opcional)
    /// </summary>
    public string? Payload { get; set; }
    
    /// <summary>
    /// Fecha de envío
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Dispositivo destino (opcional)
    /// </summary>
    public int? DeviceId { get; set; }
    public Device? Device { get; set; }
    
    /// <summary>
    /// Plantilla usada (opcional)
    /// </summary>
    public int? TemplateId { get; set; }
    public Template? Template { get; set; }
    
    /// <summary>
    /// ID del usuario destino
    /// </summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
