namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a Display &amp; Video 360 line item with flattened budget, flight, and pacing settings.
/// <para>
/// Internally maps to the Google SDK's <c>LineItem</c> model, which nests budget in
/// <c>LineItemBudget</c>, flight dates in <c>LineItemFlight</c>, and pacing in <c>Pacing</c>.
/// Line items sit under an insertion order and define the targeting, creative delivery,
/// and budget allocation for ad serving.
/// </para>
/// </summary>
public sealed class Dv360LineItem
{
    /// <summary>
    /// The DV360-assigned line item identifier. <c>null</c> until the line item is created.
    /// </summary>
    public long? LineItemId { get; set; }

    /// <summary>
    /// The parent campaign's identifier. Set automatically by the workflow.
    /// </summary>
    public long CampaignId { get; set; }

    /// <summary>
    /// The parent insertion order's identifier. Set automatically by the workflow.
    /// </summary>
    public long InsertionOrderId { get; set; }

    /// <summary>
    /// Human-readable name of the line item shown in the DV360 UI.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// The type of line item (e.g., <c>"LINE_ITEM_TYPE_DISPLAY_DEFAULT"</c>,
    /// <c>"LINE_ITEM_TYPE_VIDEO_DEFAULT"</c>).
    /// </summary>
    public required string LineItemType { get; set; }

    /// <summary>
    /// The entity lifecycle status. Defaults to <c>"ENTITY_STATUS_DRAFT"</c>.
    /// </summary>
    public string EntityStatus { get; set; } = "ENTITY_STATUS_DRAFT";

    // ── Flight ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The type of flight date configuration. Defaults to <c>"LINE_ITEM_FLIGHT_DATE_TYPE_CUSTOM"</c>.
    /// Set to <c>"LINE_ITEM_FLIGHT_DATE_TYPE_INHERITED"</c> to inherit dates from the parent insertion order.
    /// </summary>
    public string FlightDateType { get; set; } = "LINE_ITEM_FLIGHT_DATE_TYPE_CUSTOM";

    /// <summary>
    /// The start date of the line item's custom flight window.
    /// Required when <see cref="FlightDateType"/> is <c>"LINE_ITEM_FLIGHT_DATE_TYPE_CUSTOM"</c>.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// The end date of the line item's custom flight window.
    /// Required when <see cref="FlightDateType"/> is <c>"LINE_ITEM_FLIGHT_DATE_TYPE_CUSTOM"</c>.
    /// </summary>
    public DateOnly? EndDate { get; set; }

    // ── Budget ──────────────────────────────────────────────────────────────

    /// <summary>
    /// How the budget is allocated (e.g., <c>"LINE_ITEM_BUDGET_ALLOCATION_TYPE_FIXED"</c>).
    /// Defaults to fixed allocation.
    /// </summary>
    public string BudgetAllocationType { get; set; } = "LINE_ITEM_BUDGET_ALLOCATION_TYPE_FIXED";

    /// <summary>
    /// The budget unit type. Defaults to <c>"BUDGET_UNIT_CURRENCY"</c>.
    /// </summary>
    public string BudgetUnit { get; set; } = "BUDGET_UNIT_CURRENCY";

    /// <summary>
    /// The maximum budget amount in micros for this line item's lifetime.
    /// </summary>
    public long MaxBudgetAmountMicros { get; set; }

    // ── Pacing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Controls the pacing time period. Defaults to <c>"PACING_PERIOD_DAILY"</c>.
    /// </summary>
    public string PacingPeriod { get; set; } = "PACING_PERIOD_DAILY";

    /// <summary>
    /// Controls the pacing algorithm. Defaults to <c>"PACING_TYPE_AHEAD"</c>.
    /// </summary>
    public string PacingType { get; set; } = "PACING_TYPE_AHEAD";

    /// <summary>
    /// The maximum daily spend in micros when pacing period is daily
    /// and pacing type is <c>"PACING_TYPE_AHEAD"</c>. Not used with <c>"PACING_TYPE_EVEN"</c>.
    /// </summary>
    public long DailyMaxMicros { get; set; }

    // ── Bid Strategy ────────────────────────────────────────────────────────

