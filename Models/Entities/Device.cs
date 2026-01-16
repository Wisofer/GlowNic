namespace GlowNic.Models.Entities;

/// <summary>
/// Dispositivo registrado para recibir notificaciones push
/// </summary>
public class Device
{
    public int Id { get; set; }
    
    /// <summary>
    /// Token FCM del dispositivo (único)
    /// </summary>
    public string FcmToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Plataforma: "android", "ios", "web", "unknown"
    /// </summary>
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>
    /// Última actividad del dispositivo
    /// </summary>
    public DateTime? LastActiveAt { get; set; }
    
    /// <summary>
    /// ID del usuario propietario (Barbero/Usuario)
    /// </summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
