using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public class BlockBox(DisplayRole role) : DisplayNode(role)
{
    private float _x;
    private float _y;
    private float _width;
    private float _height;
    private float _markerOffset;
    private UsedGeometry? _usedGeometry;

    public float X
    {
        get => _usedGeometry?.X ?? _x;
        set
        {
            _x = value;
            if (_usedGeometry.HasValue)
            {
                _usedGeometry = _usedGeometry.Value.WithBorderX(value);
            }
        }
    }

    public float Y
    {
        get => _usedGeometry?.Y ?? _y;
        set
        {
            _y = value;
            if (_usedGeometry.HasValue)
            {
                _usedGeometry = _usedGeometry.Value.WithBorderY(value);
            }
        }
    }

    public float Width
    {
        get => _usedGeometry?.Width ?? _width;
        set
        {
            _width = value;
            if (_usedGeometry.HasValue)
            {
                _usedGeometry = _usedGeometry.Value.WithBorderWidth(value);
            }
        }
    }

    public float Height
    {
        get => _usedGeometry?.Height ?? _height;
        set
        {
            _height = value;
            if (_usedGeometry.HasValue)
            {
                _usedGeometry = _usedGeometry.Value.WithBorderHeight(value);
            }
        }
    }

    public Spacing Margin { get; set; } = new();

    public Spacing Padding { get; set; } = new();

    public string TextAlign { get; set; } = HtmlCssConstants.Defaults.TextAlign;

    public bool IsAnonymous { get; init; }

    public float MarkerOffset
    {
        get => _usedGeometry?.MarkerOffset ?? _markerOffset;
        set
        {
            _markerOffset = value;
            if (_usedGeometry.HasValue)
            {
                _usedGeometry = _usedGeometry.Value with { MarkerOffset = value };
            }
        }
    }

    public UsedGeometry? UsedGeometry
    {
        get => _usedGeometry;
        set
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
    }

    public InlineLayoutResult? InlineLayout { get; set; }

    public bool IsInlineBlockContext { get; set; }
}
