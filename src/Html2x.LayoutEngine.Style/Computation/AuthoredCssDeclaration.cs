namespace Html2x.LayoutEngine.Style.Computation;

internal sealed record AuthoredCssDeclaration(
    string PropertyName,
    string RawValue,
    string Text);
