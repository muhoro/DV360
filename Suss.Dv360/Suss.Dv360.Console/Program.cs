// =============================================================================
// Suss.Dv360.Console – Smoke-test harness for the DV360 client library.
//
// Demonstrates the full Phase 1 campaign creation workflow:
//   1. Upload creatives
//   2. Create campaign
//   3. Create insertion order + line items
//   4. Link creatives to line items
//
// Configuration is loaded from appsettings.json (auth mode, key paths, advertiser ID).
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Suss.Dv360.Client.Configuration;
using Suss.Dv360.Client.Models;
using Suss.Dv360.Client.Services;

// Build the host with default logging and configuration from appsettings.json.
var builder = Host.CreateApplicationBuilder(args);

// Register all DV360 client services, binding configuration from the "Dv360" section.
builder.Services.AddDv360Client(options =>
{
    var config = builder.Configuration.GetSection("Dv360");

    options.AuthMode = Enum.Parse<AuthMode>(config["AuthMode"] ?? "ServiceAccount");
    options.ServiceAccountKeyPath = config["ServiceAccountKeyPath"];
    options.OAuthClientId = config["OAuthClientId"];
    options.OAuthClientSecret = config["OAuthClientSecret"];
    options.TokenStorePath = config["TokenStorePath"] ?? "tokens";
});

var app = builder.Build();

// Create a DI scope to resolve scoped services (resource services are registered as scoped).
using var scope = app.Services.CreateScope();
var workflow = scope.ServiceProvider.GetRequiredService<ICampaignWorkflowService>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

// Read the target advertiser ID from configuration.
var advertiserId = long.Parse(builder.Configuration["Dv360:AdvertiserId"] ?? "0");

// Build the workflow request with sample data for smoke testing.
// All monetary values are in micros (1 currency unit = 1,000,000 micros).
var request = new CampaignWorkflowRequest
{
    AdvertiserId = advertiserId,
    Campaign = new Dv360Campaign
    {
        DisplayName = "Test Campaign",
        GoalType = "CAMPAIGN_GOAL_TYPE_BRAND_AWARENESS",
        PerformanceGoalType = "PERFORMANCE_GOAL_TYPE_CPM",
        PerformanceGoalAmountMicros = 1_000_000,              // $1.00 CPM target
        StartDate = DateOnly.FromDateTime(DateTime.Today),
        EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
    },
    Creatives =
    [
        new Dv360Creative
        {
            DisplayName = "Test Creative",
            CreativeType = "CREATIVE_TYPE_STANDARD",
            HostingSource = "HOSTING_SOURCE_HOSTED",
            WidthPixels = 300,
            HeightPixels = 250,
            Assets =
            [
                new Dv360CreativeAsset
                {
                    FilePath = "assets/banner_300x250.png",    // Local path to the image file
                    MimeType = "image/png",
                    Role = "ASSET_ROLE_MAIN"
                }
            ]
        }
    ],
    InsertionOrder = new Dv360InsertionOrder
    {
        DisplayName = "Test Insertion Order",
        BudgetAmountMicros = 10_000_000_000,                  // $10,000.00 total budget
        StartDate = DateOnly.FromDateTime(DateTime.Today),
        EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
        DailyMaxMicros = 500_000_000                          // $500.00 daily cap
    },
    LineItems =
    [
        new Dv360LineItem
        {
            DisplayName = "Test Line Item",
            LineItemType = "LINE_ITEM_TYPE_DISPLAY_DEFAULT",
            MaxBudgetAmountMicros = 5_000_000_000,            // $5,000.00 max line item budget
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
            DailyMaxMicros = 250_000_000                      // $250.00 daily cap
        }
    ]
};

try
{
    // Execute the full workflow and log the DV360-assigned identifiers for verification.
    logger.LogInformation("Starting DV360 campaign creation workflow...");
    var result = await workflow.ExecuteAsync(request);

    logger.LogInformation("Workflow completed successfully!");
    logger.LogInformation("  Campaign ID: {CampaignId}", result.Campaign.CampaignId);
    logger.LogInformation("  Insertion Order ID: {InsertionOrderId}", result.InsertionOrder.InsertionOrderId);

    foreach (var creative in result.Creatives)
        logger.LogInformation("  Creative ID: {CreativeId}", creative.CreativeId);

    foreach (var lineItem in result.LineItems)
        logger.LogInformation("  Line Item ID: {LineItemId}", lineItem.LineItemId);
}
catch (Exception ex)
{
    logger.LogError(ex, "Workflow failed");
}
