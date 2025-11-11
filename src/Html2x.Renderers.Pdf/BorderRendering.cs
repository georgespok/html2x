using Html2x.Abstractions.Layout;

namespace Html2x.Renderers.Pdf;

internal static class BorderRendering
{
    public static BorderSide? GetUniformBorder(BorderEdges? edges)
    {
        if (edges is null || !edges.HasAny)
        {
            return null;
        }

        var candidate = edges.Top ?? edges.Right ?? edges.Bottom ?? edges.Left;
        if (candidate is null)
        {
            return null;
        }

        if (candidate.Width <= 0 || candidate.LineStyle == BorderLineStyle.None)
        {
            return null;
        }

        if (!IsMatching(edges.Top, candidate) ||
            !IsMatching(edges.Right, candidate) ||
            !IsMatching(edges.Bottom, candidate) ||
            !IsMatching(edges.Left, candidate))
        {
            return null;
        }

        return candidate;
    }

    private static bool IsMatching(BorderSide? side, BorderSide candidate)
    {
        return side is null || side == candidate;
    }
}
