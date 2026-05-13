namespace Html2x.Diagnostics.Contracts;

internal static class DiagnosticStageRunner
{
    public static T Run<T>(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        Func<T> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(action);

        DiagnosticStageEmitter.Started(diagnosticsSink, stage);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = action();
            cancellationToken.ThrowIfCancellationRequested();
            DiagnosticStageEmitter.Succeeded(diagnosticsSink, stage);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Canceled(diagnosticsSink, stage, exception.Message);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(diagnosticsSink, stage, exception.Message);
            throw;
        }
    }

    public static async Task<T> RunAsync<T>(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(action);

        DiagnosticStageEmitter.Started(diagnosticsSink, stage);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await action();
            cancellationToken.ThrowIfCancellationRequested();
            DiagnosticStageEmitter.Succeeded(diagnosticsSink, stage);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Canceled(diagnosticsSink, stage, exception.Message);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(diagnosticsSink, stage, exception.Message);
            throw;
        }
    }
}
