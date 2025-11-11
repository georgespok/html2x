using Html2x.Core.Layout;

namespace Html2x.Pdf;

/// <summary>
///     External extension that enables visitor-style dispatch
///     without requiring Accept() inside model classes.
/// </summary>
internal static class FragmentVisitExtensions
{
    public static void VisitWith(this Fragment fragment, IFragmentVisitor visitor)
    {
        switch (fragment)
        {
            case BlockFragment b: visitor.Visit(b); break;
            case LineBoxFragment l: visitor.Visit(l); break;
            case ImageFragment i: visitor.Visit(i); break;
            case RuleFragment r: visitor.Visit(r); break;
            default:
                throw new NotSupportedException(
                    $"No visitor method defined for fragment type {fragment.GetType().Name}");
        }
    }
}