namespace Html2x.LayoutEngine.Geometry.Text;


/// <summary>
/// Describes whether a line should be justified and how much extra space each whitespace receives.
/// </summary>
internal readonly record struct JustificationPlan(
    bool ShouldJustify,
    float ExtraSpace,
    float PerWhitespaceExtra)
{
    public static JustificationPlan None { get; } = new(false, 0f, 0f);
}
