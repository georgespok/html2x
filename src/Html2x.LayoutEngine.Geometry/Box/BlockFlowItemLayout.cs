namespace Html2x.LayoutEngine.Geometry.Box;

internal abstract record BlockFlowItemLayout(int Order);

internal sealed record BlockChildFlowItemLayout(
    int Order,
    BlockLayoutRuleResult Child) : BlockFlowItemLayout(Order);

internal sealed record InlineSegmentFlowItemLayout(
    int Order,
    InlineFlowSegmentLayout Segment) : BlockFlowItemLayout(Order);
