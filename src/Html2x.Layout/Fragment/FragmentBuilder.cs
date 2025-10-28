using Html2x.Layout.Box;
using Html2x.Layout.Fragment.Stages;

namespace Html2x.Layout.Fragment;

public sealed class FragmentBuilder : IFragmentBuilder
{
    private readonly IReadOnlyList<IFragmentBuildStage> _stages =
    [
        new BlockFragmentStage(),
        new InlineFragmentStage(),
        new SpecializedFragmentStage(),
        new ZOrderStage()
    ];

    public FragmentTree Build(BoxTree boxes)
    {
        var context = new FragmentBuildContext(boxes);

        foreach (var stage in _stages)
        {
            stage.Execute(context);
        }

        return context.Result;
    }
}