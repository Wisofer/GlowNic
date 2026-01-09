using System.Globalization;

namespace GlowNic.Utils;

public static class ControllerHelper
{
    /// <summary>
    /// Normaliza un valor decimal desde un formulario.
    /// Convierte coma a punto y parsea usando InvariantCulture para evitar problemas de localización.
    /// </summary>
    /// <param name="rawValue">Valor crudo del formulario (puede tener coma o punto como separador decimal)</param>
    /// <returns>Valor decimal normalizado o null si no se puede parsear</returns>
    public static decimal? ParseDecimalFromForm(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        // Reemplazar coma por punto (normalizar separador decimal)
        // No eliminamos puntos porque no usamos separador de miles en los inputs
        var normalized = rawValue.Trim().Replace(",", ".");

        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Normaliza un valor decimal desde un formulario usando el nombre del campo.
    /// Útil cuando se tiene acceso directo a Request.Form.
    /// </summary>
    /// <param name="requestForm">Request.Form del HttpContext</param>
    /// <param name="fieldName">Nombre del campo en el formulario</param>
    /// <returns>Valor decimal normalizado o null si no se puede parsear</returns>
    public static decimal? ParseDecimalFromForm(Microsoft.AspNetCore.Http.IFormCollection requestForm, string fieldName)
    {
        if (!requestForm.ContainsKey(fieldName))
        {
            return null;
        }

        var rawValue = requestForm[fieldName].ToString();
        return ParseDecimalFromForm(rawValue);
    }
}

