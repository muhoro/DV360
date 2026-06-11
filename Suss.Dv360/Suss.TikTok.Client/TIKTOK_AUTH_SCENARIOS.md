# TikTok Authorization Scenarios

This client supports four TikTok authorization lanes. They are intentionally separated because each
lane answers a different business question: whose advertiser account is billed, whose token is used
for campaign APIs, and whose TikTok identity or organic post is being referenced.

For testing and early integration, campaign execution can either use the existing configured auth
provider path or an explicitly supplied `TikTokExecutionContext`. That context contains the
advertiser id and access token used by TikTok Marketing API calls. Identity connections and Spark
authorizations are separate records that can be attached to creatives but must not replace campaign
API credentials.

## Summary

| Scenario | Customer requirement | Campaign advertiser id | API access token | What we store |
|----------|----------------------|------------------------|------------------|---------------|
| Managed advertiser account | Customer has no TikTok Ads account | Our advertiser id | Our long-lived token | Optional mapping from customer to our managed advertiser |
| Client advertiser account | Customer owns a TikTok Business/Ads account | Customer-selected advertiser id | Customer-granted OAuth token | Token plus one or more advertiser ids |
| Client social identity | Customer has a TikTok social identity | Usually our advertiser id | Our campaign token, plus social authorization data | Customer identity authorization metadata |
| Spark Ad authorization | Customer has an organic TikTok post/identity | Our advertiser id or customer advertiser id | Campaign token for the selected advertiser, plus Spark permission data | Spark post id and authorized identity data |

## Execution Context

`TikTokExecutionContext` is the handoff between authorization/storage and campaign execution:

```csharp
public sealed class TikTokExecutionContext
{
    public Guid OrganizationId { get; set; }
    public string AdvertiserId { get; set; }
    public string AccessToken { get; set; }
    public TikTokExecutionLane ExecutionLane { get; set; }
    public Guid? ConnectionId { get; set; }
    public string? Currency { get; set; }
    public string? Timezone { get; set; }
}
```

The lightweight rules are:

- Managed-account testing can use the existing configured `AuthMode = AccessToken` path.
- Client-advertiser testing should pass a `TikTokExecutionContext` with the customer token and
  selected customer advertiser id.
- The client library does not choose a lane for you. The host app decides whether to use managed or
  client advertiser execution.
- Token encryption, persistence, and refresh are host-application responsibilities for now.

Campaign, ad group, ad, asset, and audience services now expose overloads that accept
`TikTokExecutionContext`. The workflow accepts one context and uses it for every API call in the
campaign creation flow. If no context is supplied, the workflow uses the existing configured
auth-provider path.

## 1. Managed Advertiser Account

Use this when the customer does not have a TikTok Business/Ads account and campaigns should run
through our platform-owned TikTok advertiser account.

Flow:

1. Configure `AuthMode = AccessToken`.
2. Configure `AccessToken` with our long-lived TikTok token.
3. Configure `AdvertiserId` with our platform-owned advertiser id.
4. Create campaigns normally through the workflow/services.

Code path:

```csharp
options.AuthMode = AuthMode.AccessToken;
options.AccessToken = "<our-lifetime-access-token>";
options.AdvertiserId = "<our-advertiser-id>";
```

Notes:

- No customer OAuth is required.
- No refresh-token behavior is implemented for this lane because the product assumption is that
  TikTok provides a long-lived token for our app/ad account.
- `ITikTokOAuthService.GetManagedAdvertiserConnection()` validates that the configured token and
  advertiser id exist before the host app uses this lane.

## 2. Client Advertiser Account

Use this for business-to-business linking when the customer owns a TikTok Business/Ads account and
authorizes our app to manage campaigns in their advertiser account.

Flow:

1. Create a pending OAuth record in our system with customer id, scenario, redirect URI, and random
   `state`.
2. Redirect the customer to `BuildAuthorizationUrl(TikTokAuthScenario.ClientAdvertiserAccount, ...)`.
3. TikTok redirects back with `code` and `state`.
4. Validate `state` against our pending OAuth record.
5. Exchange `code` with `ExchangeAuthorizationCodeAsync`.
6. If multiple advertiser ids are returned, let the customer choose one or link all allowed ids.
7. Store `TikTokClientAdvertiserConnection` records against the customer.
8. If TikTok does not include advertiser ids in the token response, the host app should call the
   relevant advertiser-list endpoint before creating the final connection record.

Code path:

