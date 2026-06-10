namespace Suss.TikTok.Client.Exceptions;

/// <summary>
/// Represents an error returned by the TikTok Marketing API.
/// <para>
/// The TikTok Marketing API is "envelope based": every response returns HTTP 200 with a JSON body
/// shaped as <c>{ "code": 0, "message": "OK", "request_id": "...", "data": { ... } }</c>. A
/// non-zero <see cref="Code"/> indicates a business/validation failure even though the HTTP status
/// is success. All services in this library translate non-zero envelopes (and transport failures)
/// into a <see cref="TikTokApiException"/> so consumers never have to parse raw envelopes.
/// </para>
/// </summary>
public class TikTokApiException : Exception
{
    /// <summary>
    /// The TikTok business error code from the response envelope (<c>code</c> field).
    /// A value of <c>0</c> means success; any other value indicates an error. <c>null</c> when the
    /// failure occurred at the transport layer before an envelope could be parsed.
    /// </summary>
    public long? Code { get; }

    /// <summary>
    /// The TikTok request identifier (<c>request_id</c>) echoed in every response. Include this when
    /// contacting TikTok support to trace a specific call.
    /// </summary>
    public string? RequestId { get; }

    /// <summary>
    /// The raw JSON body returned by the API, retained for diagnostics when available.
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// Initializes a new <see cref="TikTokApiException"/> with the specified message.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    public TikTokApiException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new <see cref="TikTokApiException"/> with the specified message and inner exception.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="innerException">The underlying exception that caused this failure.</param>
    public TikTokApiException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new <see cref="TikTokApiException"/> from a parsed error envelope.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="code">The TikTok business error code.</param>
    /// <param name="requestId">The TikTok request identifier.</param>
    /// <param name="responseBody">The raw JSON response body.</param>
    public TikTokApiException(string message, long? code, string? requestId, string? responseBody)
        : base(message)
    {
        Code = code;
        RequestId = requestId;
        ResponseBody = responseBody;
    }
}
