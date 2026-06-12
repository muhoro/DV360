# Suss.TikTok.Client

A clean, dependency-injection–friendly .NET client for the **TikTok Marketing API**. It mirrors the
engineering principles of the companion `Suss.Dv360.Client` library (layered abstractions, flat
consumer-facing models, private API mapping, and a single end-to-end workflow orchestrator) while
adapting to TikTok's very different authentication and resource model.

> Target framework: **.NET 10**

---

## Features

The client supports the full campaign-publishing lifecycle:

1. **Authentication** – multiple strategies behind one abstraction:
   - **Access Token** – use a pre-issued, long-lived `access_token`.
   - **OAuth Authorization Code** – exchange an `auth_code` (+ `app_id`/`secret`) for an `access_token`.
   - **OAuth linking helper** – build the TikTok consent URL and exchange callback codes via `ITikTokOAuthService`.
2. **Campaign creation** – supports **video**, **image**, and **Spark Ads** creatives (at the ad layer).
3. **Asset uploading** – **images**, **videos**, and **audio** via TikTok's multipart upload endpoints,
   with automatic `file_signature` (MD5) computation.
4. **Ad group & ad creation** – full targeting/budget/bidding on the ad group and batched ad creation.
5. **Audience support** – upload **emails**, **phone numbers**, and **mobile advertiser IDs**
   (SHA-256 hashed locally) and reference audiences by ID for targeting.

---

## Architecture

The library follows the same organization style as the DV360 client: public service interfaces,
internal sealed implementations, flat models, and a small auth/infrastructure layer for direct
platform calls. Workflow-level auth mode decisions stay in `ITikTokCampaignWorkflowService`; direct
TikTok HTTP mechanics stay in auth/infrastructure/resource services.

```
Configuration (options + DI)
        │
        ▼
Auth layer  ── ITikTokAuthProvider
   ├─ StaticTokenAuthProvider        (AccessToken mode)
   └─ OAuthAuthCodeAuthProvider      (OAuth authorization-code exchange)
        │
        ▼
Infrastructure ── ITikTokApiClient (TikTokApiClient)
   • Builds /open_api/{version}/ URLs
   • Attaches the "Access-Token" header
   • Serializes JSON & unwraps the standard envelope
   • Throws TikTokApiException on non-zero "code"
        │
        ▼
Services (flat model ⇄ snake_case API mapping)
   ├─ ITikTokAssetService       (file/{image|video|audio}/ad/upload/)
   ├─ ITikTokAudienceService    (dmp/custom_audience/*)
   ├─ ITikTokCampaignService    (campaign/create/)
   ├─ ITikTokAdGroupService     (adgroup/create/)
   └─ ITikTokAdService          (ad/create/)
        │
        ▼
Workflow  ── ITikTokCampaignWorkflowService
   Orchestrates: assets → audiences → campaign → ad group → ads
```

### Key engineering decisions

- **Envelope-aware transport.** TikTok returns HTTP 200 even for business errors; success is signaled
  by `code == 0` in the JSON envelope. `TikTokApiClient` centralizes this contract so services only
  ever see the unwrapped `data` payload, and `TikTokApiException` carries `code`, `request_id`, and the
  raw body for diagnostics.
- **Flat models, private mapping.** Public models (`TikTokCampaign`, `TikTokAdGroup`, `TikTokAd`, …) are
  flat and friendly. Each service maps them to/from TikTok's `snake_case` request/response shapes using
  private nested DTOs, keeping the API surface stable even if TikTok's payloads change.
- **Stateless TikTok calls.** Auth/infrastructure services only build or send direct TikTok requests:
  token resolution, OAuth token exchange, API URL building, headers, serialization, and envelope
  handling. Connection shaping and auth-mode coordination sit in the workflow layer.
- **Privacy-preserving audiences.** Customer-match identifiers are normalized and SHA-256 hashed
  **locally** before upload; raw PII never leaves the process unless `ValuesArePreHashed` is set.
- **One-call workflow.** `ITikTokCampaignWorkflowService` wires parent IDs and uploaded asset IDs
  between steps so a complete, linked campaign is created with a single call.

---

## Installation

Reference the project (or its package) and register it in DI:

