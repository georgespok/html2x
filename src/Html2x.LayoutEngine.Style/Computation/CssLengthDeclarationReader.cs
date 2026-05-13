using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Style.Computation;

internal sealed class CssLengthDeclarationReader(CssValueParser parser)
{
    private readonly CssValueParser _parser = parser ?? throw new ArgumentNullException(nameof(parser));

    public string? GetValue(ICssStyleDeclaration css, IElement element, string property) =>
        AuthoredCssDeclarationReader.GetValue(css, element, property);

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
            StyleDiagnosticEmitter.EmitUnsupportedDeclaration(
                diagnosticsSink,
                element,
                property,
                trimmed,
                $"Unsupported unit '{unsupportedUnit}' for {property}.");
            return false;
        }

        var parsed = _parser.ParseLengthPt(trimmed);
        if (!parsed.HasValue)
        {
            StyleDiagnosticEmitter.EmitIgnoredDeclaration(
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
