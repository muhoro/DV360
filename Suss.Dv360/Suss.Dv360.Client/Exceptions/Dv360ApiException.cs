using Google;

namespace Suss.Dv360.Client.Exceptions;

/// <summary>
/// Represents an error returned by the Display &amp; Video 360 API.
/// <para>
/// Wraps the underlying <see cref="GoogleApiException"/> with a user-friendly message and
/// extracts the HTTP status code and error body for easier diagnostics. All service
/// classes in this library throw <see cref="Dv360ApiException"/> instead of letting
/// raw <see cref="GoogleApiException"/> leak to consumers.
/// </para>
/// </summary>
public class Dv360ApiException : Exception
{
    /// <summary>
    /// The HTTP status code returned by the DV360 API, or <c>null</c> if the inner
    /// exception is not a <see cref="GoogleApiException"/>.
    /// </summary>
    public int? HttpStatusCode { get; }

    /// <summary>
    /// The serialized Google error body returned by the DV360 API, or <c>null</c> if
    /// no structured error payload was available.
    /// </summary>
    public string? GoogleErrorBody { get; }

    /// <summary>
    /// Initializes a new <see cref="Dv360ApiException"/> with the specified message.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    public Dv360ApiException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new <see cref="Dv360ApiException"/> with the specified message and inner exception.
    /// If the inner exception is a <see cref="GoogleApiException"/>, the HTTP status code and
    /// error body are automatically extracted.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="innerException">The underlying exception that caused this failure.</param>
    public Dv360ApiException(string message, Exception innerException)
        : base(message, innerException)
    {
        // Extract Google-specific error details when available.
        if (innerException is GoogleApiException googleEx)
        {
            HttpStatusCode = (int)googleEx.HttpStatusCode;
            GoogleErrorBody = googleEx.Error?.ToString();
        }
    }
}
