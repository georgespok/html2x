using Html2x.Abstractions.Layout;

namespace Html2x.Pdf.Visitors;

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

