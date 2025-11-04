using Html2x.Layout;
using Microsoft.Extensions.Logging;

namespace Html2x;

public interface ILayoutBuilderFactory
{
    LayoutBuilder Create(ILoggerFactory? loggerFactory = null);
}
