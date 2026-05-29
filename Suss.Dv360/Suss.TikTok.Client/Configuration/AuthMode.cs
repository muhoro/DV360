namespace Suss.TikTok.Client.Configuration;

/// <summary>
/// Specifies the authentication strategy used to obtain a TikTok Marketing API access token.
/// Configured via <see cref="TikTokClientOptions.AuthMode"/>.
/// <para>
/// Unlike Google's DV360 (which uses service-account or interactive OAuth credentials handled by
/// an SDK), TikTok's Marketing API is a raw REST API secured by a long-lived <c>access_token</c>
/// passed in the <c>Access-Token</c> request header. This enum lets the client either reuse a
/// pre-issued token or perform the OAuth 2.0 authorization-code exchange to mint one.
/// </para>
/// </summary>
public enum AuthMode
{
    /// <summary>
    /// Use a pre-existing, long-lived <c>access_token</c> supplied directly via
    /// <see cref="TikTokClientOptions.AccessToken"/>.
    /// <para>Best suited for unattended server-to-server workloads where a token has already
    /// been minted and stored securely.</para>
    /// </summary>
    AccessToken,

    /// <summary>
    /// Exchange an OAuth 2.0 <c>auth_code</c> (obtained after a user authorizes the app in the
    /// TikTok consent screen) for an <c>access_token</c> using the app's <c>app_id</c> and
    /// <c>secret</c>.
    /// <para>Best suited for tools that onboard new advertiser accounts interactively.</para>
    /// </summary>
    OAuthAuthorizationCode
}
