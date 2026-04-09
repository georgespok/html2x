namespace Html2x.LayoutEngine.Models;

public sealed class TableBox(DisplayRole role) : BlockBox(role)
{
    public int DerivedColumnCount { get; set; } = -1;
}
