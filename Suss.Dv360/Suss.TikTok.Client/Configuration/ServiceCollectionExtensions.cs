using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Suss.TikTok.Client.Auth;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Infrastructure;
using Suss.TikTok.Client.Services;

namespace Suss.TikTok.Client.Configuration;

/// <summary>
/// Dependency-injection registration helpers for the TikTok Marketing API client.
/// <para>
/// A single call to <c>AddTikTokClient(...)</c> wires up options, the auth provider (selected by
/// <see cref="AuthMode"/>), the HTTP transport, and all resource/workflow services. Consumers then
/// resolve <see cref="ITikTokCampaignWorkflowService"/> (or any individual service) from DI.
/// </para>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the TikTok client and its dependencies using an options-configuring delegate.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configureOptions">A delegate that populates <see cref="TikTokClientOptions"/>.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddTikTokClient(
        this IServiceCollection services,
        Action<TikTokClientOptions> configureOptions)
    {
        services.AddOptions<TikTokClientOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations();

        return services.AddTikTokClientCore();
    }

    /// <summary>
    /// Registers the TikTok client binding options from a configuration section
    /// (default section name: <see cref="TikTokClientOptions.SectionName"/>).
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configuration">The configuration root/section containing the "TikTok" section.</param>
    /// <param name="sectionName">Optional override for the configuration section name.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddTikTokClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = TikTokClientOptions.SectionName)
    {
        services.AddOptions<TikTokClientOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations();

        return services.AddTikTokClientCore();
    }

    /// <summary>
    /// Shared registration logic: auth provider, typed HTTP clients, transport, and services.
    /// </summary>
    private static IServiceCollection AddTikTokClientCore(this IServiceCollection services)
    {
        // Auth provider: chosen at runtime based on the configured AuthMode. Registered as a
        // singleton so any token exchange/caching happens once for the app lifetime.
        services.TryAddSingleton<ITikTokAuthProvider>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TikTokClientOptions>>().Value;
            return options.AuthMode switch
            {
                AuthMode.OAuthAuthorizationCode => ActivatorUtilities.CreateInstance<OAuthAuthCodeAuthProvider>(sp),
                _ => ActivatorUtilities.CreateInstance<StaticTokenAuthProvider>(sp)
            };
        });

        // The OAuth provider needs its own HttpClient (no Access-Token header).
        services.AddHttpClient<ITikTokOAuthService, TikTokOAuthService>();

        // Transport: a typed HttpClient backs the envelope-aware API client.
        services.AddHttpClient<ITikTokApiClient, TikTokApiClient>();

        // Asset uploads may receive campaign-level remote URLs that need to be downloaded first.
        services.AddHttpClient(nameof(TikTokAssetService));

        // Resource services (scoped: cheap, stateless, share the singleton transport).
        services.TryAddScoped<ITikTokAssetService, TikTokAssetService>();
        services.TryAddScoped<ITikTokAudienceService, TikTokAudienceService>();
        services.TryAddScoped<ITikTokCampaignService, TikTokCampaignService>();
        services.TryAddScoped<ITikTokAdGroupService, TikTokAdGroupService>();
        services.TryAddScoped<ITikTokAdService, TikTokAdService>();
        services.TryAddScoped<ITikTokCampaignWorkflowService, TikTokCampaignWorkflowService>();

        return services;
    }
}
