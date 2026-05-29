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
            FilePath = "assets/promo.mp4",
            DisplayName = "Promo Video"
        },
        new TikTokAsset
        {
            AssetType = TikTokAssetType.Image,
            FilePath = "assets/cover.jpg",
            DisplayName = "Video Cover"
        }
    ],

    // Customer-match audiences. Raw identifiers are SHA-256 hashed locally before upload.
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

    Campaign = new TikTokCampaign
    {
        CampaignName = "Test Campaign",
        ObjectiveType = "TRAFFIC",
        BudgetMode = "BUDGET_MODE_INFINITE",
        OperationStatus = "DISABLE"             // Create paused so the smoke test never spends.
    },

    AdGroup = new TikTokAdGroup
    {
        AdGroupName = "Test Ad Group",
        PlacementType = "PLACEMENT_TYPE_AUTOMATIC",
        BillingEvent = "CPC",
        OptimizationGoal = "CLICK",
        BudgetMode = "BUDGET_MODE_DAY",
        Budget = 50.0,                          // $50.00 daily budget
        BidPrice = 0.50,                        // $0.50 bid
        ScheduleType = "SCHEDULE_FROM_NOW",
        LocationIds = ["6252001"],              // Example: United States
        OperationStatus = "DISABLE"
    },

    Ads =
    [
        // A single-video ad referencing the uploaded video + cover image.
        new TikTokAd
        {
            AdName = "Video Ad",
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

    // Note: in a real run, after uploading assets you would copy the uploaded IDs onto the ads.
    // For demonstration, the workflow uploads assets first; here we link the first video/image to
    // the video ad before ads are created. (The workflow uploads before creating ads.)
    var videoAsset = request.Assets.FirstOrDefault(a => a.AssetType == TikTokAssetType.Video);
    var coverAsset = request.Assets.FirstOrDefault(a => a.AssetType == TikTokAssetType.Image);

    // The workflow populates asset IDs during execution; wire them via a pre-upload pass so the
    // ad has its references ready. In production, upload assets explicitly, then build ads.
    var assetService = scope.ServiceProvider.GetRequiredService<ITikTokAssetService>();
    var advertiserId = builder.Configuration["TikTok:AdvertiserId"]!;

    if (videoAsset is not null)
    {
        await assetService.UploadAsync(advertiserId, videoAsset);
        request.Ads[0].VideoId = videoAsset.VideoId;
    }
    if (coverAsset is not null)
    {
        await assetService.UploadAsync(advertiserId, coverAsset);
        request.Ads[0].CoverImageId = coverAsset.ImageId;
    }

    // Avoid double-uploading inside the workflow now that we've uploaded above.
    request.Assets.Clear();

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

