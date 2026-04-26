namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Classifies whether a formatting context has active layout support or a documented fallback.
/// </summary>
internal enum FormattingContextSupport
{
    Supported,
    UnsupportedDiagnostic,
    UnsupportedFailFast
}
