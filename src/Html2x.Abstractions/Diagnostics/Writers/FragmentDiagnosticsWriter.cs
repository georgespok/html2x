using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.Abstractions.Diagnostics.Writers;

public sealed class FragmentDiagnosticsWriter
{
    private const float PointsPerPixel = 72f / 96f;

    public DiagnosticsSnapshot Write(HtmlLayout layout, int sequenceStart = 0)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        var fragments = new List<FragmentDiagnostics>();
        var fragmentIndex = 0;
        var lineIndex = 0;

        foreach (var page in layout.Pages)
        {
            foreach (var fragment in page.Children)
            {
                WriteFragment(fragment, fragments, ref fragmentIndex, ref lineIndex);
            }
        }

        return new DiagnosticsSnapshot(
            sequenceStart,
            fragments,
            [],
            []);
    }

    private void WriteFragment(
        Fragment fragment,
        ICollection<FragmentDiagnostics> output,
        ref int fragmentIndex,
        ref int lineIndex)
    {
        switch (fragment)
        {
            case BlockFragment block:
                foreach (var child in block.Children)
                {
                    WriteFragment(child, output, ref fragmentIndex, ref lineIndex);
                }

                break;
            case LineBoxFragment line:
                output.Add(CreateTextFragment(line, fragmentIndex++, lineIndex++));
                break;
            default:
                fragmentIndex++;
                break;
        }
    }

    private static FragmentDiagnostics CreateTextFragment(LineBoxFragment line, int fragmentIndex, int lineIndex)
    {
        var color = line.Runs.FirstOrDefault()?.ColorHex;
        var normalizedColor = string.IsNullOrWhiteSpace(color) ? null : color;

        return new FragmentDiagnostics(
            fragmentIndex,
            "text",
            "inline",
            lineIndex,
            normalizedColor,
            ToPixels(line.LineHeight),
            string.IsNullOrWhiteSpace(line.TextAlign) ? "left" : line.TextAlign,
            ToPixels(line.Rect.Width),
            ToPixels(line.Rect.Height));
    }

    private static float ToPixels(float points)
    {
        return points <= 0 ? 0 : points / PointsPerPixel;
    }
}
