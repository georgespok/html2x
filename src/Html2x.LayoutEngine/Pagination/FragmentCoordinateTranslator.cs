using Html2x.Abstractions.Layout.Fragments;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Provides a paginator-scoped facade over descriptor-based fragment translation and extension registration.
/// </summary>
internal sealed class FragmentCoordinateTranslator
{
    private static readonly IReadOnlyList<FragmentTranslationDescriptor> BuiltInDescriptors =
        BuiltInFragmentTranslationDescriptors.Create();

    private readonly FragmentTranslator _translator;

    private FragmentCoordinateTranslator(IEnumerable<FragmentTranslationDescriptor> descriptors)
    {
        _translator = new FragmentTranslator(descriptors);
    }

    internal static FragmentCoordinateTranslator CreateDefault()
    {
        return new FragmentCoordinateTranslator(BuiltInDescriptors);
    }

    internal LayoutFragment CloneWithPlacement(LayoutFragment source, int pageNumber, float x, float y)
    {
        return _translator.TranslateToPlacement(source, pageNumber, x, y);
    }

    internal BlockFragment CloneBlockWithPlacement(BlockFragment source, int pageNumber, float x, float y)
    {
        return (BlockFragment)CloneWithPlacement(source, pageNumber, x, y);
    }

    internal void RegisterExtensionDescriptor(FragmentTranslationDescriptor descriptor)
    {
        _translator.RegisterDescriptor(descriptor);
    }

    internal void RegisterExtensionTranslator<TFragment>(
        Func<TFragment, int, float, float, LayoutFragment> translator)
        where TFragment : LayoutFragment
    {
        ArgumentNullException.ThrowIfNull(translator);

        RegisterExtensionDescriptor(
            FragmentTranslationDescriptor.Create<TFragment>(
                [nameof(LayoutFragment.Rect)],
                FragmentChildTraversalPolicy.None,
                (source, request, _) => translator(
                    source,
                    request.PageNumber,
                    request.DeltaX,
                    request.DeltaY)));
    }

    internal static IReadOnlyCollection<Type> GetBuiltInSupportedTypes()
    {
        return BuiltInDescriptors
            .Select(static descriptor => descriptor.FragmentType)
            .ToArray();
    }

    internal static IReadOnlyCollection<FragmentTranslationDescriptor> GetBuiltInDescriptors()
    {
        return BuiltInDescriptors.ToArray();
    }

    internal FragmentTranslationDescriptor GetDescriptor(Type fragmentType)
    {
        return _translator.GetDescriptor(fragmentType);
    }
}
