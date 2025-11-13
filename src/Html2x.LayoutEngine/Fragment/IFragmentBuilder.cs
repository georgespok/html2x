using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public interface IFragmentBuilder
{
    FragmentTree Build(BoxTree boxes);
}