using Html2x.LayoutEngine.Box;

namespace Html2x.LayoutEngine.Fragment;

public interface IFragmentBuilder
{
    FragmentTree Build(BoxTree boxes);
}