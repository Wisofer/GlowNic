using GlowNic.Models.DTOs.Responses;

namespace GlowNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de ayuda y soporte
/// </summary>
public interface IHelpSupportService
{
    /// <summary>
    /// Obtener información de ayuda y soporte
    /// </summary>
    Task<HelpSupportDto> GetHelpSupportAsync();
}

