namespace CloudManager;

using BunnyTail.ServiceRegistration;

using Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    // AWS サービスは回線ごとのAwsClientFactoryに依存するためScoped
    [ServiceRegistration(Lifetime.Scoped, "Service$", Namespace = "CloudManager.Services.Aws")]
    public static partial IServiceCollection AddAwsServices(this IServiceCollection services);
}
