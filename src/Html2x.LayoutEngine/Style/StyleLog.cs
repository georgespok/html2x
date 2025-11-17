using AngleSharp.Dom;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Style;

internal static class StyleLog
{
    private const string Category = "layout/warning";

    public static void InvalidSpacingValue(
        IDiagnosticSession? session,
        string property,
        string value,
        IElement element)
    {
        Publish(
            session,
            "layout/warning/invalid-spacing",
            payload =>
            {
                payload["property"] = property;
                payload["value"] = value;
                payload["element"] = Describe(element);
            });
    }

    public static void NegativeSpacingValue(
        IDiagnosticSession? session,
        string property,
        float value,
        IElement element)
    {
        Publish(
            session,
            "layout/warning/negative-spacing",
            payload =>
            {
                payload["property"] = property;
                payload["value"] = value;
                payload["element"] = Describe(element);
            });
    }

    public static void UnsupportedSpacingUnit(
        IDiagnosticSession? session,
        string property,
        string value,
        string unit,
        IElement element)
    {
        Publish(
            session,
            "layout/warning/unsupported-spacing-unit",
            payload =>
            {
                payload["property"] = property;
                payload["value"] = value;
                payload["unit"] = unit;
                payload["element"] = Describe(element);
            });
    }

    private static void Publish(
        IDiagnosticSession? session,
        string kind,
        Action<Dictionary<string, object?>> configure)
    {
        if (session is not { IsEnabled: true })
        {
            return;
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
        configure(payload);

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            session.Descriptor.SessionId,
            Category,
            kind,
            DateTimeOffset.UtcNow,
            payload);

        session.Publish(diagnosticEvent);
    }

    private static string Describe(IElement element)
    {
        var identifier = element.TagName.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(element.Id))
        {
            identifier += $"#{element.Id}";
        }

        if (!string.IsNullOrWhiteSpace(element.ClassName))
        {
            identifier += "." + element.ClassName.Replace(' ', '.');
        }

        return identifier;
    }
}
