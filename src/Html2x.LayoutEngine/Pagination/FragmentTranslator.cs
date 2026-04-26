using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Translates fragment subtrees using explicit descriptors for built-in and extension fragment types.
/// </summary>
internal sealed class FragmentTranslator : IFragmentTranslator
{
    private readonly Dictionary<Type, FragmentTranslationDescriptor> _descriptors;

    public FragmentTranslator(IEnumerable<FragmentTranslationDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        _descriptors = descriptors.ToDictionary(static descriptor => descriptor.FragmentType);
    }

    public IReadOnlyCollection<FragmentTranslationDescriptor> Descriptors =>
        _descriptors.Values.ToArray();

    public static FragmentTranslator CreateDefault()
    {
        return new FragmentTranslator(BuiltInFragmentTranslationDescriptors.Create());
    }

    public LayoutFragment Translate(LayoutFragment source, FragmentTranslationRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);

        var sourceType = source.GetType();
        if (!_descriptors.TryGetValue(sourceType, out var descriptor))
        {
            throw new NotSupportedException(
                $"No pagination translator registered for fragment type '{sourceType.Name}'. " +
                $"Configure a {nameof(FragmentCoordinateTranslator)} for this paginator with " +
                $"{nameof(FragmentCoordinateTranslator.RegisterExtensionDescriptor)} or " +
                $"{nameof(FragmentCoordinateTranslator.RegisterExtensionTranslator)}.");
        }

        return descriptor.Translate(source, request, this);
    }

    public LayoutFragment TranslateToPlacement(LayoutFragment source, int pageNumber, float x, float y)
    {
        return Translate(source, FragmentTranslationRequest.FromPlacement(source, pageNumber, x, y));
    }

    public void RegisterDescriptor(FragmentTranslationDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _descriptors[descriptor.FragmentType] = descriptor;
    }

    public IReadOnlyCollection<Type> GetSupportedTypes()
    {
        return _descriptors.Keys.ToArray();
    }

    public FragmentTranslationDescriptor GetDescriptor(Type fragmentType)
    {
        ArgumentNullException.ThrowIfNull(fragmentType);

        if (_descriptors.TryGetValue(fragmentType, out var descriptor))
        {
            return descriptor;
        }

        throw new NotSupportedException(
            $"No pagination translator registered for fragment type '{fragmentType.Name}'.");
    }
}
