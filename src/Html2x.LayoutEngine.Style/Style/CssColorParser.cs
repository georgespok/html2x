using System.Globalization;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Style.Style;

internal static class CssColorParser
{
    private static readonly IReadOnlyDictionary<string, ColorRgba> NamedColors =
        new Dictionary<string, ColorRgba>(StringComparer.OrdinalIgnoreCase)
        {
            ["black"] = ColorRgba.Black,
            ["silver"] = new(0xC0, 0xC0, 0xC0, 0xFF),
            ["gray"] = new(0x80, 0x80, 0x80, 0xFF),
            ["white"] = new(0xFF, 0xFF, 0xFF, 0xFF),
            ["maroon"] = new(0x80, 0x00, 0x00, 0xFF),
            ["red"] = new(0xFF, 0x00, 0x00, 0xFF),
            ["purple"] = new(0x80, 0x00, 0x80, 0xFF),
            ["fuchsia"] = new(0xFF, 0x00, 0xFF, 0xFF),
            ["green"] = new(0x00, 0x80, 0x00, 0xFF),
            ["lime"] = new(0x00, 0xFF, 0x00, 0xFF),
            ["olive"] = new(0x80, 0x80, 0x00, 0xFF),
            ["yellow"] = new(0xFF, 0xFF, 0x00, 0xFF),
            ["navy"] = new(0x00, 0x00, 0x80, 0xFF),
            ["blue"] = new(0x00, 0x00, 0xFF, 0xFF),
            ["teal"] = new(0x00, 0x80, 0x80, 0xFF),
            ["aqua"] = new(0x00, 0xFF, 0xFF, 0xFF)
        };

    public static ColorRgba Parse(string? value, ColorRgba fallback)
    {
        return TryParse(value, out var color)
            ? color
            : fallback;
    }

    public static bool TryParse(string? value, out ColorRgba color)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            color = default;
            return false;
        }

        var trimmed = value.Trim();

        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            return TryParseHex(trimmed, out color);
        }

        if (trimmed.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseRgbFunction(trimmed, out color);
        }

        if (string.Equals(trimmed, "transparent", StringComparison.OrdinalIgnoreCase))
        {
            color = ColorRgba.Transparent;
            return true;
        }

        if (NamedColors.TryGetValue(trimmed, out var named))
        {
            color = named;
            return true;
        }

        color = default;
        return false;
    }

    private static bool TryParseHex(string hex, out ColorRgba color)
    {
        var normalized = hex.TrimStart('#');

        if (normalized.Length is not (3 or 4 or 6 or 8))
        {
            color = default;
            return false;
        }

        static bool TryExpand(char c, out byte value)
        {
            return byte.TryParse(
                new string(c, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out value);
        }

        static bool TryParsePair(string pair, out byte value)
        {
            return byte.TryParse(pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        if (normalized.Length is 3 or 4)
        {
            if (!TryExpand(normalized[0], out var r) ||
                !TryExpand(normalized[1], out var g) ||
                !TryExpand(normalized[2], out var b))
            {
                color = default;
                return false;
            }

            if (normalized.Length == 3)
            {
                color = new ColorRgba(r, g, b, 255);
                return true;
            }

            if (!TryExpand(normalized[3], out var a))
            {
                color = default;
                return false;
            }

            color = new ColorRgba(r, g, b, a);
            return true;
        }

        if (!TryParsePair(normalized[..2], out var red) ||
            !TryParsePair(normalized.Substring(2, 2), out var green) ||
            !TryParsePair(normalized.Substring(4, 2), out var blue))
        {
            color = default;
            return false;
        }

        if (normalized.Length == 6)
        {
            color = new ColorRgba(red, green, blue, 255);
            return true;
        }

        if (!TryParsePair(normalized.Substring(6, 2), out var alpha))
        {
            color = default;
            return false;
        }

        color = new ColorRgba(red, green, blue, alpha);
        return true;
    }

    private static bool TryParseRgbFunction(string rgb, out ColorRgba color)
    {
        var openParen = rgb.IndexOf('(');
        var closeParen = rgb.IndexOf(')');

        if (openParen < 0 || closeParen <= openParen)
        {
            color = default;
            return false;
        }

        var parts = rgb.Substring(openParen + 1, closeParen - openParen - 1)
            .Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length is < 3 or > 4)
        {
            color = default;
            return false;
        }

        var r = ParseComponent(parts[0]);
        var g = ParseComponent(parts[1]);
        var b = ParseComponent(parts[2]);
        if (!r.HasValue || !g.HasValue || !b.HasValue)
        {
            color = default;
            return false;
        }

        byte a = 255;
        if (parts.Length == 4)
        {
            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var alpha))
            {
                color = default;
                return false;
            }

            a = (byte)(Math.Clamp(alpha, 0f, 1f) * 255);
        }

        color = new ColorRgba(r.Value, g.Value, b.Value, a);
        return true;
    }

    private static byte? ParseComponent(string raw)
    {
        var trimmed = raw.Trim();

        if (trimmed.EndsWith('%'))
        {
            if (!float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            {
                return null;
            }

            return (byte)Math.Round(Math.Clamp(percent / 100f, 0f, 1f) * 255);
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return (byte)Math.Clamp(value, 0, 255);
        }

        return null;
    }
}
