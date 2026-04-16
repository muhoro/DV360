namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents a date range for report queries.
/// <para>
/// Use either <see cref="RelativeDateRange"/> for predefined ranges (e.g., "LAST_30_DAYS")
/// or specify custom <see cref="StartDate"/> and <see cref="EndDate"/> values.
/// </para>
/// </summary>
public sealed class ReportDateRange
{
    /// <summary>
    /// A predefined relative date range using Bid Manager <c>Range</c> enum values.
    /// When set, <see cref="StartDate"/> and <see cref="EndDate"/> are ignored.
    /// </summary>
    /// <remarks>
    /// Accepted values (Bid Manager API <c>Range</c> enum):
    /// <list type="table">
    ///   <listheader><term>Value</term><description>Description</description></listheader>
    ///   <item><term>CURRENT_DAY</term><description>Today</description></item>
    ///   <item><term>PREVIOUS_DAY</term><description>Yesterday</description></item>
    ///   <item><term>WEEK_TO_DATE</term><description>Sunday through today</description></item>
    ///   <item><term>MONTH_TO_DATE</term><description>First of month through today</description></item>
    ///   <item><term>QUARTER_TO_DATE</term><description>First of quarter through today</description></item>
    ///   <item><term>YEAR_TO_DATE</term><description>Jan 1 through today</description></item>
    ///   <item><term>PREVIOUS_WEEK</term><description>Previous completed week (Sun–Sat)</description></item>
    ///   <item><term>PREVIOUS_MONTH</term><description>Previous completed calendar month</description></item>
    ///   <item><term>PREVIOUS_QUARTER</term><description>Previous completed quarter</description></item>
    ///   <item><term>PREVIOUS_YEAR</term><description>Previous completed calendar year</description></item>
    ///   <item><term>LAST_7_DAYS</term><description>Previous 7 days, excluding today</description></item>
    ///   <item><term>LAST_14_DAYS</term><description>Previous 14 days, excluding today</description></item>
    ///   <item><term>LAST_30_DAYS</term><description>Previous 30 days, excluding today</description></item>
    ///   <item><term>LAST_60_DAYS</term><description>Previous 60 days, excluding today</description></item>
    ///   <item><term>LAST_90_DAYS</term><description>Previous 90 days, excluding today</description></item>
    ///   <item><term>LAST_365_DAYS</term><description>Previous 365 days, excluding today</description></item>
    ///   <item><term>ALL_TIME</term><description>All available data, excluding today</description></item>
    /// </list>
    /// Friendly aliases (<c>CURRENT_WEEK</c>, <c>CURRENT_MONTH</c>, <c>CURRENT_QUARTER</c>,
    /// <c>CURRENT_YEAR</c>) are also accepted and mapped to their API equivalents.
    /// </remarks>
    public string? RelativeDateRange { get; set; }

    /// <summary>
    /// The start date for a custom date range.
    /// Required when <see cref="RelativeDateRange"/> is not specified.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// The end date for a custom date range.
    /// Required when <see cref="RelativeDateRange"/> is not specified.
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Creates a date range for the last N days.
    /// </summary>
    public static ReportDateRange Last7Days => new() { RelativeDateRange = "LAST_7_DAYS" };

    /// <summary>
    /// Creates a date range for the last 30 days.
    /// </summary>
    public static ReportDateRange Last30Days => new() { RelativeDateRange = "LAST_30_DAYS" };

    /// <summary>
    /// Creates a date range for the last 90 days.
    /// </summary>
    public static ReportDateRange Last90Days => new() { RelativeDateRange = "LAST_90_DAYS" };

    /// <summary>
    /// Creates a custom date range with specific start and end dates.
    /// </summary>
    public static ReportDateRange Custom(DateOnly startDate, DateOnly endDate) =>
        new() { StartDate = startDate, EndDate = endDate };
}
