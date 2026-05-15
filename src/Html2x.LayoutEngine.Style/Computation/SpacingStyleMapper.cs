using System.Globalization;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Style.Models;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Style.Computation;

/// <summary>
///     Maps CSS margin and padding declarations into layout spacing values and diagnostics.
/// </summary>
internal sealed class SpacingStyleMapper(CssValueParser parser)
{
    private readonly CssLengthDeclarationReader _lengthReader = new(parser);

    public void ApplySpacing(
        ICssStyleDeclaration css,
        IElement element,
        ComputedStyleBuilder style,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var margin = ParseSpacingWithOverrides(
            css,
            HtmlCssVocabulary.CssProperties.Margin,
            HtmlCssVocabulary.CssProperties.MarginTop,
            HtmlCssVocabulary.CssProperties.MarginRight,
            HtmlCssVocabulary.CssProperties.MarginBottom,
            HtmlCssVocabulary.CssProperties.MarginLeft,
            element,
            diagnosticsSink);

        style.Margin = margin;

        var padding = ParseSpacingWithOverrides(
            css,
            HtmlCssVocabulary.CssProperties.Padding,
            HtmlCssVocabulary.CssProperties.PaddingTop,
            HtmlCssVocabulary.CssProperties.PaddingRight,
            HtmlCssVocabulary.CssProperties.PaddingBottom,
            HtmlCssVocabulary.CssProperties.PaddingLeft,
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
        var raw = AuthoredCssDeclarationReader.GetValue(css, element, property);

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
        var shorthandValue = AuthoredCssDeclarationReader.GetValue(css, element, shorthandProperty);

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
            StyleDiagnosticEmitter.EmitIgnoredDeclaration(
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
            if (!_lengthReader.TryParseLengthDeclaration(
                    token,
                    element,
                    property,
                    $"Unable to parse {property} token as a supported length.",
                    diagnosticsSink,
                    out var declaration))
            {
                return null;
            }

            var points = declaration.Points;
            if (points < 0)
            {
                var decision = CreateNegativeSpacingDecision(property, points);

                StyleDiagnosticEmitter.Emit(
                    diagnosticsSink,
                    decision.EventName,
                    element,
                    property,
                    declaration.RawValue,
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
        if (!_lengthReader.TryReadLengthDeclaration(
                css,
                element,
                property,
                $"Unable to parse {property} as a supported length.",
                diagnosticsSink,
                out var declaration))
        {
            return 0;
        }

        var points = declaration.Points;
        if (points < 0)
        {
            var decision = CreateNegativeSpacingDecision(property, points);

            StyleDiagnosticEmitter.Emit(
                diagnosticsSink,
                decision.EventName,
                element,
                property,
                declaration.RawValue,
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
            HtmlCssVocabulary.CssProperties.Padding,
            StringComparison.OrdinalIgnoreCase);
        return isPadding
            ? (
                StyleDiagnosticNames.Events.PartiallyAppliedDeclaration,
                StyleDiagnosticNames.Decisions.PartiallyApplied,
                HtmlCssVocabulary.CssValues.Zero,
                "Negative padding value was clamped to zero.")
            : (
                StyleDiagnosticNames.Events.AppliedDeclaration,
                StyleDiagnosticNames.Decisions.Applied,
                points.ToString(CultureInfo.InvariantCulture),
                "Negative spacing value was applied and may affect layout.");
    }
}
