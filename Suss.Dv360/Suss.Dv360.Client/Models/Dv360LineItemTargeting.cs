namespace Suss.Dv360.Client.Models;

/// <summary>
/// Container for all targeting parameters that can be applied to a DV360 line item.
/// <para>
/// In the DV360 API v4, targeting is assigned via <c>AssignedTargetingOption</c> resources
/// that are created separately after the line item exists. This model aggregates the
/// supported targeting types into a single object so they can be declared alongside
/// the line item definition and applied automatically by the workflow.
/// </para>
/// <para>
/// Each list property is optional — <c>null</c> or empty means that targeting type is not configured.
/// </para>
/// </summary>
public sealed class Dv360LineItemTargeting
{
    /// <summary>
    /// Geographic targeting options (countries, regions, cities, DMAs, postal codes).
    /// Each entry maps to <c>TARGETING_TYPE_GEO_REGION</c>.
    /// </summary>
    public List<Dv360GeoTargeting>? GeoTargets { get; set; }

    /// <summary>
    /// Device type targeting options (desktop, mobile, tablet, connected TV).
    /// Each entry maps to <c>TARGETING_TYPE_DEVICE_TYPE</c>.
    /// </summary>
    public List<Dv360DeviceTypeTargeting>? DeviceTypeTargets { get; set; }

    /// <summary>
    /// Browser targeting options.
    /// Each entry maps to <c>TARGETING_TYPE_BROWSER</c>.
    /// </summary>
    public List<Dv360BrowserTargeting>? BrowserTargets { get; set; }

    /// <summary>
    /// Channel targeting options (inventory source groups / channel lists).
    /// Each entry maps to <c>TARGETING_TYPE_CHANNEL</c>.
    /// </summary>
    public List<Dv360ChannelTargeting>? ChannelTargets { get; set; }

    /// <summary>
    /// Digital content label exclusions for brand safety.
    /// Each entry maps to <c>TARGETING_TYPE_DIGITAL_CONTENT_LABEL_EXCLUSION</c>.
    /// </summary>
    public List<Dv360ContentLabelExclusionTargeting>? ContentLabelExclusions { get; set; }

    /// <summary>
    /// Content instream position targeting (pre-roll, mid-roll, post-roll) for video line items.
    /// Each entry maps to <c>TARGETING_TYPE_CONTENT_INSTREAM_POSITION</c>.
    /// </summary>
    public List<Dv360ContentInstreamPositionTargeting>? ContentInstreamPositionTargets { get; set; }

    /// <summary>
    /// Viewability targeting options based on predicted viewability percentage.
    /// Each entry maps to <c>TARGETING_TYPE_VIEWABILITY</c>.
    /// </summary>
    public List<Dv360ViewabilityTargeting>? ViewabilityTargets { get; set; }
}

/// <summary>
/// Represents a geographic targeting assignment for a line item.
/// <para>
/// The <see cref="TargetingOptionId"/> corresponds to a DV360 geo-region targeting option
/// (e.g., <c>"2840"</c> for United States). Use the DV360 Targeting Options API to look up
/// available IDs. Set <see cref="Negative"/> to <c>true</c> to exclude the region.
/// </para>
/// </summary>
public sealed class Dv360GeoTargeting
{
    /// <summary>
    /// The DV360 targeting option ID for the geographic region
    /// (e.g., <c>"2840"</c> for United States, <c>"2826"</c> for United Kingdom).
    /// </summary>
    public required string TargetingOptionId { get; set; }

    /// <summary>
    /// When <c>true</c>, the region is excluded rather than targeted.
    /// Defaults to <c>false</c> (positive targeting).
    /// </summary>
    public bool Negative { get; set; }
}

/// <summary>
/// Represents a device type targeting assignment for a line item.
/// <para>
/// Device type targeting in DV360 is positive-only — you include the device types you
/// want to serve on. If no device types are specified, ads serve on all devices.
/// Valid device types include:
/// <c>"DEVICE_TYPE_COMPUTER"</c>, <c>"DEVICE_TYPE_CONNECTED_TV"</c>,
/// <c>"DEVICE_TYPE_SMART_PHONE"</c>, <c>"DEVICE_TYPE_TABLET"</c>.
/// </para>
/// </summary>
public sealed class Dv360DeviceTypeTargeting
{
    /// <summary>
    /// The DV360 device type enum value
    /// (e.g., <c>"DEVICE_TYPE_COMPUTER"</c>, <c>"DEVICE_TYPE_SMART_PHONE"</c>).
    /// </summary>
    public required string DeviceType { get; set; }
}

