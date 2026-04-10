namespace Suss.Dv360.Client.Configuration;

/// <summary>
/// Specifies the authentication strategy used to connect to the DV360 API.
/// Configured via <see cref="Dv360ClientOptions.AuthMode"/>.
/// </summary>
public enum AuthMode
{
    /// <summary>
    /// Authenticate using a Google service account JSON key file.
    /// Best suited for unattended server-to-server workloads.
    /// </summary>
    ServiceAccount,

    /// <summary>
    /// Authenticate using the interactive OAuth 2.0 user consent flow.
    /// Best suited for developer tools and desktop utilities.
    /// </summary>
    OAuthUser
}
