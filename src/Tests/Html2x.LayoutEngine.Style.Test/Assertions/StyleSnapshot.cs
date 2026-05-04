using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Style;
using Shouldly;

namespace Html2x.LayoutEngine.Style.Test.Assertions;


internal record StyleSnapshot(
    string Tag,
    ComputedStyle? Style = null,
    List<StyleSnapshot>? Children = null
);
