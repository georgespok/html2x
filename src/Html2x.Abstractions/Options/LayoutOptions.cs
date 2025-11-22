using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Options;

public sealed class LayoutOptions
{
    public PageSize PageSize { get; init; } = PaperSizes.Letter;
}

