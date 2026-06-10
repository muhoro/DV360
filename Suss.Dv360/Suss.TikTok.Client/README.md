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
2. **Campaign creation** – supports **video**, **image**, and **Spark Ads** creatives (at the ad layer).
3. **Asset uploading** – **images**, **videos**, and **audio** via TikTok's multipart upload endpoints,
   with automatic `file_signature` (MD5) computation.
4. **Ad group & ad creation** – full targeting/budget/bidding on the ad group and batched ad creation.
5. **Audience support** – upload **emails**, **phone numbers**, and **mobile advertiser IDs**
   (SHA-256 hashed locally) and reference audiences by ID for targeting.

---

## Architecture

The library is layered so transport, auth, mapping, and orchestration stay independent:

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
- **Pluggable auth.** `ITikTokAuthProvider` abstracts token acquisition. The token is attached via
  TikTok's custom `Access-Token` header (not a standard bearer header), and OAuth token exchange is
  cached and concurrency-safe.
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
| `AdvertiserId`| Yes                               | Default advertiser scope for resource operations.        |
| `BaseUrl`     | No (default production host)      | Override for sandbox.                                    |
| `ApiVersion`  | No (default `v1.3`)               | Marketing API version segment.                           |

---

## Usage

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
