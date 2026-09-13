namespace CloudManager.Host.Application;

internal static partial class Log
{
    // Startup

    [LoggerMessage(Level = LogLevel.Information, Message = "Service start.")]
    public static partial void InfoServiceStart(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Runtime: os=[{osDescription}], framework=[{frameworkDescription}], rid=[{runtimeIdentifier}]")]
    public static partial void InfoServiceSettingsRuntime(this ILogger logger, string osDescription, string frameworkDescription, string runtimeIdentifier);

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment: version=[{version}], directory=[{directory}]")]
    public static partial void InfoServiceSettingsEnvironment(this ILogger logger, Version? version, string directory);

    [LoggerMessage(Level = LogLevel.Information, Message = "GCSettings: serverGC=[{isServerGC}], latencyMode=[{latencyMode}], largeObjectHeapCompactionMode=[{largeObjectHeapCompactionMode}]")]
    public static partial void InfoServiceSettingsGC(this ILogger logger, bool isServerGC, GCLatencyMode latencyMode, GCLargeObjectHeapCompactionMode largeObjectHeapCompactionMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "ThreadPool: workerThreads=[{workerThreads}], completionPortThreads=[{completionPortThreads}]")]
    public static partial void InfoServiceSettingsThreadPool(this ILogger logger, int workerThreads, int completionPortThreads);

    // Worker

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker start. worker=[{worker}]")]
    public static partial void InfoWorkerStart(this ILogger logger, string worker);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker stop. worker=[{worker}]")]
    public static partial void InfoWorkerStop(this ILogger logger, string worker);

    // Job

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job register failed. id=[{id}], name=[{name}], cron=[{cron}]")]
    public static partial void WarnJobRegisterFailed(this ILogger logger, long id, string name, string cron, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduler job error. job=[{job}]")]
    public static partial void ErrorSchedulerJobError(this ILogger logger, string job, Exception ex);

    // AWS

    [LoggerMessage(Level = LogLevel.Warning, Message = "AWS profile load failed.")]
    public static partial void WarnAwsProfileLoadFailed(this ILogger logger, Exception ex);

    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception.")]
    public static partial void ErrorUnhandledException(this ILogger logger, Exception ex);
}
