using System.Text.Json.Serialization;

namespace Suss.TikTok.Client.Infrastructure;

/// <summary>
/// Strongly-typed representation of the standard TikTok Marketing API response envelope.
/// <para>
/// Every Marketing API call returns HTTP 200 with a body shaped as:
/// <code>
/// { "code": 0, "message": "OK", "request_id": "20240101...", "data": { ... } }
/// </code>
/// Success is indicated by <see cref="Code"/> == 0 — <b>not</b> by the HTTP status. The infrastructure
/// layer inspects <see cref="Code"/> and throws when it is non-zero.
/// </para>
/// </summary>
/// <typeparam name="TData">The shape of the endpoint-specific <c>data</c> payload.</typeparam>
public sealed class TikTokApiResponse<TData>
{
    /// <summary>The business status code. <c>0</c> indicates success; any other value is an error.</summary>
    [JsonPropertyName("code")]
    public long Code { get; set; }

    /// <summary>A human-readable status message (e.g., "OK" or a validation error description).</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>The TikTok request identifier, useful for support and tracing.</summary>
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    /// <summary>The endpoint-specific payload. <c>null</c> for errors or empty responses.</summary>
    [JsonPropertyName("data")]
    public TData? Data { get; set; }
}
