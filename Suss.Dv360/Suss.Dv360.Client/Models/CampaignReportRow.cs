namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a single row of campaign report data with flattened metrics.
/// <para>
/// All metric values are accessible as simple properties. Currency values
/// are provided both as raw micros and as computed decimal properties.
/// Supports DV360 Bid Manager report dimensions and metrics.
/// </para>
/// </summary>
public sealed class CampaignReportRow
{
    // ── Dimension Fields ──────────────────────────────────────────────────

    /// <summary>
    /// The date for this row's data (when grouping by date).
    /// </summary>
    public DateOnly? Date { get; set; }

    /// <summary>
    /// The campaign ID (media plan in Bid Manager terminology).
    /// </summary>
    public long? CampaignId { get; set; }

    /// <summary>
    /// The campaign name.
    /// </summary>
    public string? CampaignName { get; set; }

    /// <summary>
    /// The advertiser ID.
    /// </summary>
    public long? AdvertiserId { get; set; }

    /// <summary>
    /// The advertiser name.
    /// </summary>
    public string? AdvertiserName { get; set; }

    /// <summary>
    /// The insertion order ID.
    /// </summary>
    public long? InsertionOrderId { get; set; }

    /// <summary>
    /// The insertion order name.
    /// </summary>
    public string? InsertionOrderName { get; set; }

    /// <summary>
    /// The line item ID (primary targeting unit in DV360).
    /// </summary>
    public long? LineItemId { get; set; }

    /// <summary>
    /// The line item name.
    /// </summary>
    public string? LineItemName { get; set; }

    /// <summary>
    /// The creative ID (when grouping by creative).
    /// </summary>
    public long? CreativeId { get; set; }

    /// <summary>
    /// The creative name.
    /// </summary>
    public string? CreativeName { get; set; }

    /// <summary>
    /// The exchange ID (when grouping by exchange).
    /// </summary>
    public long? ExchangeId { get; set; }

    /// <summary>
    /// The exchange name (e.g., Google Ad Exchange, OpenX).
    /// </summary>
    public string? ExchangeName { get; set; }

    /// <summary>
    /// The advertiser's currency code (e.g., "USD", "KES").
    /// </summary>
    public string? AdvertiserCurrency { get; set; }

    /// <summary>
    /// The domain of the site where the ad was served (e.g., "cnn.com").
    /// Populated when <c>FILTER_DOMAIN</c> is included as a GroupBy dimension.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// The full app/URL where the ad was served (e.g., "https://cnn.com/news" or an app bundle ID).
    /// Populated when <c>FILTER_APP_URL</c> is included as a GroupBy dimension.
    /// More granular than <see cref="Domain"/>.
    /// </summary>
    public string? AppUrl { get; set; }

    // ── Delivery Metrics ──────────────────────────────────────────────────

    /// <summary>
    /// Total number of ad impressions served.
    /// </summary>
    public long Impressions { get; set; }

    /// <summary>
    /// Total number of clicks on ads.
    /// </summary>
    public long Clicks { get; set; }

    /// <summary>
    /// Total media cost in micros (1/1,000,000 of the currency unit).
    /// </summary>
    public long MediaCostMicros { get; set; }

    /// <summary>
    /// Total media cost as a decimal in standard currency units.
    /// </summary>
    public decimal MediaCost => MediaCostMicros / 1_000_000m;

    /// <summary>
    /// Total revenue in micros (for partner fee calculations).
    /// </summary>
    public long RevenueMicros { get; set; }

    /// <summary>
    /// Total revenue as a decimal in standard currency units.
    /// </summary>
    public decimal Revenue => RevenueMicros / 1_000_000m;

    // ── Performance Metrics ───────────────────────────────────────────────

    /// <summary>
    /// Click-through rate as a decimal (e.g., 0.05 = 5%).
    /// May be directly from API or calculated from Clicks/Impressions.
    /// </summary>
    public decimal Ctr { get; set; }

    /// <summary>
    /// Click-through rate as a percentage (e.g., 5.0 for 5%).
    /// </summary>
    public decimal CtrPercent => Ctr * 100;

    /// <summary>
    /// Cost per click in micros.
    /// </summary>
    public long CpcMicros { get; set; }

    /// <summary>
    /// Cost per click as a decimal in standard currency units.
    /// </summary>
    public decimal Cpc => CpcMicros / 1_000_000m;

    /// <summary>
    /// Cost per mille (thousand impressions) in micros.
    /// </summary>
    public long CpmMicros { get; set; }

    /// <summary>
    /// Cost per mille as a decimal in standard currency units.
    /// </summary>
    public decimal Cpm => CpmMicros / 1_000_000m;

    // ── Conversion Metrics ────────────────────────────────────────────────

    /// <summary>
    /// Total number of conversions (post-click and post-view combined).
    /// </summary>
    public long TotalConversions { get; set; }

    /// <summary>
    /// Conversions that occurred after a user clicked on an ad.
    /// </summary>
    public long PostClickConversions { get; set; }

    /// <summary>
    /// Conversions that occurred after a user viewed an ad (without clicking).
    /// </summary>
    public long PostViewConversions { get; set; }

    /// <summary>
    /// Cost per acquisition/conversion in micros.
    /// </summary>
    public long CpaMicros { get; set; }

    /// <summary>
    /// Cost per acquisition/conversion as a decimal in standard currency units.
    /// </summary>
    public decimal Cpa => CpaMicros / 1_000_000m;

    /// <summary>
    /// Total revenue from post-click conversions in micros.
    /// </summary>
    public long PostClickRevenueMicros { get; set; }

    /// <summary>
    /// Post-click revenue as a decimal in standard currency units.
    /// </summary>
    public decimal PostClickRevenue => PostClickRevenueMicros / 1_000_000m;

    /// <summary>
    /// Total revenue from post-view conversions in micros.
    /// </summary>
    public long PostViewRevenueMicros { get; set; }

    /// <summary>
    /// Post-view revenue as a decimal in standard currency units.
    /// </summary>
    public decimal PostViewRevenue => PostViewRevenueMicros / 1_000_000m;

    /// <summary>
    /// Total conversion revenue (post-click + post-view) in micros.
    /// </summary>
    public long TotalConversionRevenueMicros => PostClickRevenueMicros + PostViewRevenueMicros;

    /// <summary>
    /// Total conversion revenue as a decimal in standard currency units.
    /// </summary>
    public decimal TotalConversionRevenue => TotalConversionRevenueMicros / 1_000_000m;

    /// <summary>
    /// Return on ad spend as a percentage.
    /// Calculated as (TotalConversionRevenue / MediaCost) × 100.
    /// </summary>
    public decimal Roas => MediaCostMicros > 0
        ? (decimal)TotalConversionRevenueMicros / MediaCostMicros * 100
        : 0;

    // ── Video Metrics ─────────────────────────────────────────────────────

    /// <summary>
    /// Total video views (TrueView or video player starts).
    /// </summary>
    public long VideoViews { get; set; }

    /// <summary>
    /// Total video completions (watched to end).
    /// </summary>
    public long VideoCompletions { get; set; }

    /// <summary>
    /// Video completion rate as a decimal.
    /// </summary>
    public decimal VideoCompletionRate => VideoViews > 0
        ? (decimal)VideoCompletions / VideoViews
        : 0;
}
