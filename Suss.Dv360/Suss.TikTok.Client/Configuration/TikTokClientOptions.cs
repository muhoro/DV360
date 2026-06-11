using System.ComponentModel.DataAnnotations;

namespace Suss.TikTok.Client.Configuration;

/// <summary>
/// Strongly-typed configuration surface for the TikTok Marketing API client.
/// <para>
/// Bind this from configuration (e.g., <c>appsettings.json</c> section "TikTok") and register it
/// via <c>services.AddTikTokClient(...)</c>. The options drive authentication, the target API
/// host/version, and the default advertiser scope used by the resource services.
/// </para>
/// </summary>
public sealed class TikTokClientOptions
{
    /// <summary>The configuration section name conventionally used to bind these options.</summary>
    public const string SectionName = "TikTok";

    /// <summary>
    /// The authentication strategy used to obtain an access token.
    /// Defaults to <see cref="AuthMode.AccessToken"/>.
    /// </summary>
    public AuthMode AuthMode { get; set; } = AuthMode.AccessToken;

    /// <summary>
    /// The TikTok developer application id (<c>app_id</c>).
    /// Required when <see cref="AuthMode"/> is <see cref="AuthMode.OAuthAuthorizationCode"/>.
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// The TikTok developer application secret.
    /// Required when <see cref="AuthMode"/> is <see cref="AuthMode.OAuthAuthorizationCode"/>.
    /// </summary>
    public string? AppSecret { get; set; }

    /// <summary>
    /// A one-time OAuth authorization code returned to the app's redirect URI after the user
    /// grants consent. Exchanged for an <see cref="AccessToken"/> when
    /// <see cref="AuthMode"/> is <see cref="AuthMode.OAuthAuthorizationCode"/>.
    /// </summary>
    public string? AuthCode { get; set; }

    /// <summary>
    /// Optional absolute TikTok OAuth authorization URL. Defaults to
    /// <c>{BaseUrl}/portal/auth</c>. Override this if TikTok provides an environment-specific
    /// authorization host for your app.
    /// </summary>
    public string? AuthorizationUrl { get; set; }

    /// <summary>
    /// Default OAuth scopes requested when building an authorization URL.
    /// Scopes can also be supplied per call to <c>ITikTokOAuthService.BuildAuthorizationUrl</c>.
    /// </summary>
    public IList<string> Scopes { get; set; } = [];

    /// <summary>
    /// Optional platform-owned identity id used when customers run ads through your managed TikTok
    /// advertiser account.
    /// </summary>
    public string? ManagedIdentityId { get; set; }

    /// <summary>
    /// Optional platform-owned identity type used with <see cref="ManagedIdentityId"/>.
    /// </summary>
    public string? ManagedIdentityType { get; set; }

    /// <summary>
    /// A pre-issued long-lived access token. Required when <see cref="AuthMode"/> is
    /// <see cref="AuthMode.AccessToken"/>; ignored otherwise (the token is fetched instead).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The default TikTok advertiser id under which resources are created/queried. Most Marketing
    /// API endpoints require an <c>advertiser_id</c>; services use this value when one is not
    /// passed explicitly.
    /// </summary>
    [Required]
    public required string AdvertiserId { get; set; }

    /// <summary>
    /// The base URL for the TikTok Marketing API. Override to target the sandbox environment.
    /// Defaults to the production host.
    /// </summary>
    public string BaseUrl { get; set; } = "https://business-api.tiktok.com";

    /// <summary>
    /// The Marketing API version segment used when composing endpoint paths (e.g., <c>v1.3</c>).
    /// </summary>
    public string ApiVersion { get; set; } = "v1.3";
}
