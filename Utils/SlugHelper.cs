using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GlowNic.Utils;

/// <summary>
/// Helper para generar slugs únicos
/// </summary>
public static class SlugHelper
{
    /// <summary>
    /// Genera un slug a partir de un texto
    /// </summary>
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convertir a minúsculas
        var slug = text.ToLowerInvariant();

        // Remover acentos
        slug = RemoveAccents(slug);

        // Reemplazar espacios y caracteres especiales con guiones
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug;
    }

    /// <summary>
    /// Genera un slug único agregando un número si es necesario
    /// </summary>
    public static string GenerateUniqueSlug(string baseSlug, Func<string, bool> existsCheck)
    {
        var slug = GenerateSlug(baseSlug);
        var uniqueSlug = slug;
        var counter = 1;

        while (existsCheck(uniqueSlug))
        {
            uniqueSlug = $"{slug}-{counter}";
            counter++;
        }

        return uniqueSlug;
    }

    /// <summary>
    /// Remueve acentos de un texto
    /// </summary>
    private static string RemoveAccents(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = char.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}