```csharp
var url = oauth.BuildAuthorizationUrl(
    TikTokAuthScenario.ClientAdvertiserAccount,
    redirectUri,
    state,
    scopes: ["ad.read", "ad.write"]);

var token = await oauth.ExchangeAuthorizationCodeAsync(code);
var connections = oauth.CreateClientAdvertiserConnections(token);
```

For a selected advertiser:

```csharp
var connection = oauth.CreateClientAdvertiserConnection(token, selectedAdvertiserId);
```

Notes:

- A single TikTok Business account can expose multiple advertiser accounts.
- The access token is the permission. The `advertiser_id` selects the ad account used by each API
  operation.
- The lightweight client does not perform advertiser-list fallback automatically.

## 3. Client Social Identity

Use this when the customer wants to connect a TikTok social identity, but campaign execution still
typically runs through our managed advertiser account.

Flow:

1. Create a pending OAuth/linking record with scenario `ClientSocialIdentity`.
2. Build and redirect to the TikTok authorization URL.
3. Validate callback `state`.
4. Exchange the returned code.
5. Store social/identity authorization data separately from advertiser-account credentials.

Important distinction:

- This is not the same as advertiser-account access.
- A social identity authorization should not automatically replace the campaign access token or
  advertiser id used for Marketing API campaign calls.

## 4. Spark Ad Authorization

Use this when the customer wants to promote an organic TikTok post as a Spark Ad. This can happen in
two ways:

- The customer has no TikTok Ads account, so campaign execution uses our managed advertiser account.
- The customer has their own TikTok Business/Ads account, so campaign execution uses their linked
  advertiser account while the Spark creative uses their authorized TikTok identity/post.

Flow:

1. Capture or resolve the customer's Spark permission data.
2. Store `TikTokSparkAdAuthorization` against the customer/post.
3. Choose the campaign advertiser lane:
   - Managed advertiser account: use our token and advertiser id.
   - Client advertiser account: use the customer's OAuth token and selected advertiser id.
4. Attach the Spark authorization to the `TikTokAd`.

Code path:

```csharp
var ad = new TikTokAd
{
    AdName = "Boost client post",
    Format = TikTokAdFormat.SparkAd,
    SparkAuthorization = new TikTokSparkAdAuthorization
    {
        TikTokItemId = "<organic-post-id>",
        IdentityId = "<authorized-identity-id-or-code>",
        IdentityType = "AUTH_CODE"
    }
};
```

Validation:

- `TikTokAdService` rejects Spark ads without a post id.
- `TikTokAdService` rejects Spark ads without both `IdentityId` and `IdentityType`.

Client-Owned Advertiser Plus Client-Owned Spark Identity:

This is catered for by combining scenario 2 and scenario 4:

```text
Client advertiser account connection
  -> AccessToken = customer-granted OAuth token
  -> AdvertiserId = selected customer advertiser id

Spark authorization
  -> TikTokItemId = customer's organic post id
  -> IdentityId / IdentityType = customer's authorized Spark identity
```

The campaign/ad group/ad API calls should use the customer advertiser connection, while the Spark
creative should include `TikTokSparkAdAuthorization`. In other words, advertiser ownership and Spark
identity ownership are independent choices that can both belong to the client.

## Error Handling

The client fails early for common integration mistakes:

- Missing app id when building OAuth URLs.
- Missing redirect URI or non-absolute redirect URI.
- Missing state value.
- Attempting to start OAuth for the managed advertiser scenario.
- Missing app id/app secret during token exchange.
- Empty auth code during token exchange.
- Invalid or non-zero TikTok token exchange response.
- Missing managed token or advertiser id for managed-account mode.
- Selecting an advertiser id not present in the OAuth token response when advertiser ids were
  returned.
- Missing Spark post or identity authorization fields.
- Expired or inactive Spark authorization.

## Host Application Responsibilities

The host application still owns:

- Creating and storing pending OAuth state.
- Validating the returned `state` before exchanging the code.
- Mapping each connection to the correct customer/business.
- Persisting tokens securely.
- Encrypting access tokens and refresh tokens at rest.
- Avoiding token values in logs.
- Letting users choose among multiple advertiser accounts when needed.
- Refreshing customer OAuth tokens if TikTok returns expiring token pairs and the product requires
  long-lived customer advertiser connections.
- Calling advertiser-list or identity-list APIs not already exposed by this client.

## Recommended Product Labels

Use labels like these in product or admin screens:

- "Run ads through our managed TikTok account"
- "Connect your TikTok Ads account"
- "Connect your TikTok identity"
- "Authorize a Spark Ad post"

Those labels avoid mixing advertiser access, social identity access, and Spark post authorization.
