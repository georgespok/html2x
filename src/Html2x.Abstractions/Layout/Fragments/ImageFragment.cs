namespace Html2x.Abstractions.Layout;

// Replaced element (img)
public sealed class ImageFragment : Fragment
{
    public ImageRef Image { get; init; } // logical reference; renderer resolves bytes/handle
    public ObjectFit ObjectFit { get; init; } // Contain, Cover, Fill, ScaleDown, None
    public Alignment Align { get; init; } // for contain/cover anchoring
}

public sealed record ImageRef(string Id); // maps to image bytes in a doc-level resource cache

public enum ObjectFit
{
    Fill,
    Contain,
    Cover,
    ScaleDown,
    None
}

public enum Alignment
{
    Center,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}