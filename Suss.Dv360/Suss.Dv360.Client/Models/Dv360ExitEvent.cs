namespace Suss.Dv360.Client.Models;

/// <summary>
/// Represents an exit event (click-through URL) on a DV360 creative.
/// <para>
/// The DV360 API requires every creative to have at least one exit event.
/// Exit events define the landing page URLs that users are directed to when
/// they click on the creative.
/// </para>
/// </summary>
public sealed class Dv360ExitEvent
{
    /// <summary>
    /// The type of exit event (e.g., <c>"EXIT_EVENT_TYPE_DEFAULT"</c>,
    /// <c>"EXIT_EVENT_TYPE_BACKUP"</c>).
    /// Defaults to <c>"EXIT_EVENT_TYPE_DEFAULT"</c>.
    /// </summary>
    public string Type { get; set; } = "EXIT_EVENT_TYPE_DEFAULT";

    /// <summary>
    /// The click-through URL that the user is directed to when they interact with
    /// this exit event.
    /// </summary>
    public required string Url { get; set; }
}