    /// <summary>
    /// The fixed bid amount in micros for a <c>FixedBidStrategy</c>.
    /// When set, the line item uses a fixed CPM bid. Mutually exclusive with
    /// <see cref="MaximizeSpendPerformanceGoalType"/>.
    /// </summary>
    public long? FixedBidAmountMicros { get; set; }

    /// <summary>
    /// The performance goal type for a <c>MaximizeSpendBidStrategy</c>
    /// (e.g., <c>"BIDDING_STRATEGY_PERFORMANCE_GOAL_TYPE_CPC"</c>,
    /// <c>"BIDDING_STRATEGY_PERFORMANCE_GOAL_TYPE_VIEWABLE_CPM"</c>).
    /// When set, the line item uses auto-bidding to maximize spend. Mutually exclusive with
    /// <see cref="FixedBidAmountMicros"/>.
    /// </summary>
    public string? MaximizeSpendPerformanceGoalType { get; set; }

    /// <summary>
    /// Optional cap on the average CPM bid in micros when using <c>MaximizeSpendBidStrategy</c>.
    /// <c>null</c> for uncapped auto-bidding.
    /// </summary>
    public long? MaxAverageCpmBidAmountMicros { get; set; }

    // ── Frequency Cap ───────────────────────────────────────────────────────

    /// <summary>
    /// Whether the frequency cap is unlimited. Defaults to <c>true</c>.
    /// Set to <c>false</c> and configure <see cref="FrequencyCapMaxImpressions"/>,
    /// <see cref="FrequencyCapTimeUnit"/>, and <see cref="FrequencyCapTimeUnitCount"/>
    /// for a limited cap.
    /// </summary>
    public bool FrequencyCapUnlimited { get; set; } = true;

    /// <summary>
    /// The maximum number of impressions to serve per user within the frequency cap window.
    /// Only used when <see cref="FrequencyCapUnlimited"/> is <c>false</c>.
    /// </summary>
    public int? FrequencyCapMaxImpressions { get; set; }

    /// <summary>
    /// The time unit for the frequency cap (e.g., <c>"TIME_UNIT_DAYS"</c>, <c>"TIME_UNIT_WEEKS"</c>,
    /// <c>"TIME_UNIT_MONTHS"</c>, <c>"TIME_UNIT_LIFETIME"</c>).
    /// Only used when <see cref="FrequencyCapUnlimited"/> is <c>false</c>.
    /// </summary>
    public string? FrequencyCapTimeUnit { get; set; }

    /// <summary>
    /// The number of <see cref="FrequencyCapTimeUnit"/> periods for the frequency cap window.
    /// Only used when <see cref="FrequencyCapUnlimited"/> is <c>false</c>.
    /// </summary>
    public int? FrequencyCapTimeUnitCount { get; set; }

    // ── Partner Revenue Model ───────────────────────────────────────────────

    /// <summary>
    /// The markup type of the partner revenue model. Defaults to
    /// <c>"PARTNER_REVENUE_MODEL_MARKUP_TYPE_TOTAL_MEDIA_COST_MARKUP"</c>.
    /// </summary>
    public string PartnerRevenueModelMarkupType { get; set; } = "PARTNER_REVENUE_MODEL_MARKUP_TYPE_TOTAL_MEDIA_COST_MARKUP";

    /// <summary>
    /// The markup amount of the partner revenue model. Interpretation depends on
    /// <see cref="PartnerRevenueModelMarkupType"/>. Defaults to <c>0</c>.
    /// </summary>
    public long PartnerRevenueModelMarkupAmount { get; set; }

    // ── Conversion Counting ─────────────────────────────────────────────────

    /// <summary>
    /// Optional conversion counting configuration for the line item.
    /// When <c>null</c>, conversion counting is not explicitly configured.
    /// </summary>
    public Dv360ConversionCountingConfig? ConversionCounting { get; set; }

    // ── Integration Details ─────────────────────────────────────────────────

    /// <summary>
    /// Optional integration code for the line item.
    /// Maps to the <c>integrationDetails.integrationCode</c> field in the API.
    /// </summary>
    public string? IntegrationCode { get; set; }

