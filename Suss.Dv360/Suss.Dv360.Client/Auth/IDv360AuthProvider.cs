using Google.Apis.Http;

namespace Suss.Dv360.Client.Auth;

/// <summary>
/// Abstracts the authentication mechanism for DV360 API access.
/// <para>
/// This is the primary extensibility point for authentication. The library ships with
/// two built-in implementations:
/// <list type="bullet">
///   <item><description><see cref="ServiceAccountAuthProvider"/> – server-to-server via a Google service account JSON key.</description></item>
///   <item><description><see cref="OAuthUserAuthProvider"/> – interactive OAuth 2.0 consent flow for end users.</description></item>
/// </list>
/// Implement this interface to support additional authentication strategies
/// (e.g., Workload Identity Federation, API keys, or custom token providers).
/// </para>
/// </summary>
public interface IDv360AuthProvider
{
    /// <summary>
    /// Obtains a credential that the Google SDK uses to initialize authenticated HTTP requests
    /// to the Display &amp; Video 360 API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the credential acquisition (e.g., during app shutdown).</param>
    /// <returns>
    /// An <see cref="IConfigurableHttpClientInitializer"/> that attaches authorization headers
    /// to every outgoing request made by the <c>DisplayVideoService</c>.
    /// </returns>
    Task<IConfigurableHttpClientInitializer> GetCredentialAsync(CancellationToken cancellationToken = default);
}
