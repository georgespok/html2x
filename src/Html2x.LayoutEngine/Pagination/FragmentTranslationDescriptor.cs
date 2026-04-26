using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Declares how one fragment type is cloned, which geometry fields move, and which children are translated.
/// </summary>
internal sealed class FragmentTranslationDescriptor
{
    private readonly Func<LayoutFragment, FragmentTranslationRequest, IFragmentTranslator, LayoutFragment> _translate;

    public FragmentTranslationDescriptor(
        Type fragmentType,
        IReadOnlyList<string> geometryFields,
        FragmentChildTraversalPolicy childTraversal,
        Func<LayoutFragment, FragmentTranslationRequest, IFragmentTranslator, LayoutFragment> translate)
    {
        FragmentType = fragmentType ?? throw new ArgumentNullException(nameof(fragmentType));
        GeometryFields = (geometryFields ?? throw new ArgumentNullException(nameof(geometryFields))).ToArray();
        ChildTraversal = childTraversal;
        _translate = translate ?? throw new ArgumentNullException(nameof(translate));

        if (!typeof(LayoutFragment).IsAssignableFrom(fragmentType))
        {
            throw new ArgumentException("Fragment type must derive from Fragment.", nameof(fragmentType));
        }

        if (GeometryFields.Count == 0)
        {
            throw new ArgumentException("At least one geometry field must be declared.", nameof(geometryFields));
        }
    }

    public Type FragmentType { get; }

    public IReadOnlyList<string> GeometryFields { get; }

    public FragmentChildTraversalPolicy ChildTraversal { get; }

    public LayoutFragment Translate(
        LayoutFragment source,
        FragmentTranslationRequest request,
        IFragmentTranslator translator)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(translator);

        if (source.GetType() != FragmentType)
        {
            throw new ArgumentException(
                $"Descriptor for '{FragmentType.Name}' cannot translate '{source.GetType().Name}'.",
                nameof(source));
        }

        return _translate(source, request, translator);
    }

    public static FragmentTranslationDescriptor Create<TFragment>(
        IReadOnlyList<string> geometryFields,
        FragmentChildTraversalPolicy childTraversal,
        Func<TFragment, FragmentTranslationRequest, IFragmentTranslator, LayoutFragment> translate)
        where TFragment : LayoutFragment
    {
        ArgumentNullException.ThrowIfNull(translate);

        return new FragmentTranslationDescriptor(
            typeof(TFragment),
            geometryFields,
            childTraversal,
            (source, request, translator) => translate((TFragment)source, request, translator));
    }
}
