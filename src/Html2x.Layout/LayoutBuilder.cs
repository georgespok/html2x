using Html2x.Core.Layout;

namespace Html2x.Layout
{
    public class LayoutBuilder
    {
        public Core.Layout.HtmlLayout Build(string html)
        {
            return new HtmlLayout();
        }
    }
}
