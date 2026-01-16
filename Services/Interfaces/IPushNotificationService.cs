using GlowNic.Models.Entities;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Servicio para enviar notificaciones push usando Firebase Cloud Messaging
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Envía notificación push usando una plantilla
    /// </summary>
    /// <param name="template">Plantilla con título, cuerpo e imagen</param>
    /// <param name="devices">Lista de dispositivos destino</param>
    /// <param name="extraData">Datos adicionales (opcional)</param>
    /// <param name="dataOnly">Si es true, solo envía datos sin notificación del sistema</param>
    Task SendPushNotificationAsync(
        Template? template, 
        List<Device> devices, 
        IDictionary<string, string>? extraData = null, 
        bool dataOnly = false);
}
