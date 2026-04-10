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

    options.AuthMode = Enum.Parse<AuthMode>(config["AuthMode"] ?? "OAuthUser");
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
        DisplayName = $"Test Campaign {Guid.NewGuid()}",
        GoalType = "CAMPAIGN_GOAL_TYPE_BRAND_AWARENESS",
        PerformanceGoalType = "PERFORMANCE_GOAL_TYPE_CPM",
        PerformanceGoalAmountMicros = 1_000_000,              // $1.00 CPM target
        BudgetAmountMicros = 1_000_000_000,                  // $1,000.00 total budget
        StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1).AddMonths(1)),
        EntityStatus = "ENTITY_STATUS_PAUSED",
        //BudgetUnit = "BUDGET_UNIT_CURRENCY",
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
            ExitEvents =
            [
                new Dv360ExitEvent
                {
                    Type = "EXIT_EVENT_TYPE_DEFAULT",
                    Url = "https://docs.google.com/forms/d/e/1FAIpQLScUi3Mgz5zBGaIgUR_Z4po5bGWwGkH9fE0Ugh85yyVUjAyOtg/viewform"
                }
            ],
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
        EntityStatus = "ENTITY_STATUS_DRAFT",
        BudgetAmountMicros = 1_000_000_000,                  // $1,000.00 total budget
        StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1).AddMonths(1)),
        PacingPeriod = "PACING_PERIOD_FLIGHT",
        PacingType = "PACING_TYPE_AHEAD", // PACING_PERIOD_FLIGHT
        DailyMaxMicros = 500_000_000,                         // $500.00 daily cap
        KpiType = "KPI_TYPE_CPM",
        KpiAmountMicros = 1_000_000                           // $1.00 CPM target
    },
    LineItems =
    [
        new Dv360LineItem
        {
            DisplayName = "Test Line Item",
            EntityStatus = "ENTITY_STATUS_DRAFT",
            LineItemType = "LINE_ITEM_TYPE_DISPLAY_DEFAULT",
            MaxBudgetAmountMicros = 1_000_000_000,            // $5,000.00 max line item budget
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1).AddMonths(1)),
            PacingPeriod = "PACING_PERIOD_FLIGHT",
            PacingType = "PACING_TYPE_AHEAD", // PACING_PERIOD_FLIGHT
            DailyMaxMicros = 250_000_000,                     // $250.00 daily cap
            FixedBidAmountMicros = 2_000_000,                 // $2.00 fixed CPM bid
            Targeting = new Dv360LineItemTargeting
            {
                // Target United States (geo-region ID 2840).
                GeoTargets =
                [
                    new Dv360GeoTargeting { TargetingOptionId = "2840", Negative = false }
                ],
                // Target desktop and mobile devices.
                DeviceTypeTargets =
                [
                    new Dv360DeviceTypeTargeting { DeviceType = "DEVICE_TYPE_COMPUTER" },
                    new Dv360DeviceTypeTargeting { DeviceType = "DEVICE_TYPE_SMART_PHONE" }
                ],
                // Exclude sexually suggestive and profanity content for brand safety.
                ContentLabelExclusions =
                [
                    new Dv360ContentLabelExclusionTargeting { ContentLabelType = "CONTENT_LABEL_TYPE_SEXUALLY_SUGGESTIVE" },
                    new Dv360ContentLabelExclusionTargeting { ContentLabelType = "CONTENT_LABEL_TYPE_PROFANITY" }
                ]
            }
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
