using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting.WindowsServices;

using MudBlazor.Services;

using Serilog;

using Smart.AspNetCore;
using Smart.AspNetCore.ApplicationModels;
using Smart.Data.Accessor.Extensions.DependencyInjection;
using Smart.Data;

using CloudConsole.Server.Application;

using Smart.Data.Accessor;

using CloudConsole.Accessor;
using CloudConsole.Services;

using Smart.AspNetCore.Filters;

#pragma warning disable CA1852

// System
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

// Service
builder.Host
    .UseWindowsService()
    .UseSystemd();

// Add framework Services.
builder.Services.AddHttpContextAccessor();

// Log
builder.Logging.ClearProviders();
builder.Host
    .UseSerilog((hostingContext, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
    });

// Route
builder.Services.Configure<RouteOptions>(options =>
{
    options.AppendTrailingSlash = true;
});

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddMudServices(config =>
{
    // Snackbar
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
});

builder.Services.AddSingleton<IErrorBoundaryLogger, ErrorBoundaryLogger>();

// API
builder.Services.AddExceptionLogging();
builder.Services.AddTimeLogging(options =>
{
    options.Threshold = 10_000;
});
builder.Services.AddSingleton<ExceptionStatusFilter>();

builder.Services
    .AddControllers(options =>
    {
        options.Filters.AddExceptionLogging();
        options.Filters.AddTimeLogging();
        options.Filters.AddService<ExceptionStatusFilter>();
        options.Conventions.Add(new LowercaseControllerModelConvention());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        options.JsonSerializerOptions.Converters.Add(new CloudConsole.Components.Json.DateTimeConverter());
    });

// Health
builder.Services.AddHealthChecks();

// Validation
ValidatorOptions.Global
    .UseDisplayName()
    .UseCustomLocalizeMessage();
ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Continue;
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

// HTTP
builder.Services.AddHttpClient();

// Data
var connectionStringBuilder = new SqliteConnectionStringBuilder
{
    DataSource = "Data.db",
    Pooling = true,
    Cache = SqliteCacheMode.Shared
};
var connectionString = connectionStringBuilder.ConnectionString;
builder.Services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(connectionString)));
builder.Services.AddSingleton<IDialect>(new DelegateDialect(
    static ex => ex is SqliteException { SqliteErrorCode: 1555 },
    static x => Regex.Replace(x, @"[%_]", "[$0]")));
builder.Services.AddDataAccessor();

// Mapper
builder.Services.AddSingleton<IMapper>(new Mapper(new MapperConfiguration(c =>
{
    c.AddProfile<MappingProfile>();
})));

// Service
builder.Services.AddSingleton<DataService>();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline
//--------------------------------------------------------------------------------
var app = builder.Build();

// Prepare
if (!File.Exists(connectionStringBuilder.DataSource))
{
    var accessor = app.Services.GetRequiredService<IAccessorResolver<IDataAccessor>>().Accessor;
    accessor.Create();
}

// Serilog
if (!app.Environment.IsProduction())
{
    app.UseSerilogRequestLogging(options =>
    {
        options.IncludeQueryInRequestPath = true;
    });
}

// Forwarded headers
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// HTTPS redirection
app.UseHttpsRedirection();

// Static files
app.UseStaticFiles();

// Routing
app.UseRouting();

// Authentication
app.UseAuthentication();
app.UseAuthorization();

// API
app.MapControllers();

// Blazor
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Run
app.Run();
