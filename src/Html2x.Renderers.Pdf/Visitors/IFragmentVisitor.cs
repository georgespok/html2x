using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.Renderers.Pdf.Visitors;

/// <summary>
///     Defines the visitor interface — one method per fragment type.
/// </summary>
internal interface IFragmentVisitor
{
    void Visit(BlockFragment fragment);
    void Visit(LineBoxFragment fragment);
    void Visit(ImageFragment fragment);
    void Visit(RuleFragment fragment);
}