using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Layout.Fonts;

/// <summary>
/// Resolves fonts strictly from a configured source.
/// </summary>
public interface IFontSource
{
    ResolvedFont Resolve(FontKey requested);
}
