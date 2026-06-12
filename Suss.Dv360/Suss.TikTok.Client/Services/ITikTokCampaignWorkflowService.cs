using Suss.TikTok.Client.Models;
using Suss.TikTok.Client.Auth;

namespace Suss.TikTok.Client.Services;

/// <summary>
/// Orchestrates the full end-to-end TikTok campaign creation workflow, wiring together asset upload,
/// audience creation, and the campaign → ad group → ad hierarchy in the correct order.
/// </summary>
public interface ITikTokCampaignWorkflowService
{
    /// <summary>
    /// Builds the TikTok OAuth consent URL for a workflow-managed authorization mode.
    /// </summary>
    string BuildAuthorizationUrl(
        TikTokAuthMode authMode,
        string redirectUri,
        string state,
        IEnumerable<string>? scopes = null,
        IReadOnlyDictionary<string, string?>? additionalParameters = null);

    /// <summary>
    /// Exchanges a TikTok OAuth callback authorization code for token data.
    /// </summary>
    Task<TikTokOAuthTokenResult> ExchangeAuthorizationCodeAsync(
        string authCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a customer-owned advertiser connection from a token result and selected advertiser id.
    /// </summary>
    TikTokClientAdvertiserConnection CreateClientAdvertiserConnection(
        TikTokOAuthTokenResult token,
        string advertiserId);

    /// <summary>
    /// Creates one customer-owned advertiser connection for each advertiser id returned by TikTok.
    /// </summary>
    IReadOnlyList<TikTokClientAdvertiserConnection> CreateClientAdvertiserConnections(
        TikTokOAuthTokenResult token);

    /// <summary>
    /// Returns the configured managed advertiser connection for workflow-managed campaign execution.
    /// </summary>
    TikTokManagedAdvertiserConnection GetManagedAdvertiserConnection();

    /// <summary>
    /// Executes the complete workflow:
    /// <list type="number">
    ///   <item><description>Upload all <see cref="TikTokCampaignWorkflowRequest.Assets"/> (videos/images/audio).</description></item>
    ///   <item><description>Create all <see cref="TikTokCampaignWorkflowRequest.Audiences"/> from customer-match files.</description></item>
    ///   <item><description>Create the <see cref="TikTokCampaignWorkflowRequest.Campaign"/>.</description></item>
    ///   <item><description>Create the <see cref="TikTokCampaignWorkflowRequest.AdGroup"/> under the campaign (auto-wiring created audience ids).</description></item>
    ///   <item><description>Create all <see cref="TikTokCampaignWorkflowRequest.Ads"/> under the ad group.</description></item>
    /// </list>
    /// Parent ids and uploaded asset ids are propagated automatically between steps.
    /// </summary>
    /// <param name="request">The bundle of resources to create.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A result carrying every created resource with its server-assigned id.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown when any step fails at the API.</exception>
    Task<TikTokCampaignWorkflowResult> ExecuteAsync(
        TikTokCampaignWorkflowRequest request,
        CancellationToken cancellationToken = default);
}
