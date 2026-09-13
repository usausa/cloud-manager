namespace CloudManager.Host.Application;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using CloudManager.Accessors;
using CloudManager.Host.Application.Telemetry;
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
using Microsoft.FeatureManagement;

using MiniDataProfiler;
using MiniDataProfiler.Listener.Logging;
using MiniDataProfiler.Listener.OpenTelemetry;

using Mofucat.JobScheduler;

using MudBlazor;
using MudBlazor.Services;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

        // Feature management
        builder.Services.AddFeatureManagement();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Logging
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureLogging(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = builder.Configuration.IsOtelExporterEnabled();

        // Application log
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(
            options =>
            {
                options.ReadFrom.Configuration(builder.Configuration);
            },
            writeToProviders: useOtlpExporter);

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
        // Add services to the container.
        builder.Services.AddHttpContextAccessor();

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

        // Validation
        builder.Services.AddValidation();

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
        // Page: not found page (UseWhen のブランチ内では再ルーティングされないためアプリ直下に置く)
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        // API: ProblemDetails, status code as is
        app.UseWhen(
            static context => context.Request.Path.StartsWithSegments(ApiPathPrefix, StringComparison.OrdinalIgnoreCase),
            static b =>
            {
                b.UseExceptionHandler();
                b.Use(static (context, next) =>
                {
                    var feature = context.Features.Get<IStatusCodePagesFeature>();
                    if (feature is not null)
                    {
                        feature.Enabled = false;
                    }

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
    // OpenApi
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureOpenApi(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(static options =>
        {
            options.AddDocumentTransformer(static (document, _, _) =>
            {
                document.Info.Title = "CloudManager API";
                document.Info.Version = "v1";
                document.Info.Description = "CloudManager API.";
                return Task.CompletedTask;
            });
        });

        return builder;
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
            options.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
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
    // Telemetry
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureTelemetry(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = builder.Configuration.IsOtelExporterEnabled();

        var prometheusSection = builder.Configuration.GetSection("Prometheus");
        var prometheusUri = prometheusSection.GetValue<string>("Uri")!;
        var usePrometheusExporter = !String.IsNullOrEmpty(prometheusUri);

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(config =>
            {
                config.AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
                    serviceInstanceId: Environment.MachineName);
            });

        // Log
        if (useOtlpExporter)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
            builder.Services.Configure<OpenTelemetryLoggerOptions>(static logging =>
            {
                logging.AddOtlpExporter();
            });
        }

        // Metrics
        if (useOtlpExporter || usePrometheusExporter)
        {
            telemetry
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddApplicationInstrumentation();

                    if (useOtlpExporter)
                    {
                        metrics.AddOtlpExporter();
                    }

                    if (usePrometheusExporter)
                    {
                        var prometheusEndpoint = new Uri(prometheusUri);
                        metrics.AddPrometheusHttpListener(config =>
                        {
                            config.Host = prometheusEndpoint.Host;
                            config.Port = prometheusEndpoint.Port;
                        });
                    }
                });
        }

        // Trace
        if (useOtlpExporter)
        {
            telemetry
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(static options =>
                        {
                            options.Filter = static context =>
                            {
                                var path = context.Request.Path;
                                return !path.StartsWithSegments(AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments(HealthEndpointPath, StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/redoc", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase) &&
                                       !path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase);
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .AddMiniDataProfilerInstrumentation()
                        .AddApplicationInstrumentation();

                    tracing.AddOtlpExporter();
                });
        }

        // Custom instrument
        builder.Services.AddApplicationInstrument();

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

        // Cache
        builder.Services.AddMemoryCache();

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
            // プロファイルは画面で切り替えられるため、クライアント生成のたびにセッションから解決する
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

        var prometheusSection = app.Configuration.GetSection("Prometheus");
        var prometheusUri = prometheusSection.GetValue("Uri", string.Empty);

        app.Logger.InfoServiceStart();
        app.Logger.InfoServiceSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
        app.Logger.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.CurrentDirectory);
        app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
        app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);
        app.Logger.InfoServiceSettingsTelemetry(app.Configuration.GetOtelExporterEndpoint(), prometheusUri);
    }

    //--------------------------------------------------------------------------------
    // End point
    //--------------------------------------------------------------------------------

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // Develop
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            // [MEMO] Add yaml support
            app.MapOpenApi("/openapi/{documentName}.yaml");

            // NSwag UI (SwaggerUI / ReDoc) using MapOpenApi generated specification
            app.UseSwaggerUi(static options =>
            {
                options.DocumentPath = "/openapi/v1.json";
            });
            app.UseReDoc(static options =>
            {
                options.Path = "/redoc";
                options.DocumentPath = "/openapi/v1.json";
            });
        }

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
        // Prepare instrument
        app.Services.GetRequiredService<ApplicationInstrument>();

        // Prepare database
        app.Services.GetRequiredService<JobService>().CreateTable();
        app.Services.GetRequiredService<JobLogService>().CreateTable();

        return ValueTask.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Configuration
    //--------------------------------------------------------------------------------

    private static bool IsOtelExporterEnabled(this IConfiguration configuration) =>
        !String.IsNullOrWhiteSpace(configuration.GetOtelExporterEndpoint());

    //--------------------------------------------------------------------------------
    // Profiler
    //--------------------------------------------------------------------------------

    // SQLトレースをログ/テレメトリそれぞれの設定で有効化する
    private static IProfileListener? CreateProfileListener(IServiceProvider provider, ProfilerSetting setting)
    {
        var listeners = new List<IProfileListener>();
        if (setting.SqlLog.Enable)
        {
            var option = new LoggingListenerOption
            {
                OutputParameter = setting.SqlLog.OutputParameter,
                ElapsedThreshold = TimeSpan.FromMilliseconds(setting.SqlLog.ElapsedThresholdMilliseconds)
            };
            listeners.Add(new LoggingListener(provider.GetRequiredService<ILogger<LoggingListener>>(), option));
        }

        if (setting.SqlTelemetry.Enable)
        {
            listeners.Add(new OpenTelemetryListener(new OpenTelemetryListenerOption()));
        }

        return listeners.Count switch
        {
            0 => null,
            1 => listeners[0],
            _ => new ChainListener(listeners)
        };
    }

    private static string GetOtelExporterEndpoint(this IConfiguration configuration) =>
        configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? string.Empty;
}
