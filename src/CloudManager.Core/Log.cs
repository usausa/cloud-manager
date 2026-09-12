namespace CloudManager;

using CloudManager.Models.Jobs;

internal static partial class Log
{
    // Job

    [LoggerMessage(Level = LogLevel.Information, Message = "Job start. id=[{id}], name=[{name}], operation=[{operation}]")]
    public static partial void InfoJobStart(this ILogger logger, long id, string name, JobOperation operation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job success. id=[{id}], name=[{name}], message=[{message}]")]
    public static partial void InfoJobSuccess(this ILogger logger, long id, string name, string? message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job failed. id=[{id}], name=[{name}], error=[{error}]")]
    public static partial void WarnJobFailed(this ILogger logger, long id, string name, string error, Exception ex);
}
