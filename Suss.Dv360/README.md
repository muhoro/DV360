# Suss.Dv360 — .NET 10 Client Library for Google Display & Video 360

A clean, idiomatic .NET 10 client library that wraps the [Google Display & Video 360 API (v3)](https://developers.google.com/display-video/api/reference/rest) behind strongly-typed interfaces, flattened models, and a dependency-injection-first design. Consumers never interact with Google SDK types directly.

---

## Table of Contents

1. [Overview](#overview)
2. [Solution Structure](#solution-structure)
3. [Architecture](#architecture)
4. [Authentication](#authentication)
5. [Phase 1 Campaign Workflow](#phase-1-campaign-workflow)
6. [Asset Upload Pipeline](#asset-upload-pipeline)
7. [Getting Started](#getting-started)
8. [Configuration Reference](#configuration-reference)
9. [Dependency Injection Registration](#dependency-injection-registration)
10. [Engineering Decisions](#engineering-decisions)
11. [Known Limitations](#known-limitations)
12. [NuGet Dependencies](#nuget-dependencies)
13. [License](#license)

---

## Overview

**Suss.Dv360** provides a reusable .NET library for programmatically managing DV360 campaigns end-to-end. The library:

- Abstracts the Google Display & Video 360 SDK (`Google.Apis.DisplayVideo.v3`) behind clean `I*Service` interfaces.
- Exposes flat POCO models (`Dv360Campaign`, `Dv360Creative`, etc.) instead of nested Google SDK types.
- Supports dual authentication modes: **Service Account** (server-to-server) and **OAuth 2.0 User** (interactive).
- Orchestrates a complete Phase 1 campaign creation workflow in a single call.
- Automatically uploads creative assets via resumable media upload before creative creation.

---

## Solution Structure

```
Suss.Dv360/
├── Suss.Dv360.slnx                          # Solution file
├── README.md
├── .gitignore
│
├── Suss.Dv360.Client/                        # Class library (reusable NuGet-ready package)
│   ├── Auth/
│   │   ├── IDv360AuthProvider.cs             # Auth abstraction (returns ICredential)
│   │   ├── ServiceAccountAuthProvider.cs     # Google service-account JSON key auth
│   │   └── OAuthUserAuthProvider.cs          # OAuth 2.0 interactive user auth
│   │
│   ├── Configuration/
│   │   ├── AuthMode.cs                       # Enum: ServiceAccount | OAuthUser
│   │   ├── Dv360ClientOptions.cs             # Options POCO (key paths, client IDs, etc.)
│   │   └── ServiceCollectionExtensions.cs    # AddDv360Client() DI entry point
│   │
│   ├── Exceptions/
│   │   └── Dv360ApiException.cs              # Typed exception wrapping GoogleApiException
│   │
│   ├── Infrastructure/
│   │   ├── IDisplayVideoServiceFactory.cs    # Internal factory interface
│   │   ├── DisplayVideoServiceFactory.cs     # Thread-safe cached factory (SemaphoreSlim)
│   │   └── GoogleTypeMapper.cs               # DateOnly ↔ Google.Apis Date mapping
│   │
│   ├── Models/
│   │   ├── Dv360Campaign.cs                  # Flat campaign model
│   │   ├── Dv360Creative.cs                  # Flat creative model (with Assets list)
│   │   ├── Dv360CreativeAsset.cs             # Asset model (FilePath, MimeType, Role, MediaId)
│   │   ├── Dv360InsertionOrder.cs            # Flat insertion order model
│   │   ├── Dv360LineItem.cs                  # Flat line item model
│   │   ├── CampaignWorkflowRequest.cs        # Workflow input (all resources in one object)
│   │   └── CampaignWorkflowResult.cs         # Workflow output (with server-assigned IDs)
│   │
│   └── Services/
│       ├── IAssetService.cs                  # Upload media assets
│       ├── AssetService.cs                   # Resumable upload via Google SDK
│       ├── ICampaignService.cs               # Campaign CRUD
│       ├── CampaignService.cs
│       ├── ICreativeService.cs               # Creative CRUD (with integrated asset upload)
│       ├── CreativeService.cs
│       ├── IInsertionOrderService.cs         # IO CRUD
│       ├── InsertionOrderService.cs
│       ├── ILineItemService.cs               # Line item CRUD + creative assignment
│       ├── LineItemService.cs
│       ├── ICampaignWorkflowService.cs       # Orchestrated workflow
│       └── CampaignWorkflowService.cs
│
└── Suss.Dv360.Console/                       # Smoke-test console app
    ├── Program.cs                            # End-to-end workflow demonstration
    └── appsettings.json                      # Auth & advertiser configuration
```

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                         Consumer Code                            │
│       (ASP.NET Core controller, console app, Azure Function)     │
└──────────────┬───────────────────────────────────────────────────┘
               │  Inject: ICampaignWorkflowService
               │          ICampaignService, ICreativeService, ...
               ▼
┌──────────────────────────────────────────────────────────────────┐
│                     Public Service Interfaces                    │
│  ICampaignWorkflowService │ ICampaignService │ ICreativeService  │
│  IInsertionOrderService   │ ILineItemService │ IAssetService     │
└──────────────┬───────────────────────────────────────────────────┘
               │  internal sealed implementations
               ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Internal Service Layer                         │
│  CampaignWorkflowService → orchestrates Steps 1–4               │
│  CampaignService, CreativeService, InsertionOrderService, ...    │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  Model Mapping: Dv360* POCOs  ←→  Google.Apis.*.Data.*    │  │
│  │  (MapToGoogle / MapFromGoogle — private static methods)    │  │
│  └────────────────────────────────────────────────────────────┘  │
└──────────────┬───────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                          │
│  IDisplayVideoServiceFactory → DisplayVideoServiceFactory        │
│  (Thread-safe singleton, SemaphoreSlim double-checked locking)   │
│  GoogleTypeMapper (DateOnly ↔ Google Date)                       │
└──────────────┬───────────────────────────────────────────────────┘
               │
               ▼
┌──────────────────────────────────────────────────────────────────┐
│                     Authentication Layer                          │
│  IDv360AuthProvider (returns ICredential)                         │
│  ├── ServiceAccountAuthProvider (JSON key file)                  │
│  └── OAuthUserAuthProvider (interactive browser flow + token)    │
└──────────────┬───────────────────────────────────────────────────┘
               │
               ▼
       Google.Apis.DisplayVideo.v3  (SDK)
               │
               ▼
       DV360 REST API (v3)
```

### Key Principles

| Principle | Implementation |
|---|---|
| **SDK isolation** | Google SDK types are `internal`; consumers see only `Dv360*` POCOs |
| **Interface-first** | Every service has a public `I*Service` interface; implementations are `internal sealed` |
| **DI-native** | Single `AddDv360Client()` extension method registers everything |
| **Async-throughout** | All operations return `Task<T>` and accept `CancellationToken` |
| **Structured logging** | `ILogger<T>` on every service with semantic log properties |
| **Exception hygiene** | `GoogleApiException` → `Dv360ApiException`; 404 → `null` pattern on GET operations |

---

## Authentication

The library supports two authentication strategies, selected at configuration time:

### Service Account (server-to-server)

Best for backend services, CI/CD pipelines, and automated systems.

```json
{
  "Dv360": {
    "AuthMode": "ServiceAccount",
    "ServiceAccountKeyPath": "path/to/service-account-key.json"
  }
}
```

The service account must be granted the **DV360 API** scopes and appropriate advertiser-level permissions in the DV360 partner settings.

### OAuth 2.0 User (interactive)

Best for developer tools, admin dashboards, or scenarios requiring user-delegated access.

```json
{
  "Dv360": {
    "AuthMode": "OAuthUser",
    "OAuthClientId": "your-client-id.apps.googleusercontent.com",
    "OAuthClientSecret": "your-client-secret",
    "TokenStorePath": "tokens"
  }
}
```

On first run, a browser window opens for consent. Refresh tokens are cached in `TokenStorePath` to avoid re-prompting.

### Auth Provider Resolution

The `AddDv360Client()` method inspects `Dv360ClientOptions.AuthMode` at registration time and wires the correct `IDv360AuthProvider` implementation as a singleton. The `DisplayVideoServiceFactory` then calls the provider once, caches the authenticated `DisplayVideoService`, and reuses it for all subsequent API calls.

---

## Phase 1 Campaign Workflow

The `ICampaignWorkflowService.ExecuteAsync()` method orchestrates the complete DV360 campaign setup in dependency order:

```
Step 1 ─── Upload Creatives (with asset files)    ──► CreativeIds assigned
               │
Step 2 ─── Create Campaign                        ──► CampaignId assigned
               │
Step 3a ── Create Insertion Order (wires CampaignId) ──► InsertionOrderId assigned
               │
Step 3b ── Create Line Items (wires CampaignId + InsertionOrderId)
               │
Step 4 ─── Link Creatives to Line Items (cross-join)
```

### Why this order?

- **Creatives** are independent of campaigns — they belong to the advertiser level and can be uploaded first.
- **Insertion orders** require a `CampaignId` (created in Step 2).
- **Line items** require both `CampaignId` and `InsertionOrderId`.
- **Creative assignment** requires both `CreativeId` and `LineItemId`.

### Workflow Input / Output

```csharp
var request = new CampaignWorkflowRequest
{
    AdvertiserId = 1234567890,
    Campaign = new Dv360Campaign { DisplayName = "Q3 Brand Campaign", ... },
    Creatives = [ new Dv360Creative { DisplayName = "Banner 300x250", ... } ],
    InsertionOrder = new Dv360InsertionOrder { DisplayName = "Q3 IO", ... },
    LineItems = [ new Dv360LineItem { DisplayName = "Display Line Item", ... } ]
};

CampaignWorkflowResult result = await workflow.ExecuteAsync(request);
// result.Campaign.CampaignId  → server-assigned ID
// result.Creatives[0].CreativeId → server-assigned ID
// result.InsertionOrder.InsertionOrderId → server-assigned ID
// result.LineItems[0].LineItemId → server-assigned ID
```

---

## Asset Upload Pipeline

For hosted creatives (`HOSTING_SOURCE_HOSTED`), the library handles the full upload-then-create flow transparently:

```
Dv360CreativeAsset          AssetService               CreativeService
    (FilePath,        ──►  UploadAsync()          ──►  CreateAsync()
     MimeType,              │ Opens FileStream           │ Maps MediaIds into
     Role)                  │ Resumable upload            │   AssetAssociation[]
                            │ Returns MediaId             │ Creates creative
                            ▼                             ▼
                       asset.MediaId = "..."       Creative with wired assets
```

### Usage

```csharp
var creative = new Dv360Creative
{
    DisplayName = "Hero Banner",
    CreativeType = "CREATIVE_TYPE_STANDARD",
    HostingSource = "HOSTING_SOURCE_HOSTED",
    WidthPixels = 728,
    HeightPixels = 90,
    Assets =
    [
        new Dv360CreativeAsset
        {
            FilePath = "assets/hero_728x90.png",
            MimeType = "image/png",
            Role = "ASSET_ROLE_MAIN"
        }
    ]
};
```

When `CreativeService.CreateAsync()` is called (either directly or via the workflow), it:

1. Iterates over `creative.Assets` and calls `IAssetService.UploadAsync()` for each asset with a non-empty `FilePath`.
2. The upload uses Google's **resumable media upload** protocol, populating `asset.MediaId` on success.
3. Maps the uploaded assets into `Google.Apis.DisplayVideo.v3.Data.AssetAssociation` entries.
4. Creates the creative with the wired asset associations.

### Third-Party Tag Creatives

For third-party tag creatives, no asset upload is needed:

```csharp
var creative = new Dv360Creative
{
    DisplayName = "Third-Party Tag",
    CreativeType = "CREATIVE_TYPE_STANDARD",
    HostingSource = "HOSTING_SOURCE_THIRD_PARTY",
    ThirdPartyTag = "<script src='https://ads.example.com/tag.js'></script>",
    WidthPixels = 300,
    HeightPixels = 250
};
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Google Cloud project with the **Display & Video 360 API** enabled
- Either a service account JSON key or OAuth 2.0 client credentials
- DV360 advertiser access for the authenticated identity

### 1. Clone the repository

```bash
git clone https://github.com/muhoro/DV360.git
cd DV360
```

### 2. Configure authentication

Edit `Suss.Dv360.Console/appsettings.json`:

```json
{
  "Dv360": {
    "AuthMode": "ServiceAccount",
    "ServiceAccountKeyPath": "path/to/your-key.json",
    "AdvertiserId": "YOUR_ADVERTISER_ID"
  }
}
```

### 3. Run the smoke test

```bash
dotnet run --project Suss.Dv360.Console
```

The console app executes the full Phase 1 workflow and logs the server-assigned IDs for every created resource.

### 4. Integrate into your own project

Add a project reference:

```xml
<ProjectReference Include="..\Suss.Dv360.Client\Suss.Dv360.Client.csproj" />
```

Register services in your host builder:

```csharp
builder.Services.AddDv360Client(options =>
{
    options.AuthMode = AuthMode.ServiceAccount;
    options.ServiceAccountKeyPath = "key.json";
});
```

Inject and use:

```csharp
public class MyController(ICampaignWorkflowService workflow)
{
    public async Task<IActionResult> CreateCampaign(CampaignWorkflowRequest request)
    {
        var result = await workflow.ExecuteAsync(request);
        return Ok(result);
    }
}
```

---

## Configuration Reference

All options live under the `Dv360ClientOptions` class:

| Property | Type | Default | Description |
|---|---|---|---|
| `AuthMode` | `AuthMode` | `ServiceAccount` | Authentication strategy (`ServiceAccount` or `OAuthUser`) |
| `ServiceAccountKeyPath` | `string?` | `null` | Path to the Google service account JSON key file |
| `OAuthClientId` | `string?` | `null` | OAuth 2.0 client ID from Google Cloud Console |
| `OAuthClientSecret` | `string?` | `null` | OAuth 2.0 client secret |
| `TokenStorePath` | `string` | `"tokens"` | Directory for caching OAuth refresh tokens |
| `ApplicationName` | `string` | `"Suss.Dv360.Client"` | `User-Agent` sent with every API request |

### appsettings.json Example

```json
{
  "Dv360": {
    "AuthMode": "ServiceAccount",
    "ServiceAccountKeyPath": "path/to/service-account-key.json",
    "OAuthClientId": "",
    "OAuthClientSecret": "",
    "TokenStorePath": "tokens",
    "AdvertiserId": "0"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Suss.Dv360": "Debug"
    }
  }
}
```

---

## Dependency Injection Registration

`AddDv360Client()` registers all services in a single call:

| Registration | Lifetime | Rationale |
|---|---|---|
| `IDv360AuthProvider` | **Singleton** | Credentials are loaded once and reused |
| `IDisplayVideoServiceFactory` | **Singleton** | Caches the authenticated `DisplayVideoService` with thread-safe lazy initialization |
| `IAssetService` | **Scoped** | Aligns with per-request lifetime in ASP.NET Core |
| `ICampaignService` | **Scoped** | Same |
| `ICreativeService` | **Scoped** | Same |
| `IInsertionOrderService` | **Scoped** | Same |
| `ILineItemService` | **Scoped** | Same |
| `ICampaignWorkflowService` | **Scoped** | Orchestrates scoped services |

All implementations are `internal sealed` — consumers interact exclusively through the public interfaces.

---

## Engineering Decisions

### 1. Google SDK Behind Interfaces (not raw REST)

The library uses `Google.Apis.DisplayVideo.v3` internally rather than hand-crafting HTTP requests. This gives us:
- Automatic OAuth token refresh and retry handling
- Strongly-typed request/response serialization
- Resumable media upload for large asset files

However, the SDK types are **never exposed** to consumers. Each service maps between flat `Dv360*` POCOs and the nested Google SDK structures in private `MapToGoogle` / `MapFromGoogle` methods. This means:
- Consumer code has **zero dependency** on the Google SDK
- The SDK version can be upgraded without breaking the public API
- Models stay flat and serialization-friendly

### 2. Thread-Safe Singleton Factory

`DisplayVideoServiceFactory` uses a `SemaphoreSlim`-based double-checked locking pattern to ensure the `DisplayVideoService` is created exactly once, even under concurrent access. This avoids redundant authentication round-trips in multi-threaded scenarios (e.g., ASP.NET Core request pipeline).

### 3. Scoped Service Lifetime

Resource services (`CampaignService`, `CreativeService`, etc.) are registered as **scoped** rather than singleton. This:
- Aligns with ASP.NET Core's per-request scope model
- Enables future per-request state (e.g., correlation IDs)
- Keeps the singleton factory as the single shared resource

### 4. Two-Step Creative Creation

`CreativeService.CreateAsync()` performs a transparent two-step process:
1. **Upload** — iterates over the creative's `Assets` list and calls `IAssetService.UploadAsync()` for each file
2. **Create** — maps the populated `MediaId` values into `AssetAssociation` entries and creates the creative

This ensures consumers don't need to manually manage the upload-then-create lifecycle.

### 5. Flat Model Design

Google's API types use deep nesting (e.g., `Creative.Dimensions.WidthPixels`). The library's models flatten these into direct properties (`Dv360Creative.WidthPixels`), making them easier to construct and serialize.

### 6. Exception Wrapping

All `GoogleApiException` errors are caught and re-thrown as `Dv360ApiException`, maintaining a clean exception hierarchy for consumers. GET operations follow a **404 → null** convention instead of throwing, making "check if exists" patterns natural:

```csharp
var creative = await creativeService.GetAsync(advertiserId, creativeId);
if (creative is null)
{
    // Creative does not exist — handle gracefully
}
```

### 7. Pagination Handling

List operations (`ListAsync`) internally handle DV360's page-token-based pagination, iterating through all pages and returning a complete `IReadOnlyList<T>`. Consumers never deal with page tokens.

### 8. DateOnly Mapping

The library uses .NET's `DateOnly` for date fields (campaign start/end, IO flight dates) and converts to/from Google's `Date` type via `GoogleTypeMapper`. This provides a cleaner API than exposing the Google type directly.

### 9. C# 14.0 Language Features

The codebase leverages modern C# features for concise, readable code:
- **Primary constructors** on services (no boilerplate field assignments)
- **Collection expressions** (`[ ]` syntax) for inline list creation
- **Property patterns** (`is { Count: > 0 }`) for null-safe collection checks

---

## Known Limitations

| Area | Status | Detail |
|---|---|---|
| **Creative ↔ Line Item assignment** | ⚠️ Placeholder | `LineItemService.AssignCreativeAsync` currently patches `entityStatus` instead of performing a real creative assignment. The DV360 API's actual mechanism (`bulkEditAssignedTargetingOptions` or creative assignment resources) needs to be wired in. |
| **Error recovery** | Not implemented | The workflow service does not roll back previously created resources if a later step fails. |
| **Rate limiting** | Delegated to SDK | Retry/back-off for API quota limits is handled by the Google SDK's built-in retry mechanism; no library-level retry policy is added. |
| **Bulk operations** | Sequential | Creatives and line items are created sequentially. Parallel creation could improve throughput for large batches. |
| **Unit tests** | Not included | The project does not yet include a test suite. Services are interface-based and readily mockable. |

---

## NuGet Dependencies

### Suss.Dv360.Client

| Package | Version | Purpose |
|---|---|---|
| `Google.Apis.DisplayVideo.v3` | 1.73.0.4107 | DV360 API SDK (internal only) |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.5 | DI registration |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.5 | `ILogger<T>` |
| `Microsoft.Extensions.Options` | 10.0.5 | Options pattern |

### Suss.Dv360.Console

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Extensions.Hosting` | 10.0.5 | Generic host builder |

---

## License

This project is provided as-is for demonstration and internal use. See the repository for any applicable license terms.
