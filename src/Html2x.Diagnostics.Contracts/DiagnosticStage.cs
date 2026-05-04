namespace Html2x.Diagnostics.Contracts;

internal static class DiagnosticStage
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
            DiagnosticStageEmitter.Succeeded(diagnosticsSink, stage);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Cancelled(diagnosticsSink, stage, exception.Message);
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
            DiagnosticStageEmitter.Succeeded(diagnosticsSink, stage);
            return result;
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Cancelled(diagnosticsSink, stage, exception.Message);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(diagnosticsSink, stage, exception.Message);
            throw;
        }
    }
}
