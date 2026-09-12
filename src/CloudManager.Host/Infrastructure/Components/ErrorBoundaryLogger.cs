namespace CloudManager.Host.Infrastructure.Components;

using CloudManager.Host.Application;

using Microsoft.AspNetCore.Components.Web;

public sealed class ErrorBoundaryLogger : IErrorBoundaryLogger
{
    private readonly ILogger<ErrorBoundaryLogger> log;

    public ErrorBoundaryLogger(ILogger<ErrorBoundaryLogger> log)
    {
        this.log = log;
    }

    public ValueTask LogErrorAsync(Exception exception)
    {
        log.ErrorUnhandledException(exception);
        return ValueTask.CompletedTask;
    }
}
