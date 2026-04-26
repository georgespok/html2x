using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Translates fragment geometry and metadata when pagination places content on a page.
/// </summary>
internal interface IFragmentTranslator
{
    LayoutFragment Translate(LayoutFragment source, FragmentTranslationRequest request);

    LayoutFragment TranslateToPlacement(LayoutFragment source, int pageNumber, float x, float y);
}
