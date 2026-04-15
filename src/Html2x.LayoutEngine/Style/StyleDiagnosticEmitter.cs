using AngleSharp.Dom;
using Html2x.Abstractions.Diagnostics;

namespace Html2x.LayoutEngine.Style;

internal static class StyleDiagnosticEmitter
{
    public static void Emit(
        DiagnosticsSession? diagnosticsSession,
        string eventName,
        IElement element,
        string propertyName,
        string rawValue,
        string? normalizedValue,
        string decision,
        string reason)
    {
        if (diagnosticsSession is null)
        {
            return;
        }

        var context = CreateDiagnosticContext(element, propertyName, rawValue);
        diagnosticsSession.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Warning,
            Name = eventName,
            Description = reason,
            Severity = DiagnosticSeverity.Warning,
            Context = context,
            RawUserInput = context.RawUserInput,
            Payload = new StyleDiagnosticPayload
            {
                PropertyName = propertyName,
                RawValue = rawValue,
                NormalizedValue = normalizedValue,
                Decision = decision,
                Reason = reason,
                Context = context
            }
        });
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
