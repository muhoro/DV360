namespace Suss.Dv360.Client.Configuration;

/// <summary>
/// Configuration options for report polling behavior.
/// </summary>
public sealed class ReportPollingOptions
{
    /// <summary>
    /// The initial delay between polling attempts.
    /// Defaults to 5 seconds.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The maximum delay between polling attempts.
    /// Defaults to 60 seconds.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The multiplier applied to the delay after each attempt (exponential backoff).
    /// Defaults to 2.0 (double the delay each time).
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// The maximum number of polling attempts before timing out.
    /// Defaults to 60 attempts (with exponential backoff, this is roughly 30 minutes).
    /// </summary>
    public int MaxAttempts { get; set; } = 60;

    /// <summary>
    /// The maximum total time to wait for a report to complete.
    /// Defaults to 30 minutes.
    /// </summary>
    public TimeSpan MaxTotalWait { get; set; } = TimeSpan.FromMinutes(30);
}
