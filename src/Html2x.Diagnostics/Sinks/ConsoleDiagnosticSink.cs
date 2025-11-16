using System.Globalization;
using System.Text;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Diagnostics.Sinks;

public sealed class ConsoleDiagnosticSink(ConsoleDiagnosticSinkOptions options) : IDiagnosticSink
{
    private readonly ConsoleDiagnosticSinkOptions _options = options?.Clone() ?? throw new ArgumentNullException(nameof(options));
    private readonly object _gate = new();

    public string SinkId => _options.SinkId;

    public void Publish(DiagnosticsModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var writer = _options.Writer ?? Console.Out;
        var message = FormatMessage(model);

        lock (_gate)
        {
            writer.WriteLine(message);
            writer.Flush();
        }
    }

    private string FormatMessage(DiagnosticsModel model)
    {
        var builder = new StringBuilder();
        builder.Append("[Diagnostics] ");

        if (_options.IncludeSessionName)
        {
            builder.Append('(');
            builder.Append(model.Session.Name);
            builder.Append(") ");
        }

        builder.Append(model.Event.Category);
        builder.Append(' ');
        builder.Append(model.Event.Kind);

        if (_options.IncludeTimestamp)
        {
            builder.Append(' ');
            builder.Append(model.Event.Timestamp.ToString("O", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
