using Html2x.RenderModel;

using Html2x.LayoutEngine.Geometry;

namespace Html2x.LayoutEngine.Geometry.Models;

/// <summary>
/// Represents mutable block-level layout state while geometry and inline flow are resolved.
/// </summary>
internal class BlockBox(BoxRole role) : BoxNode(role)
{
    private Spacing _padding = new();
    private UsedGeometry? _usedGeometry;

    /// <summary>
    /// Gets or sets the layout input reserved for synthetic list markers.
    /// </summary>
    /// <remarks>
    /// Final post-layout marker geometry is published through <see cref="UsedGeometry"/>.
    /// </remarks>
    public float MarkerOffset { get; set; }

    /// <summary>
    /// Gets or sets the margin resolved for the current layout pass.
    /// </summary>
    public Spacing Margin { get; internal set; } = new();

    /// <summary>
    /// Gets or sets the padding resolved for the current layout pass.
    /// </summary>
    public Spacing Padding
    {
        get => _padding;
        internal set => _padding = value;
    }

    /// <summary>
    /// Gets or sets the text alignment resolved for inline layout.
    /// </summary>
    public string TextAlign { get; internal set; } = HtmlCssConstants.Defaults.TextAlign;

    public bool IsAnonymous { get; init; }

    public UsedGeometry? UsedGeometry
    {
        get => _usedGeometry;
        internal set => SetUsedGeometry(value);
    }

    internal void ApplyLayoutGeometry(UsedGeometry geometry)
    {
        SetUsedGeometry(geometry);
    }

    private void SetUsedGeometry(UsedGeometry? value)
    {
        _usedGeometry = value;
    }

    /// <summary>
    /// Gets or sets the inline layout resolved for this block during layout.
    /// </summary>
    /// <remarks>
    /// Layout owns this value. Measurement paths must not assign temporary inline layout here.
    /// </remarks>
    public InlineLayoutResult? InlineLayout { get; internal set; }

    /// <summary>
    /// Gets or sets whether this block is the content box for an inline-block formatting context.
    /// </summary>
    public bool IsInlineBlockContext { get; internal set; }

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new BlockBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
            SourceIdentity = SourceIdentity
        });
    }

    protected TBlock CopyBlockStateTo<TBlock>(TBlock clone)
        where TBlock : BlockBox
    {
        ArgumentNullException.ThrowIfNull(clone);

        clone._usedGeometry = _usedGeometry;
        clone.MarkerOffset = MarkerOffset;
        clone.Margin = Margin;
        clone.Padding = Padding;
        clone.TextAlign = TextAlign;
        clone.IsInlineBlockContext = IsInlineBlockContext;

        return clone;
    }
}

