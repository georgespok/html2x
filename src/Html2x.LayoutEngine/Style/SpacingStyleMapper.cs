using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Maps CSS margin and padding declarations into layout spacing values and diagnostics.
/// </summary>
internal sealed class SpacingStyleMapper(ICssValueConverter converter)
{
    private readonly ICssValueConverter _converter = converter ?? throw new ArgumentNullException(nameof(converter));

    public void ApplySpacing(
        ICssStyleDeclaration css,
        IElement element,
        ComputedStyleBuilder style,
        DiagnosticsSession? diagnosticsSession)
    {
        var margin = ParseSpacingWithOverrides(
            css,
            HtmlCssConstants.CssProperties.Margin,
            HtmlCssConstants.CssProperties.MarginTop,
            HtmlCssConstants.CssProperties.MarginRight,
            HtmlCssConstants.CssProperties.MarginBottom,
            HtmlCssConstants.CssProperties.MarginLeft,
            element,
            diagnosticsSession);

        style.Margin = margin;

        var padding = ParseSpacingWithOverrides(
            css,
            HtmlCssConstants.CssProperties.Padding,
            HtmlCssConstants.CssProperties.PaddingTop,
            HtmlCssConstants.CssProperties.PaddingRight,
            HtmlCssConstants.CssProperties.PaddingBottom,
            HtmlCssConstants.CssProperties.PaddingLeft,
            element,
            diagnosticsSession);

        style.Padding = new Spacing(
            Math.Max(0, padding.Top),
            Math.Max(0, padding.Right),
            Math.Max(0, padding.Bottom),
            Math.Max(0, padding.Left));
    }

    public Spacing ParseSpacingWithOverrides(
        ICssStyleDeclaration css,
        string shorthandProperty,
        string topProperty,
        string rightProperty,
        string bottomProperty,
        string leftProperty,
        IElement element,
        DiagnosticsSession? diagnosticsSession)
    {
        var top = 0f;
        var right = 0f;
        var bottom = 0f;
        var left = 0f;

        ApplySpacingWithOverrides(
            css,
            shorthandProperty,
            topProperty,
            rightProperty,
            bottomProperty,
            leftProperty,
            element,
            diagnosticsSession,
            value => top = value,
            value => right = value,
            value => bottom = value,
            value => left = value);

        return new Spacing(top, right, bottom, left);
    }

    private void ApplySpacingWithOverrides(
        ICssStyleDeclaration css,
        string shorthandProperty,
        string topProperty,
        string rightProperty,
        string bottomProperty,
        string leftProperty,
        IElement element,
        DiagnosticsSession? diagnosticsSession,
        Action<float> setTop,
        Action<float> setRight,
        Action<float> setBottom,
        Action<float> setLeft)
    {
        ApplySpacingShorthand(css, shorthandProperty, element, diagnosticsSession, setTop, setRight, setBottom, setLeft);
        OverrideSpacingSide(css, topProperty, element, diagnosticsSession, setTop);
        OverrideSpacingSide(css, rightProperty, element, diagnosticsSession, setRight);
        OverrideSpacingSide(css, bottomProperty, element, diagnosticsSession, setBottom);
        OverrideSpacingSide(css, leftProperty, element, diagnosticsSession, setLeft);
    }

    private void OverrideSpacingSide(
        ICssStyleDeclaration css,
        string property,
        IElement element,
        DiagnosticsSession? diagnosticsSession,
        Action<float> setter)
    {
        var raw = InlineStyleSource.GetValue(element, property) ?? css.GetPropertyValue(property);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        setter(GetSpacingWithLogging(css, property, element, diagnosticsSession));
    }

    private void ApplySpacingShorthand(
        ICssStyleDeclaration css,
        string shorthandProperty,
        IElement element,
        DiagnosticsSession? diagnosticsSession,
        Action<float> setTop,
        Action<float> setRight,
        Action<float> setBottom,
        Action<float> setLeft)
    {
        var shorthandValue = InlineStyleSource.GetValue(element, shorthandProperty) ?? css.GetPropertyValue(shorthandProperty);

        if (string.IsNullOrWhiteSpace(shorthandValue))
        {
            return;
        }

        if (!TryParseSpacingValues(shorthandProperty, shorthandValue, element, diagnosticsSession, out var parsedValues))
        {
            return;
        }

        switch (parsedValues.Count)
        {
            case 1:
                setTop(parsedValues[0]);
                setRight(parsedValues[0]);
                setBottom(parsedValues[0]);
                setLeft(parsedValues[0]);
                break;
            case 2:
                setTop(parsedValues[0]);
                setBottom(parsedValues[0]);
                setRight(parsedValues[1]);
                setLeft(parsedValues[1]);
                break;
            case 3:
                setTop(parsedValues[0]);
                setRight(parsedValues[1]);
                setLeft(parsedValues[1]);
                setBottom(parsedValues[2]);
                break;
            case 4:
                setTop(parsedValues[0]);
                setRight(parsedValues[1]);
                setBottom(parsedValues[2]);
                setLeft(parsedValues[3]);
                break;
            default:
                break;
        }
    }

