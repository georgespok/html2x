using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public class BlockBox(DisplayRole role) : DisplayNode(role)
{
    // Compatibility mirrors for pre-UsedGeometry callers. Layout geometry is authoritative after assignment.
    private float _x;
    private float _y;
    private float _width;
    private float _height;
    private float _markerOffset;
    private Spacing _padding = new();
    private UsedGeometry? _usedGeometry;

    public float X
    {
        get => _usedGeometry?.X ?? _x;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(X));
            _x = value;
        }
    }

    public float Y
    {
        get => _usedGeometry?.Y ?? _y;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(Y));
            _y = value;
        }
    }

    public float Width
    {
        get => _usedGeometry?.Width ?? _width;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(Width));
            _width = value;
        }
    }

    public float Height
    {
        get => _usedGeometry?.Height ?? _height;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(Height));
            _height = value;
        }
    }

    // Temporary layout-stage compatibility surfaces. New geometry code should prefer UsedGeometry.
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
        get => _usedGeometry?.MarkerOffset ?? _markerOffset;
        set
        {
            ThrowIfLayoutGeometryApplied(nameof(MarkerOffset));
            _markerOffset = value;
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

    private void SetUsedGeometry(UsedGeometry? value)
    {
        _usedGeometry = value;
        if (value.HasValue)
        {
            var geometry = value.Value;
            _x = geometry.X;
            _y = geometry.Y;
            _width = geometry.Width;
            _height = geometry.Height;
            _markerOffset = geometry.MarkerOffset;
        }
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

    protected override DisplayNode CloneShallowForParent(DisplayNode parent)
    {
        return new BlockBox(Role)
        {
            Element = Element,
            Style = Style,
            Parent = parent,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Margin = Margin,
            Padding = Padding,
            IsAnonymous = IsAnonymous,
            TextAlign = TextAlign,
            MarkerOffset = MarkerOffset,
            UsedGeometry = UsedGeometry,
            IsInlineBlockContext = IsInlineBlockContext
        };
    }
}
