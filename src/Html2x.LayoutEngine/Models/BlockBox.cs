using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public class BlockBox(BoxRole role) : BoxNode(role)
{
    // Compatibility seed values for callers that still populate geometry before layout resolves UsedGeometry.
    private float _pendingX;
    private float _pendingY;
    private float _pendingWidth;
    private float _pendingHeight;
    private float _pendingMarkerOffset;
    private Spacing _padding = new();
    private UsedGeometry? _usedGeometry;

    public float X
    {
        get => _usedGeometry?.X ?? _pendingX;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(X));
            _pendingX = value;
        }
    }

    public float Y
    {
        get => _usedGeometry?.Y ?? _pendingY;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(Y));
            _pendingY = value;
        }
    }

    public float Width
    {
        get => _usedGeometry?.Width ?? _pendingWidth;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(Width));
            _pendingWidth = value;
        }
    }

    public float Height
    {
        get => _usedGeometry?.Height ?? _pendingHeight;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(Height));
            _pendingHeight = value;
        }
    }

    // Temporary layout-stage compatibility surface. New geometry code should prefer UsedGeometry.
    public Spacing Margin { get; set; } = new();

    public Spacing Padding
    {
        get => _padding;
        set => _padding = value;
    }

    public string TextAlign { get; set; } = HtmlCssConstants.Defaults.TextAlign;

    public bool IsAnonymous { get; init; }

    public float MarkerOffset
    {
        get => _usedGeometry?.MarkerOffset ?? _pendingMarkerOffset;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(MarkerOffset));
            _pendingMarkerOffset = value;
        }
    }

    public UsedGeometry? UsedGeometry
    {
        get => _usedGeometry;
        internal set => SetUsedGeometry(value);
    }

    internal void ApplyLayoutGeometry(UsedGeometry geometry)
    {
        SetUsedGeometry(geometry);
    }

    internal BlockBoxLayoutState CaptureLayoutState()
    {
        return new BlockBoxLayoutState(
            _pendingX,
            _pendingY,
            _pendingWidth,
            _pendingHeight,
            _pendingMarkerOffset,
            _padding,
            Margin,
            TextAlign,
            _usedGeometry,
            InlineLayout,
            IsInlineBlockContext);
    }

    internal void RestoreLayoutState(BlockBoxLayoutState state)
    {
        _pendingX = state.PendingX;
        _pendingY = state.PendingY;
        _pendingWidth = state.PendingWidth;
        _pendingHeight = state.PendingHeight;
        _pendingMarkerOffset = state.PendingMarkerOffset;
        _padding = state.Padding;
        Margin = state.Margin;
        TextAlign = state.TextAlign;
        _usedGeometry = state.UsedGeometry;
        InlineLayout = state.InlineLayout;
        IsInlineBlockContext = state.IsInlineBlockContext;
    }

    private void SetUsedGeometry(UsedGeometry? value)
    {
        _usedGeometry = value;
    }

    private void ThrowIfLayoutGeometryApplied(string propertyName)
    {
        if (_usedGeometry.HasValue)
        {
            throw new InvalidOperationException(
                $"{propertyName} cannot be changed after layout geometry has been applied. " +
                $"Use {nameof(ApplyLayoutGeometry)} to replace post-layout geometry.");
        }
    }

    // Layout owns this value. Measurement paths must restore any prior value after probing.
    public InlineLayoutResult? InlineLayout { get; set; }

    public bool IsInlineBlockContext { get; set; }

    protected override BoxNode CloneShallowForParent(BoxNode parent)
    {
        return CopyBlockStateTo(new BlockBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            IsAnonymous = IsAnonymous,
        });
    }

    protected TBlock CopyBlockStateTo<TBlock>(TBlock clone)
        where TBlock : BlockBox
    {
        ArgumentNullException.ThrowIfNull(clone);

        clone._pendingX = _pendingX;
        clone._pendingY = _pendingY;
        clone._pendingWidth = _pendingWidth;
        clone._pendingHeight = _pendingHeight;
        clone._pendingMarkerOffset = _pendingMarkerOffset;
        clone._usedGeometry = _usedGeometry;
        clone.Margin = Margin;
        clone.Padding = Padding;
        clone.TextAlign = TextAlign;
        clone.IsInlineBlockContext = IsInlineBlockContext;

        return clone;
    }
}

internal readonly record struct BlockBoxLayoutState(
    float PendingX,
    float PendingY,
    float PendingWidth,
    float PendingHeight,
    float PendingMarkerOffset,
    Spacing Padding,
    Spacing Margin,
    string TextAlign,
    UsedGeometry? UsedGeometry,
    InlineLayoutResult? InlineLayout,
    bool IsInlineBlockContext);