    private bool TryParseSpacingValues(
        string property,
        string shorthandValue,
        IElement element,
        DiagnosticsSession? diagnosticsSession,
        out List<float> parsedValues)
    {
        parsedValues = [];

        var tokens = shorthandValue.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();

        if (tokens.Length == 0)
        {
            return false;
        }

        if (tokens.Length > 4)
        {
            StyleDiagnosticEmitter.Emit(
                diagnosticsSession,
                "style/ignored-declaration",
                element,
                property,
                shorthandValue.Trim(),
                null,
                "Ignored",
                $"{property} shorthand has {tokens.Length} values; expected 1 to 4.");
            return false;
        }

        foreach (var token in tokens)
        {
            var unsupportedUnit = CssLengthUnitClassifier.DetectUnsupportedUnit(token);
            if (unsupportedUnit != null)
            {
                StyleDiagnosticEmitter.Emit(
                    diagnosticsSession,
                    "style/unsupported-declaration",
                    element,
                    property,
                    token,
                    null,
                    "Unsupported",
                    $"Unsupported unit '{unsupportedUnit}' for {property}.");
                return false;
            }

            if (!_converter.TryGetLengthPt(token, out var points))
            {
                StyleDiagnosticEmitter.Emit(
                    diagnosticsSession,
                    "style/ignored-declaration",
                    element,
                    property,
                    token,
                    null,
                    "Ignored",
                    $"Unable to parse {property} token as a supported length.");
                return false;
            }

            if (points < 0)
            {
                var decision = CreateNegativeSpacingDecision(property, points);

                StyleDiagnosticEmitter.Emit(
                    diagnosticsSession,
                    decision.EventName,
                    element,
                    property,
                    token,
                    decision.NormalizedValue,
                    decision.Decision,
                    decision.Reason);
            }

            parsedValues.Add(points);
        }

        return true;
    }

    private float GetSpacingWithLogging(
        ICssStyleDeclaration css,
        string property,
        IElement element,
        DiagnosticsSession? diagnosticsSession)
    {
        var rawValue = InlineStyleSource.GetValue(element, property) ?? css.GetPropertyValue(property);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return 0;
        }

        var trimmed = rawValue.Trim();

        var unsupportedUnit = CssLengthUnitClassifier.DetectUnsupportedUnit(trimmed);
        if (unsupportedUnit != null)
        {
            StyleDiagnosticEmitter.Emit(
                diagnosticsSession,
                "style/unsupported-declaration",
                element,
                property,
                trimmed,
                null,
                "Unsupported",
                $"Unsupported unit '{unsupportedUnit}' for {property}.");
            return 0;
        }

        if (!_converter.TryGetLengthPt(rawValue, out var points))
        {
            StyleDiagnosticEmitter.Emit(
                diagnosticsSession,
                "style/ignored-declaration",
                element,
                property,
                trimmed,
                null,
                "Ignored",
                $"Unable to parse {property} as a supported length.");
            return 0;
        }

        if (points < 0)
        {
            var decision = CreateNegativeSpacingDecision(property, points);

            StyleDiagnosticEmitter.Emit(
                diagnosticsSession,
                decision.EventName,
                element,
                property,
                trimmed,
                decision.NormalizedValue,
                decision.Decision,
                decision.Reason);
        }

        return points;
    }

    private static (
        string EventName,
        string Decision,
        string NormalizedValue,
        string Reason) CreateNegativeSpacingDecision(string property, float points)
    {
        var isPadding = property.StartsWith("padding", StringComparison.OrdinalIgnoreCase);
        return isPadding
            ? (
                "style/partially-applied-declaration",
                "PartiallyApplied",
                "0",
                "Negative padding value was clamped to zero.")
            : (
                "style/applied-declaration",
                "Applied",
                points.ToString(CultureInfo.InvariantCulture),
                "Negative spacing value was applied and may affect layout.");
    }
}
