namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a DV360 geographic region targeting option returned from the API.
/// <para>
/// Used to discover the <see cref="TargetingOptionId"/> for a country, region, city, DMA,
/// or postal code without hard-coding numeric identifiers. Consumers can look up geo-region
/// IDs by display name via <see cref="Services.IGeoRegionService"/>.
/// </para>
/// </summary>
public sealed class Dv360GeoRegion
{
    /// <summary>
    /// The targeting option ID to use in <see cref="Dv360GeoTargeting.TargetingOptionId"/>
    /// (e.g., <c>"2840"</c> for United States, <c>"1014044"</c> for Nairobi).
    /// </summary>
    public required string TargetingOptionId { get; set; }

    /// <summary>
    /// The human-readable display name (e.g., <c>"United States"</c>, <c>"London"</c>, <c>"Nairobi"</c>).
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// The geo-region type indicating the granularity level.
    /// <para>
    /// Common values include:
    /// <c>"GEO_REGION_TYPE_COUNTRY"</c>, <c>"GEO_REGION_TYPE_REGION"</c>,
    /// <c>"GEO_REGION_TYPE_CITY"</c>, <c>"GEO_REGION_TYPE_DMA_REGION"</c>,
    /// <c>"GEO_REGION_TYPE_POSTAL_CODE"</c>, <c>"GEO_REGION_TYPE_COUNTY"</c>.
    /// </para>
    /// </summary>
    public required string GeoRegionType { get; set; }
}
