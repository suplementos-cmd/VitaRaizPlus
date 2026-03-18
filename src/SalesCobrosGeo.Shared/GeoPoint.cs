using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SalesCobrosGeo.Shared;

/// <summary>
/// Represents a validated geographic coordinate pair (latitude, longitude).
/// Replaces unstructured strings like "19.4326,-99.1332" throughout the codebase.
/// </summary>
public readonly record struct GeoPoint(double Lat, double Lng)
{
    /// <summary>Formats the point as "lat,lng" suitable for storage and URL use.</summary>
    public override string ToString() => FormattableString.Invariant($"{Lat:F6},{Lng:F6}");

    /// <summary>Returns a Google Maps URL for this point.</summary>
    public string ToMapUrl() => $"https://maps.google.com/?q={ToString()}";

    /// <summary>
    /// Parses a "lat,lng" string into a <see cref="GeoPoint"/>.
    /// Returns true only if both values are in valid geographic ranges.
    /// </summary>
    public static bool TryParse(
        [NotNullWhen(true)] string? raw,
        out GeoPoint point)
    {
        point = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var parts = raw.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
        {
            return false;
        }

        if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
        {
            return false;
        }

        point = new GeoPoint(lat, lng);
        return true;
    }

    /// <summary>
    /// Parses a "lat,lng" string, throwing <see cref="FormatException"/> if invalid.
    /// </summary>
    public static GeoPoint Parse(string raw)
    {
        if (!TryParse(raw, out var point))
        {
            throw new FormatException($"'{raw}' no es un par lat,lng válido (ej: 19.4326,-99.1332).");
        }

        return point;
    }
}
