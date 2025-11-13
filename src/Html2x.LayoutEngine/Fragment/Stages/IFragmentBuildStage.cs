namespace Html2x.LayoutEngine.Fragment.Stages;

public interface IFragmentBuildStage
{
    FragmentBuildState Execute(FragmentBuildState state);
}
