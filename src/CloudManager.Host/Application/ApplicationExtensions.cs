namespace CloudManager.Host.Application;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using Amazon;

using CloudManager.Accessors;
using CloudManager.Host.Components;
using CloudManager.Host.Endpoints;
using CloudManager.Host.Infrastructure.Aws;
using CloudManager.Host.Infrastructure.ExceptionHandling;
using CloudManager.Host.Infrastructure.HealthChecks;
using CloudManager.Host.Infrastructure.Jobs;
using CloudManager.Host.Workers;
using CloudManager.Infrastructure.Aws;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using MiniDataProfiler;
using MiniDataProfiler.Listener.Logging;

using Mofucat.JobScheduler;

using MudBlazor;
using MudBlazor.Services;

using Serilog;

using Smart.Data;

public static class ApplicationExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string ApiPathPrefix = "/api";

    //--------------------------------------------------------------------------------
    // System
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureSystem(this WebApplicationBuilder builder)
    {
        // Path
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);

        // Encoding
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // AWS SDK collections are empty instead of null
        AWSConfigs.InitializeCollections = true;

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Host
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHost(this WebApplicationBuilder builder)
    {
        // Service
        builder.Services
            .AddWindowsService()
            .AddSystemd();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Logging
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureLogging(this IHostApplicationBuilder builder)
    {
        // Application log
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(options =>
        {
            options.ReadFrom.Configuration(builder.Configuration);
        });

        // HTTP log
        builder.Services.AddHttpLogging(static options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod |
                                    HttpLoggingFields.RequestPath |
                                    HttpLoggingFields.ResponseStatusCode |
                                    HttpLoggingFields.Duration;
        });

        return builder;
    }

    public static WebApplication UseLogging(this WebApplication app)
    {
        var setting = app.Services.GetRequiredService<LogSetting>();
        if (setting.HttpLog)
        {
            app.UseWhen(
                static context => context.Request.Path.StartsWithSegments(ApiPathPrefix, StringComparison.OrdinalIgnoreCase),
                static b => b.UseHttpLogging());
        }

        return app;
    }

    //--------------------------------------------------------------------------------
    // Http
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHttp(this IHostApplicationBuilder builder)
    {
        // XForward
        builder.Services.Configure<ForwardedHeadersOptions>(static options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Do not restrict to local network/proxy
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // API
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureApi(this IHostApplicationBuilder builder)
    {
        // JSON
        builder.Services.ConfigureHttpJsonOptions(static options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = NamingPolicy.JsonPropertyNaming;
            options.SerializerOptions.DictionaryKeyPolicy = NamingPolicy.JsonDictionaryKeyNaming;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        });

        // Error handler
        builder.Services.AddProblemDetails(static options =>
        {
            options.CustomizeProblemDetails = static context =>
            {
                context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            };
        });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    public static WebApplication UseErrorHandler(this WebApplication app)
    {
        // Page: not found (re-execution does not work inside a UseWhen branch)
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        // API: ProblemDetails, status code as is
        app.UseWhen(
            static context => context.Request.Path.StartsWithSegments(ApiPathPrefix, StringComparison.OrdinalIgnoreCase),
            static b =>
            {
                b.UseExceptionHandler();
                b.Use(static (context, next) =>
                {
                    context.Features.Get<IStatusCodePagesFeature>()?.Enabled = false;

                    return next(context);
                });
            });

        // Page: error page
        app.UseWhen(
            static context => !context.Request.Path.StartsWithSegments(ApiPathPrefix, StringComparison.OrdinalIgnoreCase),
            static b => b.UseExceptionHandler("/error", createScopeForErrors: true));

        return app;
    }

    //--------------------------------------------------------------------------------
    // Compress
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureCompression(this IHostApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(static options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        return builder;
    }

    public static WebApplication UseCompression(this WebApplication app)
    {
        app.UseResponseCompression();

        return app;
    }

    //--------------------------------------------------------------------------------
    // Blazor
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureBlazor(this IHostApplicationBuilder builder)
    {
        // Razor components
        builder.Services
            .AddRazorComponents()
            .AddInteractiveServerComponents();

        // Error boundary logging
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger, Infrastructure.Components.ErrorBoundaryLogger>();

        // MudBlazor
        builder.Services.AddMudServices(static options =>
        {
            options.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            options.SnackbarConfiguration.PreventDuplicates = true;
            options.SnackbarConfiguration.NewestOnTop = false;
            options.SnackbarConfiguration.ShowCloseIcon = true;
            options.SnackbarConfiguration.VisibleStateDuration = 5000;
            options.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Health
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHealth(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck<DatabaseHealthCheck>("database");

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Components
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureComponents(this IHostApplicationBuilder builder)
    {
        // System
        builder.Services.AddSingleton(TimeProvider.System);

        // Data
        builder.Services.AddSingleton<IDbProvider>(static p =>
        {
            var configuration = p.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Default");

            var listener = CreateProfileListener(p, p.GetRequiredService<ProfilerSetting>());
            if (listener is not null)
            {
                return new DelegateDbProvider(() => new ProfileDbConnection(listener, new SqliteConnection(connectionString)));
            }

            return new DelegateDbProvider(() => new SqliteConnection(connectionString));
        });
        builder.Services.AddSingleton<IDialect>(new DelegateDialect(
            static ex => ex is SqliteException { SqliteErrorCode: 19 } or SqliteException { SqliteExtendedErrorCode: 1555 or 2067 },
            static x => Regex.Replace(x, "[%_]", "[$0]")));
        builder.Services.AddDataAccessors(typeof(JobAccessor).Assembly);

        // Service
        builder.Services.AddCoreServices();

        // Job
        builder.Services.AddSingleton<JobScheduler>();
        builder.Services.AddSingleton<JobManager>();
        builder.Services.AddHostedService<JobSchedulerWorker>();

        // AWS
        builder.Services.AddScoped<AwsSession>();
        builder.Services.AddScoped(static p =>
        {
            // Resolved from the session per client because the profile can be switched in the UI
            var session = p.GetRequiredService<AwsSession>();
            return new AwsClientFactory(() => CredentialResolver.Resolve(session.ProfileName, session.Region?.SystemName));
        });
        builder.Services.AddAwsServices();

        // Setting
        builder.Services.AddOptions<ProfilerSetting>().BindConfiguration("Profiler").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<ProfilerSetting>>().Value);
        builder.Services.AddOptions<LogSetting>().BindConfiguration("Log").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<LogSetting>>().Value);
        builder.Services.AddOptions<AwsSetting>().BindConfiguration("Aws").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<AwsSetting>>().Value);
        builder.Services.AddOptions<JobExecutionOptions>().BindConfiguration("Job").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<JobExecutionOptions>>().Value);

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Information
    //--------------------------------------------------------------------------------

    public static void LogStartupInformation(this WebApplication app)
    {
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);

        app.Logger.InfoServiceStart();
        app.Logger.InfoServiceSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
        app.Logger.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.CurrentDirectory);
        app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
        app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);
    }

    //--------------------------------------------------------------------------------
    // End point
    //--------------------------------------------------------------------------------

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // Static assets
        app.MapStaticAssets();

        // Blazor
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // API
        app.MapS3Endpoints();

        // Health
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = static r => r.Tags.Contains("live")
        });

        return app;
    }

    //--------------------------------------------------------------------------------
    // Startup
    //--------------------------------------------------------------------------------

    public static ValueTask InitializeApplicationAsync(this WebApplication app)
    {
        // Prepare database
        app.Services.GetRequiredService<JobService>().CreateTable();
        app.Services.GetRequiredService<JobLogService>().CreateTable();

        return ValueTask.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Profiler
    //--------------------------------------------------------------------------------

    // Writes SQL traces to the log when enabled by settings
    private static LoggingListener? CreateProfileListener(IServiceProvider provider, ProfilerSetting setting)
    {
        if (!setting.SqlLog.Enable)
        {
            return null;
        }

        var option = new LoggingListenerOption
        {
            OutputParameter = setting.SqlLog.OutputParameter,
            ElapsedThreshold = TimeSpan.FromMilliseconds(setting.SqlLog.ElapsedThresholdMilliseconds)
        };
        return new LoggingListener(provider.GetRequiredService<ILogger<LoggingListener>>(), option);
    }
}
