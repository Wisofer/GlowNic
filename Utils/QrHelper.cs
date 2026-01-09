using Microsoft.Extensions.Configuration;
using QRCoder;
using System.Text;

namespace GlowNic.Utils;

/// <summary>
/// Helper para generar códigos QR
/// </summary>
public static class QrHelper
{
    /// <summary>
    /// Genera la URL pública del salón (vista web para clientes)
    /// </summary>
    public static string GenerateBarberUrl(string slug, IConfiguration? configuration = null)
    {
        // Si se pasa configuración, usar la URL del appsettings.json
        if (configuration != null)
        {
            var baseUrl = configuration["AppSettings:PublicBarberBaseUrl"] 
                ?? "https://barbepro.encuentrame.org";
            return $"{baseUrl}/b/{slug}";
        }
        
        // Fallback por defecto - apunta a la vista web bonita
        return $"https://barbepro.encuentrame.org/b/{slug}";
    }

    /// <summary>
    /// Genera un código QR en formato Base64 (imagen PNG)
    /// </summary>
    public static string GenerateQrCodeBase64(string url, int pixelsPerModule = 20)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL no puede estar vacía", nameof(url));

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
        
        return Convert.ToBase64String(qrCodeBytes);
    }
}

