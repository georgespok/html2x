namespace Html2x.LayoutEngine.Style;

internal static class CssLengthUnitClassifier
{
    private static readonly string[] UnsupportedTwoCharacterUnits =
    [
        "em",
        "in",
        "cm",
        "mm",
        "vh",
        "vw",
        "ex",
        "ch",
        "pc"
    ];

    public static string? DetectUnsupportedUnit(string trimmed)
    {
        if (HasSupportedUnit(trimmed))
        {
            return null;
        }

        if (trimmed.EndsWith("%", StringComparison.Ordinal))
        {
            return "%";
        }

        if (trimmed.Length < 3)
        {
            return null;
        }

        var lastTwo = trimmed[^2..].ToLowerInvariant();
        if (UnsupportedTwoCharacterUnits.Contains(lastTwo, StringComparer.Ordinal))
        {
            return lastTwo;
        }

        if (trimmed.Length >= 4)
        {
            var lastThree = trimmed[^3..].ToLowerInvariant();
            if (lastThree == "rem")
            {
                return lastThree;
            }
        }

        return null;
    }

    private static bool HasSupportedUnit(string trimmed)
    {
        return trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase) ||
               trimmed.EndsWith("pt", StringComparison.OrdinalIgnoreCase);
    }
}
