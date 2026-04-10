namespace Suss.Dv360.Client.Configuration;

/// <summary>
/// Configuration options for the DV360 client library.
/// <para>
/// Bind this from a configuration section (e.g., <c>"Dv360"</c> in <c>appsettings.json</c>)
/// or configure it programmatically via the <see cref="ServiceCollectionExtensions.AddDv360Client"/>
/// delegate. At minimum, set <see cref="AuthMode"/> and the corresponding credential properties.
/// </para>
/// </summary>
public sealed class Dv360ClientOptions
{
    /// <summary>
    /// The authentication strategy to use when connecting to the DV360 API.
    /// Defaults to <see cref="AuthMode.ServiceAccount"/> for server-to-server scenarios.
    /// </summary>
    public AuthMode AuthMode { get; set; } = AuthMode.ServiceAccount;

    /// <summary>
    /// Absolute or relative path to the Google service account JSON key file.
    /// Required when <see cref="AuthMode"/> is <see cref="AuthMode.ServiceAccount"/>.
    /// Ignored when using <see cref="AuthMode.OAuthUser"/>.
    /// </summary>
    public string? ServiceAccountKeyPath { get; set; }

    /// <summary>
    /// OAuth 2.0 client ID obtained from the Google Cloud Console credentials page.
    /// Required when <see cref="AuthMode"/> is <see cref="AuthMode.OAuthUser"/>.
    /// Ignored when using <see cref="AuthMode.ServiceAccount"/>.
    /// </summary>
    public string? OAuthClientId { get; set; }

    /// <summary>
    /// OAuth 2.0 client secret obtained from the Google Cloud Console credentials page.
    /// Required when <see cref="AuthMode"/> is <see cref="AuthMode.OAuthUser"/>.
    /// Ignored when using <see cref="AuthMode.ServiceAccount"/>.
    /// </summary>
    public string? OAuthClientSecret { get; set; }

    /// <summary>
    /// Directory path for storing OAuth 2.0 refresh tokens so users are not re-prompted
    /// on subsequent runs. Defaults to <c>"tokens"</c>.
    /// </summary>
    public string TokenStorePath { get; set; } = "tokens";

    /// <summary>
    /// Application name sent with every API request in the <c>User-Agent</c> header.
    /// Useful for request attribution in Google Cloud audit logs.
    /// Defaults to <c>"Suss.Dv360.Client"</c>.
    /// </summary>
    public string ApplicationName { get; set; } = "Suss.Dv360.Client";
}
