using Html2x.Abstractions.Diagnostics;
using Html2x.LayoutEngine;

namespace Html2x;

public interface ILayoutBuilderFactory
{
    LayoutBuilder Create(IDiagnosticSession? diagnosticSession = null);
}
