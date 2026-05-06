using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Style.Style;

internal sealed class CssLengthDeclarationReader(CssValueConverter converter)
{
    private readonly CssValueConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));

    public string? GetValue(ICssStyleDeclaration css, IElement element, string property) =>
        InlineStyleSource.GetValue(css, element, property);

    public bool TryParseLengthToken(
        string rawValue,
        IElement element,
        string property,
        string parseFailureReason,
        IDiagnosticsSink? diagnosticsSink,
        out float points)
    {
        points = 0f;

        var trimmed = rawValue.Trim();
        var unsupportedUnit = CssLengthUnitClassifier.DetectUnsupportedUnit(trimmed);
        if (unsupportedUnit != null)
        {
            StyleDiagnostics.EmitUnsupportedDeclaration(
                diagnosticsSink,
                element,
                property,
                trimmed,
                $"Unsupported unit '{unsupportedUnit}' for {property}.");
            return false;
        }

        var parsed = _converter.ParseLengthPt(trimmed);
        if (!parsed.HasValue)
        {
            StyleDiagnostics.EmitIgnoredDeclaration(
                diagnosticsSink,
                element,
                property,
                trimmed,
                null,
                parseFailureReason);
            return false;
        }

        points = parsed.Value;
        return true;
    }
}