using Html2x.RenderModel.Text;

namespace Html2x.Text;

/// <summary>
/// Resolves fonts strictly from a configured source.
/// </summary>
public interface IFontSource
{
    ResolvedFont Resolve(FontKey requested, string consumer);
}
