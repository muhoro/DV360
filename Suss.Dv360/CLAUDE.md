# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build the solution
dotnet build Suss.Dv360.slnx

# Restore dependencies
dotnet restore

# Run the console app (end-to-end smoke test)
dotnet run --project Suss.Dv360.Console
```

Target framework is `net10.0`. There is no test project; the `Suss.Dv360.Console` project serves as an integration harness.

## Architecture Overview

**Purpose:** A .NET 10 client library wrapping the Google Display & Video 360 API (v3) and Bid Manager API v2 behind strongly-typed interfaces and flat POCOs.

### Two Projects

| Project | Type | Role |
|---|---|---|
| `Suss.Dv360.Client` | Class library | Reusable, NuGet-ready DV360 client |
| `Suss.Dv360.Console` | Executable | Integration demo / smoke-test harness |

### Core Design Principles

- **SDK Isolation** — Google SDK types are `internal`; consumers see only `Dv360*` POCOs
- **Interface-First** — Every service has a public `I*Service` interface; implementations are `internal sealed`
- **DI Native** — Register everything via `services.AddDv360Client(options => { ... })` in `ServiceCollectionExtensions.cs`
- **404 → null** — GET operations return `null` on 404 instead of throwing
- **Flat Models** — `Dv360*` POCOs flatten Google's deeply nested SDK types (e.g., `Campaign.CampaignGoal.PerformanceGoal.PerformanceGoalType` → `Dv360Campaign.PerformanceGoalType`)

### Authentication

Two modes controlled by `Dv360ClientOptions.AuthMode`:
- `ServiceAccount` — server-to-server via a JSON key file (`ServiceAccountAuthProvider`)
- `OAuthUser` — interactive OAuth 2.0 with token caching to disk (`OAuthUserAuthProvider`)

Auth is resolved through `IDv360AuthProvider`, which produces a `GoogleCredential` consumed by both service factories.

### Service Layers

**Phase 1 — Campaign Management (Display & Video 360 API v3)**

The workflow always follows this dependency order:

1. Upload assets → `IAssetService`
2. Create campaign → `ICampaignService` (returns `CampaignId`)
3. Create insertion order → `IInsertionOrderService` (needs `CampaignId`)
4. Create line items → `ILineItemService` (needs `CampaignId` + `InsertionOrderId`)
5. Apply targeting → `ITargetingService`
6. Assign creatives → `ILineItemService.AssignCreativeAsync`

`ICampaignWorkflowService` orchestrates steps 1–6 in the correct order from a single `CampaignWorkflowRequest`.

**Phase 2 — Reporting (Bid Manager API v2)**

- `IReportingService` — creates a Bid Manager query, polls until complete, downloads the CSV from GCS, and returns typed `CampaignReportRow` objects
- `IReportPollingService` — handles async polling with configurable timeout/interval (`ReportPollingOptions`)
- `CsvReportParser` — parses the raw CSV response into strongly-typed rows

### Infrastructure

| Class | Responsibility |
|---|---|
| `DisplayVideoServiceFactory` | Thread-safe singleton (SemaphoreSlim double-check) for DV360 service init |
| `BidManagerServiceFactory` | Same pattern for Bid Manager service |
| `GoogleTypeMapper` | `DateOnly` ↔ `Google.Apis.Utils.Date` conversions |
| `CsvReportParser` | Parses report CSV lines into `CampaignReportRow` |

### Service Lifetimes

- **Singleton:** `IDv360AuthProvider`, `IDisplayVideoServiceFactory`, `IBidManagerServiceFactory`, `IReportParser`
- **Scoped:** all `I*Service` implementations

### Mapping Pattern

Each service contains private static `MapToGoogle()` / `MapFromGoogle()` methods. Do not expose Google SDK types across service boundaries.

### Configuration (`appsettings.json`)

```json
{
  "Dv360": {
    "AuthMode": "OAuthUser",
    "ServiceAccountKeyPath": "key.json",
    "OAuthClientId": "...",
    "OAuthClientSecret": "...",
    "TokenStorePath": "tokens",
    "AdvertiserId": "..."
  }
}
```

## Key Known Gaps

- **No rollback** — `CampaignWorkflowService` does not roll back partial resource creation on failure
- **No retry policy** — rate limiting is delegated entirely to the Google SDK
- **No test project** — services are interface-based and mockable, but no tests exist yet
