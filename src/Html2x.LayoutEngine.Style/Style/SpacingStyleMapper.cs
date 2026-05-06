using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Style.Models;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Style.Style;

/// <summary>
///     Maps CSS margin and padding declarations into layout spacing values and diagnostics.
/// </summary>
internal sealed class SpacingStyleMapper(CssValueConverter converter)
{
    private readonly CssLengthDeclarationReader _lengthReader = new(converter);

    public void ApplySpacing(
        ICssStyleDeclaration css,
        IElement element,
        ComputedStyleBuilder style,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var margin = ParseSpacingWithOverrides(
            css,
            HtmlCssConstants.CssProperties.Margin,
            HtmlCssConstants.CssProperties.MarginTop,
            HtmlCssConstants.CssProperties.MarginRight,
            HtmlCssConstants.CssProperties.MarginBottom,
            HtmlCssConstants.CssProperties.MarginLeft,
            element,
            diagnosticsSink);

        style.Margin = margin;

        var padding = ParseSpacingWithOverrides(
            css,
            HtmlCssConstants.CssProperties.Padding,
            HtmlCssConstants.CssProperties.PaddingTop,
            HtmlCssConstants.CssProperties.PaddingRight,
            HtmlCssConstants.CssProperties.PaddingBottom,
            HtmlCssConstants.CssProperties.PaddingLeft,
            element,
            diagnosticsSink);

        style.Padding = new(
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
        IDiagnosticsSink? diagnosticsSink = null)
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
            diagnosticsSink,
            value => top = value,
            value => right = value,
            value => bottom = value,
            value => left = value);

        return new(top, right, bottom, left);
    }

    private void ApplySpacingWithOverrides(
        ICssStyleDeclaration css,
        string shorthandProperty,
        string topProperty,
        string rightProperty,
        string bottomProperty,
        string leftProperty,
        IElement element,
        IDiagnosticsSink? diagnosticsSink,
        Action<float> setTop,
        Action<float> setRight,
        Action<float> setBottom,
        Action<float> setLeft)
    {
        ApplySpacingShorthand(
            css,
            shorthandProperty,
            element,
            diagnosticsSink,
            setTop,
            setRight,
            setBottom,
            setLeft);
        OverrideSpacingSide(css, topProperty, element, diagnosticsSink, setTop);
        OverrideSpacingSide(css, rightProperty, element, diagnosticsSink, setRight);
        OverrideSpacingSide(css, bottomProperty, element, diagnosticsSink, setBottom);
        OverrideSpacingSide(css, leftProperty, element, diagnosticsSink, setLeft);
    }

    private void OverrideSpacingSide(
        ICssStyleDeclaration css,
        string property,
        IElement element,
        IDiagnosticsSink? diagnosticsSink,
        Action<float> setter)
    {
        var raw = InlineStyleSource.GetValue(css, element, property);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        setter(GetSpacingWithLogging(css, property, element, diagnosticsSink));
    }

    private void ApplySpacingShorthand(
        ICssStyleDeclaration css,
        string shorthandProperty,
        IElement element,
        IDiagnosticsSink? diagnosticsSink,
        Action<float> setTop,
        Action<float> setRight,
        Action<float> setBottom,
        Action<float> setLeft)
    {
        var shorthandValue = InlineStyleSource.GetValue(css, element, shorthandProperty);

        if (string.IsNullOrWhiteSpace(shorthandValue))
        {
            return;
        }

        var parsedValues = ParseSpacingValues(
            shorthandProperty,
            shorthandValue,
            element,
            diagnosticsSink);
        if (parsedValues is null)
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
        }
    }

    private List<float>? ParseSpacingValues(
        string property,
        string shorthandValue,
        IElement element,
        IDiagnosticsSink? diagnosticsSink)
    {
        var parsedValues = new List<float>();

        var tokens = shorthandValue.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();

        if (tokens.Length == 0)
        {
            return null;
        }

        if (tokens.Length > 4)
        {
            StyleDiagnostics.EmitIgnoredDeclaration(
                diagnosticsSink,
                element,
                property,
                shorthandValue.Trim(),
                null,
                $"{property} shorthand has {tokens.Length} values; expected 1 to 4.");
            return null;
        }

        foreach (var token in tokens)
        {
            if (!_lengthReader.TryParseLengthToken(
                    token,
                    element,
                    property,
                    $"Unable to parse {property} token as a supported length.",
                    diagnosticsSink,
                    out var points))
            {
                return null;
            }

            if (points < 0)
            {
                var decision = CreateNegativeSpacingDecision(property, points);

                StyleDiagnostics.Emit(
                    diagnosticsSink,
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

        return parsedValues;
    }

    private float GetSpacingWithLogging(
        ICssStyleDeclaration css,
        string property,
        IElement element,
        IDiagnosticsSink? diagnosticsSink)
    {
        var rawValue = _lengthReader.GetValue(css, element, property);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return 0;
        }

        var trimmed = rawValue.Trim();

        if (!_lengthReader.TryParseLengthToken(
                trimmed,
                element,
                property,
                $"Unable to parse {property} as a supported length.",
                diagnosticsSink,
                out var points))
        {
            return 0;
        }

        if (points < 0)
        {
            var decision = CreateNegativeSpacingDecision(property, points);

            StyleDiagnostics.Emit(
                diagnosticsSink,
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
        var isPadding = property.StartsWith(
            HtmlCssConstants.CssProperties.Padding,
            StringComparison.OrdinalIgnoreCase);
        return isPadding
            ? (
                StyleDiagnosticNames.Events.PartiallyAppliedDeclaration,
                StyleDiagnosticNames.Decisions.PartiallyApplied,
                HtmlCssConstants.CssValues.Zero,
                "Negative padding value was clamped to zero.")
            : (
                StyleDiagnosticNames.Events.AppliedDeclaration,
                StyleDiagnosticNames.Decisions.Applied,
                points.ToString(CultureInfo.InvariantCulture),
                "Negative spacing value was applied and may affect layout.");
    }
}