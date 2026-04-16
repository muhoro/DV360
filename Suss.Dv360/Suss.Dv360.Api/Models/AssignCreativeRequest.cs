namespace Suss.Dv360.Api.Models;

/// <summary>Request body for assigning a creative to a line item.</summary>
public sealed class AssignCreativeRequest
{
    /// <summary>The DV360 creative ID to link to the line item.</summary>
    public required long CreativeId { get; set; }
}
