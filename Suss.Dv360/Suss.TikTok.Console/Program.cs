// =============================================================================
// Suss.TikTok.Console – Smoke-test harness for the TikTok Marketing API client.
//
// Demonstrates the full end-to-end campaign creation workflow:
//   1. Upload assets (video/image/audio)
//   2. Create custom audiences (emails / phone numbers / mobile advertiser IDs)
//   3. Create campaign
//   4. Create ad group (auto-references created audiences)
//   5. Create ads (video, image, and Spark Ads)
//
// Configuration is loaded from appsettings.json (auth mode, tokens, advertiser ID).
// Replace the placeholder values in appsettings.json before running for real.
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Suss.TikTok.Client.Configuration;
using Suss.TikTok.Client.Models;
using Suss.TikTok.Client.Services;
using System.Globalization;

// Build the host with default logging and configuration from appsettings.json.
var builder = Host.CreateApplicationBuilder(args);

// Register all TikTok client services, binding configuration from the "TikTok" section.
// This wires up the auth provider (per AuthMode), the HTTP transport, and all services.
builder.Services.AddTikTokClient(builder.Configuration);

var app = builder.Build();

// Create a DI scope to resolve scoped services (resource services are registered as scoped).
using var scope = app.Services.CreateScope();
var workflow = scope.ServiceProvider.GetRequiredService<ITikTokCampaignWorkflowService>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var promoVideoPath = ResolveAssetPath("promo.mp4", builder.Environment.ContentRootPath);
var bannerImagePath = ResolveAssetPath("banner_300x250.png", builder.Environment.ContentRootPath);
var runSuffix = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
var scheduleStartTime = DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

// Build the workflow request with sample data for smoke testing.
// NOTE: Budgets/bids are in the advertiser account currency (e.g., USD).
var request = new TikTokCampaignWorkflowRequest
{
    // Assets are uploaded first so their server-assigned IDs can be referenced by ads.
    Assets =
    [
        new TikTokAsset
        {
            AssetType = TikTokAssetType.Video,
            FilePath = promoVideoPath,
            DisplayName = $"Promo Video {runSuffix}"
        },
        new TikTokAsset
        {
            AssetType = TikTokAssetType.Image,
            FilePath = bannerImagePath,
            DisplayName = $"Promo Cover {runSuffix}"
        }
    ],

    // Customer-match audiences. Raw identifiers are SHA-256 hashed locally before upload.
    Audiences =
    [
    ],

    Campaign = new TikTokCampaign
    {
        CampaignName = $"Test Campaign {runSuffix}",
        ObjectiveType = "TRAFFIC",
        BudgetMode = "BUDGET_MODE_TOTAL",
        Budget = 50.0,
        OperationStatus = "DISABLE"             // Create paused so the smoke test never spends.
    },

    AdGroup = new TikTokAdGroup
    {
        AdGroupName = $"Test Ad Group {runSuffix}",
        PlacementType = "PLACEMENT_TYPE_AUTOMATIC",
        BillingEvent = "CPC",
        OptimizationGoal = "CLICK",
        PromotionType = "WEBSITE",
        BudgetMode = "BUDGET_MODE_DAY",
        Budget = 50.0,                          // $50.00 daily budget
        BidPrice = 0.50,                        // $0.50 bid
        ScheduleType = "SCHEDULE_FROM_NOW",
        ScheduleStartTime = scheduleStartTime,
        LocationIds = ["6252001"],              // Example: United States
        OperationStatus = "DISABLE"
    },

    Ads =
    [
        // A single-video ad referencing the uploaded video + cover image.
        new TikTokAd
        {
            AdName = $"Video Ad {runSuffix}",
            Format = TikTokAdFormat.SingleVideo,
            DisplayName = "My Brand",
            AdText = "Check out our latest collection!",
            CallToAction = "SHOP_NOW",
            LandingPageUrl = "https://example.com",
            OperationStatus = "DISABLE"
            // VideoId / CoverImageId are wired below from the uploaded assets.
        }
    ]
};

try
{
    logger.LogInformation("Starting TikTok campaign creation workflow...");

    var result = await workflow.ExecuteAsync(request);

    logger.LogInformation("Workflow completed successfully!");
    logger.LogInformation("  Campaign ID: {CampaignId}", result.Campaign.CampaignId);
    logger.LogInformation("  Ad Group ID: {AdGroupId}", result.AdGroup.AdGroupId);

    foreach (var audience in result.Audiences)
        logger.LogInformation("  Audience ID: {AudienceId}", audience.AudienceId);

    foreach (var ad in result.Ads)
        logger.LogInformation("  Ad ID: {AdId}", ad.AdId);
}
catch (Exception ex)
{
    logger.LogError(ex, "Workflow failed");
}

static string ResolveAssetPath(string fileName, string contentRootPath)
{
    var searchRoots = new[]
    {
        contentRootPath,
        AppContext.BaseDirectory,
        Directory.GetCurrentDirectory()
    }
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Select(Path.GetFullPath)
    .Distinct(StringComparer.OrdinalIgnoreCase);

    var checkedPaths = new List<string>();

    foreach (var root in searchRoots)
    {
        for (var directory = new DirectoryInfo(root);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "assets", fileName);
            checkedPaths.Add(candidate);

            if (File.Exists(candidate))
                return candidate;
        }
    }

    throw new FileNotFoundException(
        $"Could not find sample asset '{fileName}'. Checked: {string.Join(", ", checkedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}",
        fileName);
}

