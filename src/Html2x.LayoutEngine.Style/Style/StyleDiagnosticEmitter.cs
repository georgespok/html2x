using AngleSharp.Dom;
using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Style;

internal static class StyleDiagnostics
{
    public static void EmitIgnoredDeclaration(
        IDiagnosticsSink? diagnosticsSink,
        IElement element,
        string propertyName,
        string rawValue,
        string? normalizedValue,
        string reason)
    {
        Emit(
            diagnosticsSink,
            "style/ignored-declaration",
            element,
            propertyName,
            rawValue,
            normalizedValue,
            "Ignored",
            reason);
    }

    public static void EmitUnsupportedDeclaration(
        IDiagnosticsSink? diagnosticsSink,
        IElement element,
        string propertyName,
        string rawValue,
        string reason)
    {
        Emit(
            diagnosticsSink,
            "style/unsupported-declaration",
            element,
            propertyName,
            rawValue,
            normalizedValue: null,
            "Unsupported",
            reason);
    }

    public static void Emit(
        IDiagnosticsSink? diagnosticsSink,
        string eventName,
        IElement element,
        string propertyName,
        string rawValue,
        string? normalizedValue,
        string decision,
        string reason)
    {
        var context = CreateDiagnosticContext(element, propertyName, rawValue);
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: "stage/style",
            Name: eventName,
            Severity: DiagnosticSeverity.Warning,
            Message: reason,
            Context: context,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field("propertyName", propertyName),
                DiagnosticFields.Field("rawValue", rawValue),
                DiagnosticFields.Field(
                    "normalizedValue",
                    normalizedValue is null ? null : DiagnosticValue.From(normalizedValue)),
                DiagnosticFields.Field("decision", decision),
                DiagnosticFields.Field("reason", reason)),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static DiagnosticContext CreateDiagnosticContext(
        IElement element,
        string propertyName,
        string rawValue)
    {
        return new DiagnosticContext(
            Selector: CreateSelector(element),
            ElementIdentity: CreateElementIdentity(element),
            StyleDeclaration: InlineStyleSource.GetDeclaration(element, propertyName) ?? $"{propertyName}: {rawValue}",
            StructuralPath: BuildStructuralPath(element),
            RawUserInput: element.GetAttribute(HtmlCssConstants.HtmlAttributes.Style));
    }

    private static string CreateSelector(IElement element)
    {
        if (!string.IsNullOrWhiteSpace(element.Id))
        {
            return $"#{element.Id}";
        }

        var className = element.ClassList.FirstOrDefault();
        return string.IsNullOrWhiteSpace(className)
            ? element.TagName.ToLowerInvariant()
            : $"{element.TagName.ToLowerInvariant()}.{className}";
    }

    private static string CreateElementIdentity(IElement element)
    {
        var identity = element.TagName.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(element.Id))
        {
            identity += $"#{element.Id}";
        }

        foreach (var className in element.ClassList)
        {
            identity += $".{className}";
        }

        return identity;
    }

    private static string BuildStructuralPath(IElement element)
    {
        var segments = new Stack<string>();
        var current = element;
        while (current is not null)
        {
            segments.Push(CreateElementIdentity(current));
            current = current.ParentElement;
        }

        return string.Join("/", segments);
    }
}
