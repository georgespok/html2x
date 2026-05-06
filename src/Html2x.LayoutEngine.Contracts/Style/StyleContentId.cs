using System.Globalization;

namespace Html2x.LayoutEngine.Contracts.Style;

internal readonly record struct StyleContentId
{
    public StyleContentId(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        Value = value;
    }

    public static StyleContentId Unspecified { get; } = new(0);

    public int Value { get; }

    public bool IsSpecified => Value > 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}