    /// <summary>
    /// Optional integration details for the line item.
    /// Maps to the <c>integrationDetails.details</c> field in the API.
    /// </summary>
    public string? IntegrationDetails { get; set; }

    // ── Exchanges ───────────────────────────────────────────────────────────

    /// <summary>
    /// Whether to exclude new exchanges from automatically being targeted.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool ExcludeNewExchanges { get; set; }

    // ── EU Political Ads ────────────────────────────────────────────────────

    /// <summary>
    /// Whether this line item contains EU political ads.
    /// Must be set when creating a new line item, otherwise the create request will fail
    /// (unless the parent advertiser has set <c>DOES_NOT_CONTAIN_EU_POLITICAL_ADVERTISING</c>).
    /// <para>
    /// Typical values: <c>"DOES_NOT_CONTAIN_EU_POLITICAL_ADVERTISING"</c>,
    /// <c>"CONTAINS_EU_POLITICAL_ADVERTISING"</c>.
    /// </para>
    /// </summary>
    public string ContainsEuPoliticalAds { get; set; } = "DOES_NOT_CONTAIN_EU_POLITICAL_ADVERTISING";

    // ── Mobile App ──────────────────────────────────────────────────────────

    /// <summary>
    /// The mobile app ID promoted by the line item. Required when <see cref="LineItemType"/>
    /// is <c>LINE_ITEM_TYPE_DISPLAY_MOBILE_APP_INSTALL</c> or <c>LINE_ITEM_TYPE_VIDEO_MOBILE_APP_INSTALL</c>.
    /// </summary>
    public string? MobileAppId { get; set; }

    /// <summary>
    /// The mobile app platform. Required alongside <see cref="MobileAppId"/>.
    /// Typical values: <c>"IOS"</c>, <c>"ANDROID"</c>.
    /// </summary>
    public string? MobileAppPlatform { get; set; }

    // ── Read-Only / Output Fields ───────────────────────────────────────────

    /// <summary>
    /// Output only. The timestamp when the line item was last updated. Populated on read.
    /// </summary>
    public DateTimeOffset? UpdateTime { get; set; }

    /// <summary>
    /// Output only. The reservation type of the line item.
    /// </summary>
    public string? ReservationType { get; set; }

    /// <summary>
    /// Output only. Warning messages generated by the line item.
    /// </summary>
    public IReadOnlyList<string>? WarningMessages { get; set; }

    // ── Targeting ────────────────────────────────────────────────────────────

    /// <summary>
    /// Optional targeting parameters to assign to this line item after creation.
    /// <para>
    /// When not <c>null</c>, the workflow will create <c>AssignedTargetingOption</c> resources
    /// for each configured targeting type via the DV360 API. Leave <c>null</c> to skip
    /// targeting assignment (the line item will use default/inherited targeting).
    /// </para>
    /// </summary>
    public Dv360LineItemTargeting? Targeting { get; set; }
}

/// <summary>
/// Configuration for conversion counting on a line item.
/// </summary>
public sealed class Dv360ConversionCountingConfig
{
    /// <summary>
    /// The percentage of post-view conversions to count, in millis (1/1000 of a percent).
    /// Must be between 0 and 100000 inclusive.
    /// </summary>
    public long? PostViewCountPercentageMillis { get; set; }

    /// <summary>
    /// The Floodlight activity configs used to track conversions.
    /// </summary>
    public List<Dv360FloodlightActivityConfig>? FloodlightActivityConfigs { get; set; }
}

/// <summary>
/// Settings for a single Floodlight activity used for conversion tracking.
/// </summary>
public sealed class Dv360FloodlightActivityConfig
{
    /// <summary>
    /// The ID of the Floodlight activity.
    /// </summary>
    public required long FloodlightActivityId { get; set; }

    /// <summary>
    /// The number of days after an ad click in which a conversion may be counted.
    /// Must be between 0 and 90 inclusive.
    /// </summary>
    public required int PostClickLookbackWindowDays { get; set; }

    /// <summary>
    /// The number of days after an ad view in which a conversion may be counted.
    /// Must be between 0 and 90 inclusive.
    /// </summary>
    public required int PostViewLookbackWindowDays { get; set; }
}
