namespace Html2x.Layout.Fragment.Stages;

public interface IFragmentBuildStage
{
    FragmentBuildState Execute(FragmentBuildState state);
}
