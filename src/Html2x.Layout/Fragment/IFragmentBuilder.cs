using Html2x.Layout.Box;

namespace Html2x.Layout.Fragment;

public interface IFragmentBuilder
{
    FragmentTree Build(BoxTree boxes);
}