```csharp
using Suss.TikTok.Client.Configuration;

// From IConfiguration (binds the "TikTok" section):
builder.Services.AddTikTokClient(builder.Configuration);

// Or configure inline:
builder.Services.AddTikTokClient(options =>
{
    options.AuthMode = AuthMode.AccessToken;
    options.AccessToken = "<token>";
    options.AdvertiserId = "<advertiser-id>";
});
```

### Configuration (`appsettings.json`)

```json
{
  "TikTok": {
    "AuthMode": "AccessToken",
    "AccessToken": "<your-long-lived-access-token>",
    "AppId": "<your-app-id>",
    "AppSecret": "<your-app-secret>",
    "AuthCode": "<one-time-oauth-auth-code>",
    "AuthorizationUrl": "https://business-api.tiktok.com/portal/auth",
    "Scopes": [ "ad.read", "ad.write" ],
    "ManagedIdentityId": "<your-platform-owned-identity-id>",
    "ManagedIdentityType": "<your-platform-owned-identity-type>",
    "AdvertiserId": "<your-advertiser-id>",
    "BaseUrl": "https://business-api.tiktok.com",
    "ApiVersion": "v1.3"
  }
}
```

| Option        | Required                          | Description                                              |
|---------------|-----------------------------------|----------------------------------------------------------|
| `AuthMode`    | Yes                               | `AccessToken` or `OAuthAuthorizationCode`.               |
| `AccessToken` | When `AuthMode = AccessToken`     | Pre-issued long-lived token.                             |
| `AppId`       | When `AuthMode = OAuth…`          | TikTok developer app id.                                 |
| `AppSecret`   | When `AuthMode = OAuth…`          | TikTok developer app secret.                             |
| `AuthCode`    | When `AuthMode = OAuth…`          | One-time authorization code from the consent redirect.   |
| `AuthorizationUrl` | No                           | Override for TikTok's OAuth consent URL.                 |
| `Scopes`      | No                                | Default scopes requested when building consent URLs.     |
| `ManagedIdentityId` | No                          | Platform-owned identity used for managed-account ads.    |
| `ManagedIdentityType` | No                        | Identity type for `ManagedIdentityId`.                   |
| `AdvertiserId`| Yes                               | Default advertiser scope for resource operations.        |
| `BaseUrl`     | No (default production host)      | Override for sandbox.                                    |
| `ApiVersion`  | No (default `v1.3`)               | Marketing API version segment.                           |

---

## Usage

### OAuth linking flow

There are four supported authorization lanes:

| Scenario | Campaign advertiser | Token used for API calls | What the customer contributes |
|----------|---------------------|--------------------------|-------------------------------|
| Managed advertiser account | Your advertiser id | Your long-lived access token | Nothing required |
| Client advertiser account | Customer advertiser id | Customer-granted OAuth access token | TikTok Business/Ads consent |
| Client social identity | Usually your advertiser id | Your campaign token, plus stored social authorization data | TikTok social/identity consent |
| Spark Ad authorization | Your advertiser id or customer advertiser id | Campaign token for selected advertiser, plus stored Spark authorization data | Organic post/identity permission |

For the managed advertiser account auth mode, configure `AuthMode = AccessToken`, `AccessToken`, and
`AdvertiserId` with your platform-owned TikTok values. `StaticTokenAuthProvider` uses that token as-is;
there is no refresh-token flow in this lane.

Your web app owns the OAuth routes and storage. The client helps with the TikTok-specific URL and
token exchange. Store the auth mode in your own state/session/database before redirecting; the
generated OAuth URL only sends TikTok-recognized OAuth values plus any explicit extra parameters you
pass.

```csharp
// GET /tiktok/connect
var workflow = serviceProvider.GetRequiredService<ITikTokCampaignWorkflowService>();
var state = "<random-csrf-correlation-value>"; // store this in session/db before redirecting

var authorizationUrl = workflow.BuildAuthorizationUrl(
    TikTokAuthMode.ClientAdvertiserAccount,
    redirectUri: "https://yourapp.example.com/tiktok/callback",
    state: state,
    scopes: ["ad.read", "ad.write"]);

return Results.Redirect(authorizationUrl);
```

