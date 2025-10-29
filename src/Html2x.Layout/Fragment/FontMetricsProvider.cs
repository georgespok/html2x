using Html2x.Core.Layout;

namespace Html2x.Layout.Fragment;

public sealed class FontMetricsProvider
{
    public (float ascent, float descent) Get(FontKey font, float size)
    {
        // Roughly 80/20 rule: 80% up, 20% down
        return (size * 0.8f, size * 0.2f);
    }
}