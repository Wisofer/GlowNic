using System.Text.Json;
using GlowNic.Models.Entities;

namespace GlowNic.Utils;

public static class Helpers
{
    public static Usuario? ObtenerUsuarioActual(ISession session)
    {
        var usuarioJson = session.GetString("UsuarioActual");
        if (string.IsNullOrEmpty(usuarioJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Usuario>(usuarioJson);
    }

    public static bool EsAdministrador(ISession session)
    {
        var rol = session.GetString("RolUsuario");
        return rol == SD.RolAdministrador;
    }

    public static bool EsUsuarioNormal(ISession session)
    {
        var rol = session.GetString("RolUsuario");
        return rol == SD.RolNormal;
    }

    /// <summary>
    /// Normaliza un texto removiendo acentos y caracteres especiales para búsquedas.
    /// Convierte: á→a, é→e, í→i, ó→o, ú→u, ñ→n, y a minúsculas.
    /// </summary>
    public static string NormalizarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        // Convertir a minúsculas y normalizar
        var textoNormalizado = texto.ToLower().Trim();

        // Reemplazar caracteres especiales
        var caracteres = textoNormalizado.ToCharArray();
        var resultado = new System.Text.StringBuilder(textoNormalizado.Length);

        foreach (var c in caracteres)
        {
            switch (c)
            {
                // Vocales con acento agudo
                case 'á': resultado.Append('a'); break;
                case 'é': resultado.Append('e'); break;
                case 'í': resultado.Append('i'); break;
                case 'ó': resultado.Append('o'); break;
                case 'ú': resultado.Append('u'); break;
                // Vocales con acento grave
                case 'à': resultado.Append('a'); break;
                case 'è': resultado.Append('e'); break;
                case 'ì': resultado.Append('i'); break;
                case 'ò': resultado.Append('o'); break;
                case 'ù': resultado.Append('u'); break;
                // Vocales con acento circunflejo
                case 'â': resultado.Append('a'); break;
                case 'ê': resultado.Append('e'); break;
                case 'î': resultado.Append('i'); break;
                case 'ô': resultado.Append('o'); break;
                case 'û': resultado.Append('u'); break;
                // Vocales con diéresis
                case 'ä': resultado.Append('a'); break;
                case 'ë': resultado.Append('e'); break;
                case 'ï': resultado.Append('i'); break;
                case 'ö': resultado.Append('o'); break;
                case 'ü': resultado.Append('u'); break;
                // Ñ
                case 'ñ': resultado.Append('n'); break;
                // Otros caracteres especiales comunes
                case 'ç': resultado.Append('c'); break;
                default:
                    // Mantener el carácter si no es especial
                    resultado.Append(c);
                    break;
            }
        }

        return resultado.ToString();
    }

    /// <summary>
    /// Formatea un precio con separador de miles y siempre 2 decimales (estándar contable).
    /// Ejemplos: 500 → "500.00", 500.50 → "500.50", 1500 → "1,500.00", 15000 → "15,000.00"
    /// </summary>
    public static string FormatearPrecio(decimal precio)
    {
        // Formato estándar: separador de miles (coma) y siempre 2 decimales (punto)
        return precio.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formatea un precio con el prefijo "C$ " con separador de miles y siempre 2 decimales.
    /// Ejemplos: 500 → "C$ 500.00", 500.50 → "C$ 500.50", 1500 → "C$ 1,500.00"
    /// </summary>
    public static string FormatearPrecioConSimbolo(decimal precio)
    {
        return $"C$ {FormatearPrecio(precio)}";
    }

    /// <summary>
    /// Formatea un precio en dólares con separador de miles y siempre 2 decimales.
    /// Ejemplos: 500 → "$ 500.00", 500.50 → "$ 500.50", 1500 → "$ 1,500.00"
    /// </summary>
    public static string FormatearDolares(decimal dolares)
    {
        return $"$ {FormatearPrecio(dolares)}";
    }
}

