using Html2x.LayoutEngine;
using Microsoft.Extensions.Logging;

namespace Html2x;

public interface ILayoutBuilderFactory
{
    LayoutBuilder Create(ILoggerFactory? loggerFactory = null);
}