/// <summary>
/// Represents a browser targeting assignment for a line item.
/// <para>
/// The <see cref="TargetingOptionId"/> corresponds to a DV360 browser targeting option.
/// Use the DV360 Targeting Options API to look up available browser IDs.
/// </para>
/// </summary>
public sealed class Dv360BrowserTargeting
{
    /// <summary>
    /// The DV360 targeting option ID for the browser.
    /// </summary>
    public required string TargetingOptionId { get; set; }

    /// <summary>
    /// When <c>true</c>, the browser is excluded rather than targeted.
    /// Defaults to <c>false</c> (positive targeting).
    /// </summary>
    public bool Negative { get; set; }
}

/// <summary>
/// Represents a channel (inventory source group) targeting assignment for a line item.
/// <para>
/// Channels are curated lists of sites/apps. The <see cref="ChannelId"/> is the DV360 channel
/// resource ID. Set <see cref="Negative"/> to <c>true</c> to block the channel.
/// </para>
/// </summary>
public sealed class Dv360ChannelTargeting
{
    /// <summary>
    /// The DV360 channel resource ID to target or exclude.
    /// </summary>
    public required long ChannelId { get; set; }

    /// <summary>
    /// When <c>true</c>, the channel is excluded rather than targeted.
    /// Defaults to <c>false</c> (positive targeting).
    /// </summary>
    public bool Negative { get; set; }
}

/// <summary>
/// Represents a digital content label exclusion for brand safety.
/// <para>
/// Content labels classify inventory by sensitivity. Excluding a label prevents ads
/// from serving on matching inventory. Maps to the Google SDK's
/// <c>DigitalContentLabelAssignedTargetingOptionDetails.ExcludedContentRatingTier</c>.
/// </para>
/// <para>
/// Common values include:
/// <c>"CONTENT_LABEL_TYPE_SEXUALLY_SUGGESTIVE"</c>, <c>"CONTENT_LABEL_TYPE_BELOW_THE_FOLD"</c>,
/// <c>"CONTENT_LABEL_TYPE_PROFANITY"</c>, <c>"CONTENT_LABEL_TYPE_TRAGEDY"</c>,
/// <c>"CONTENT_LABEL_TYPE_TRANSPORTATION_ACCIDENTS"</c>, <c>"CONTENT_LABEL_TYPE_SENSITIVE_SOCIAL_ISSUES"</c>.
/// </para>
/// </summary>
public sealed class Dv360ContentLabelExclusionTargeting
{
    /// <summary>
    /// The content label type to exclude
    /// (e.g., <c>"CONTENT_LABEL_TYPE_SEXUALLY_SUGGESTIVE"</c>).
    /// </summary>
    public required string ContentLabelType { get; set; }
}

/// <summary>
/// Represents a content instream position targeting assignment for video line items.
/// <para>
/// Controls whether ads serve in pre-roll, mid-roll, or post-roll positions. Common values:
/// <c>"CONTENT_INSTREAM_POSITION_PRE_ROLL"</c>, <c>"CONTENT_INSTREAM_POSITION_MID_ROLL"</c>,
/// <c>"CONTENT_INSTREAM_POSITION_POST_ROLL"</c>.
/// </para>
/// </summary>
public sealed class Dv360ContentInstreamPositionTargeting
{
    /// <summary>
    /// The instream ad position type
    /// (e.g., <c>"CONTENT_INSTREAM_POSITION_PRE_ROLL"</c>).
    /// </summary>
    public required string ContentInstreamPosition { get; set; }
}

/// <summary>
/// Represents a viewability targeting assignment for a line item.
/// <para>
/// Filters inventory by predicted viewability percentage. Maps to the Google SDK's
/// <c>ViewabilityAssignedTargetingOptionDetails.Viewability</c>. Common values:
/// <c>"VIEWABILITY_10_PERCENT_OR_MORE"</c>, <c>"VIEWABILITY_20_PERCENT_OR_MORE"</c>, etc.
/// up to <c>"VIEWABILITY_90_PERCENT_OR_MORE"</c>.
/// </para>
/// </summary>
public sealed class Dv360ViewabilityTargeting
{
    /// <summary>
    /// The DV360 viewability enum value
    /// (e.g., <c>"VIEWABILITY_40_PERCENT_OR_MORE"</c>).
    /// </summary>
    public required string TargetingOptionId { get; set; }
}
