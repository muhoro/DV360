using Microsoft.Extensions.DependencyInjection;
using Suss.Dv360.Client.Auth;
using Suss.Dv360.Client.Infrastructure;
using Suss.Dv360.Client.Services;

namespace Suss.Dv360.Client.Configuration;

/// <summary>
/// Extension methods for registering the DV360 client library into a
/// <see cref="IServiceCollection"/> dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all DV360 client services, authentication providers, and infrastructure
    /// components into the dependency injection container.
    /// <para>
    /// Call this method in your host builder’s service configuration to make
    /// <see cref="ICampaignWorkflowService"/> and the individual resource services
    /// (campaigns, creatives, insertion orders, line items) available for injection.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection to register DV360 services into.</param>
    /// <param name="configure">
    /// A delegate that configures <see cref="Dv360ClientOptions"/>, including authentication
    /// mode, key paths, and application name.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Dv360ClientOptions.AuthMode"/> is set to an unsupported value.
    /// </exception>
    public static IServiceCollection AddDv360Client(this IServiceCollection services, Action<Dv360ClientOptions> configure)
    {
        // Bind the options so they are available via IOptions<Dv360ClientOptions> throughout the container.
        services.Configure(configure);

        // Materialize options immediately to determine which auth provider to register.
        var options = new Dv360ClientOptions();
        configure(options);

        // Register the appropriate auth provider based on the configured auth mode.
        switch (options.AuthMode)
        {
            case AuthMode.ServiceAccount:
                services.AddSingleton<IDv360AuthProvider, ServiceAccountAuthProvider>();
                break;
            case AuthMode.OAuthUser:
                services.AddSingleton<IDv360AuthProvider, OAuthUserAuthProvider>();
                break;
            default:
                throw new ArgumentException($"Unsupported auth mode: {options.AuthMode}");
        }

        // The factory is a singleton because it caches the authenticated DisplayVideoService instance.
        services.AddSingleton<IDisplayVideoServiceFactory, DisplayVideoServiceFactory>();

        // Resource services are scoped so they align with per-request lifetimes
        // (e.g., in ASP.NET Core) while sharing the singleton factory/auth provider.
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ICreativeService, CreativeService>();
        services.AddScoped<IInsertionOrderService, InsertionOrderService>();
        services.AddScoped<ILineItemService, LineItemService>();
        services.AddScoped<ITargetingService, TargetingService>();
        services.AddScoped<IGeoRegionService, GeoRegionService>();
        services.AddScoped<ICampaignWorkflowService, CampaignWorkflowService>();

        return services;
    }
}
