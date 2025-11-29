using System.Globalization;

namespace Html2x.Abstractions.Layout.Styles;

public readonly record struct ColorRgba(byte R, byte G, byte B, byte A)
{
    public static readonly ColorRgba Black = new(0, 0, 0, 255);
    public static readonly ColorRgba Transparent = new(0, 0, 0, 0);

    private static readonly IReadOnlyDictionary<string, ColorRgba> NamedColors =
        new Dictionary<string, ColorRgba>(StringComparer.OrdinalIgnoreCase)
        {
            ["black"] = Black,
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

    public static ColorRgba FromCss(string? value, ColorRgba fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var trimmed = value.Trim();

        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            return FromHex(trimmed, fallback);
        }

        if (trimmed.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return FromRgbFunction(trimmed, fallback);
        }

        if (string.Equals(trimmed, "transparent", StringComparison.OrdinalIgnoreCase))
        {
            return new ColorRgba(0, 0, 0, 0);
        }

        if (NamedColors.TryGetValue(trimmed, out var named))
        {
            return named;
        }

        return fallback;
    }

    private static ColorRgba FromHex(string hex, ColorRgba fallback)
    {
        var normalized = hex.TrimStart('#');

        if (normalized.Length is not (3 or 4 or 6 or 8))
        {
            return fallback;
        }

        int Expand(char c) => int.Parse(new string(c, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        byte ParsePair(string pair) => byte.Parse(pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return normalized.Length switch
        {
            3 => new ColorRgba((byte)Expand(normalized[0]), (byte)Expand(normalized[1]), (byte)Expand(normalized[2]), 255),
            4 => new ColorRgba((byte)Expand(normalized[0]), (byte)Expand(normalized[1]), (byte)Expand(normalized[2]), (byte)Expand(normalized[3])),
            6 => new ColorRgba(ParsePair(normalized[..2]), ParsePair(normalized.Substring(2, 2)), ParsePair(normalized.Substring(4, 2)), 255),
            8 => new ColorRgba(ParsePair(normalized[..2]), ParsePair(normalized.Substring(2, 2)), ParsePair(normalized.Substring(4, 2)), ParsePair(normalized.Substring(6, 2))),
            _ => fallback
        };
    }

    private static ColorRgba FromRgbFunction(string rgb, ColorRgba fallback)
    {
        var openParen = rgb.IndexOf('(');
        var closeParen = rgb.IndexOf(')');

        if (openParen < 0 || closeParen <= openParen)
        {
            return fallback;
        }

        var parts = rgb.Substring(openParen + 1, closeParen - openParen - 1)
            .Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length is < 3 or > 4)
        {
            return fallback;
        }

        if (!TryParseComponent(parts[0], out var r) ||
            !TryParseComponent(parts[1], out var g) ||
            !TryParseComponent(parts[2], out var b))
        {
            return fallback;
        }

        byte a = 255;
        if (parts.Length == 4)
        {
            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var alpha))
            {
                return fallback;
            }

            a = (byte)(Math.Clamp(alpha, 0f, 1f) * 255);
        }

        return new ColorRgba(r, g, b, a);
    }

    private static bool TryParseComponent(string raw, out byte component)
    {
        component = 0;
        var trimmed = raw.Trim();

        if (trimmed.EndsWith('%'))
        {
            if (!float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            {
                return false;
            }

            component = (byte)(Math.Clamp(percent / 100f, 0f, 1f) * 255);
            return true;
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            component = (byte)Math.Clamp(value, 0, 255);
            return true;
        }

        return false;
    }

    public string ToHex(bool includeAlpha = true)
    {
        return includeAlpha && A != 255
            ? $"#{R:X2}{G:X2}{B:X2}{A:X2}"
            : $"#{R:X2}{G:X2}{B:X2}";
    }
}
