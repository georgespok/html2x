using System;
using System.Globalization;

namespace Html2x.Abstractions.Layout;

public readonly record struct ColorRgba(byte R, byte G, byte B, byte A)
{
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
}
