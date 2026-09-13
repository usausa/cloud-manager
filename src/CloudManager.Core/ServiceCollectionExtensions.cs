namespace CloudManager;

using BunnyTail.ServiceRegistration;

using Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    [ServiceRegistration(Lifetime.Singleton, "^Job.*Service$")]
    public static partial IServiceCollection AddCoreServices(this IServiceCollection services);

    // AWS services are scoped because they depend on the per-circuit AwsClientFactory
    [ServiceRegistration(Lifetime.Scoped, "Service$", Namespace = "CloudManager.Services.Aws")]
    public static partial IServiceCollection AddAwsServices(this IServiceCollection services);
}