```csharp
// GET /tiktok/callback?code=...&state=...
// First validate the returned state equals the value you stored for this user/session.
var token = await workflow.ExchangeAuthorizationCodeAsync(code);

// For business-to-business linking, let the customer choose one advertiser id from token.AdvertiserIds
// or from a later advertiser-list API call, then store this connection against the customer account.
var connection = workflow.CreateClientAdvertiserConnection(token, selectedAdvertiserId);
```

Use `connection.AccessToken` and `connection.AdvertiserId` when campaigns should run inside the
customer's own TikTok Business/Ads account.

For Spark Ads, campaign calls can run through either your managed advertiser account or the
customer's linked advertiser account. Attach the client's Spark authorization to the ad in both
cases:

```csharp
var ad = new TikTokAd
{
    AdName = "Boost client post",
    Format = TikTokAdFormat.SparkAd,
    AdText = "Watch now",
    SparkAuthorization = new TikTokSparkAdAuthorization
    {
        TikTokItemId = "<client-organic-post-id>",
        IdentityId = "<authorized-spark-identity-id-or-code>",
        IdentityType = "AUTH_CODE"
    }
};
```

### End-to-end workflow

```csharp
var workflow = serviceProvider.GetRequiredService<ITikTokCampaignWorkflowService>();

var result = await workflow.ExecuteAsync(new TikTokCampaignWorkflowRequest
{
    Assets =
    [
        new TikTokAsset { AssetType = TikTokAssetType.Video, FilePath = "promo.mp4" },
        new TikTokAsset { AssetType = TikTokAssetType.Image, FilePath = "cover.jpg" }
    ],
    Audiences =
    [
        new TikTokAudience
        {
            AudienceName = "VIP Customers",
            File = new TikTokAudienceFile
            {
                IdType = TikTokAudienceIdType.Email,
                Values = ["alice@example.com", "bob@example.com"]
            }
        }
    ],
    Campaign = new TikTokCampaign { CampaignName = "Launch", ObjectiveType = "TRAFFIC" },
    AdGroup  = new TikTokAdGroup  { AdGroupName = "US - Broad", BillingEvent = "CPC", OptimizationGoal = "CLICK" },
    Ads =
    [
        new TikTokAd { AdName = "Hero Video", Format = TikTokAdFormat.SingleVideo, AdText = "Shop now!" }
    ]
});

Console.WriteLine($"Campaign {result.Campaign.CampaignId}, Ad Group {result.AdGroup.AdGroupId}");
```

### Individual services

```csharp
// Upload an asset
var asset = await assetService.UploadAsync(advertiserId,
    new TikTokAsset { AssetType = TikTokAssetType.Video, FilePath = "promo.mp4" });

// Create a Spark Ad that boosts an organic post
await adService.CreateAsync(advertiserId, adGroupId,
    [ new TikTokAd { AdName = "Boosted Post", Format = TikTokAdFormat.SparkAd, TikTokItemId = "73904..." } ]);

// Reference an existing audience by listing
var audiences = await audienceService.ListAsync(advertiserId);
```

---

## Ad formats

| Format                    | Required references                         |
|---------------------------|---------------------------------------------|
| `TikTokAdFormat.SingleVideo` | `VideoId` (+ `CoverImageId` recommended) |
| `TikTokAdFormat.SingleImage` | one or more `ImageIds`                   |
| `TikTokAdFormat.SparkAd`     | `TikTokItemId` of the organic post       |

The `TikTokAdService` validates these requirements before sending and throws an
`InvalidOperationException` with a clear message if an asset reference is missing.

---

## Error handling

All services translate failures into `TikTokApiException`, which exposes:

- `Code` – the TikTok business error code (`0` = success).
- `RequestId` – the `request_id` to quote in support tickets.
- `ResponseBody` – the raw JSON body for debugging.

```csharp
try
{
    await workflow.ExecuteAsync(request);
}
catch (TikTokApiException ex)
{
    logger.LogError("TikTok error {Code} (request {RequestId}): {Message}",
        ex.Code, ex.RequestId, ex.Message);
}
```

---

## Notes & limitations

- Endpoint paths/fields follow TikTok Marketing API **v1.3** conventions; adjust `ApiVersion` and any
  enum string values (objectives, optimization goals, CTAs) to match your account's allowed values.
- Spark Ads require an authorized identity/`auth_code` from the post's creator; supply `IdentityId` /
  `IdentityType` as needed.
- Monetary values are expressed in the advertiser account currency (not micros, unlike DV360).
