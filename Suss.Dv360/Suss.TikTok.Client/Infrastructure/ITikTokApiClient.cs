namespace Suss.TikTok.Client.Infrastructure;

using Suss.TikTok.Client.Models;

/// <summary>
/// Low-level transport abstraction over the TikTok Marketing API.
/// <para>
/// Hides the repetitive concerns of every API call behind a small surface:
/// <list type="bullet">
///   <item><description>Composing the full URL from base host + version + relative path.</description></item>
///   <item><description>Attaching the <c>Access-Token</c> header from the configured auth provider.</description></item>
///   <item><description>Serializing/deserializing JSON and unwrapping the standard envelope.</description></item>
///   <item><description>Translating non-zero envelope codes and transport faults into <see cref="Exceptions.TikTokApiException"/>.</description></item>
/// </list>
/// All resource services depend on this abstraction rather than on <see cref="HttpClient"/> directly,
/// which keeps mapping logic in services and transport logic in one place.
/// </para>
/// </summary>
public interface ITikTokApiClient
{
    /// <summary>
    /// Issues a GET request and returns the unwrapped <c>data</c> payload.
    /// </summary>
    /// <typeparam name="TData">The expected shape of the <c>data</c> payload.</typeparam>
    /// <param name="path">Relative endpoint path (e.g., <c>campaign/get/</c>); version is added automatically.</param>
    /// <param name="queryParameters">Optional query string parameters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The deserialized <c>data</c> payload.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown on a non-zero envelope code or transport failure.</exception>
    Task<TData> GetAsync<TData>(
        string path,
        IDictionary<string, string?>? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a GET request using an already resolved TikTok execution context.
    /// </summary>
    Task<TData> GetAsync<TData>(
        TikTokExecutionContext executionContext,
        string path,
        IDictionary<string, string?>? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a POST request with a JSON body and returns the unwrapped <c>data</c> payload.
    /// </summary>
    /// <typeparam name="TData">The expected shape of the <c>data</c> payload.</typeparam>
    /// <param name="path">Relative endpoint path (e.g., <c>campaign/create/</c>); version is added automatically.</param>
    /// <param name="body">The request body object; serialized as JSON.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The deserialized <c>data</c> payload.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown on a non-zero envelope code or transport failure.</exception>
    Task<TData> PostAsync<TData>(
        string path,
        object body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a POST request using an already resolved TikTok execution context.
    /// </summary>
    Task<TData> PostAsync<TData>(
        TikTokExecutionContext executionContext,
        string path,
        object body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads multipart/form-data content (used for image/video/audio asset uploads) and returns the
    /// unwrapped <c>data</c> payload.
    /// </summary>
    /// <typeparam name="TData">The expected shape of the <c>data</c> payload.</typeparam>
    /// <param name="path">Relative endpoint path (e.g., <c>file/image/ad/upload/</c>).</param>
    /// <param name="content">The pre-built multipart content describing the file and metadata.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The deserialized <c>data</c> payload.</returns>
    /// <exception cref="Exceptions.TikTokApiException">Thrown on a non-zero envelope code or transport failure.</exception>
    Task<TData> PostMultipartAsync<TData>(
        string path,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads multipart/form-data content using an already resolved TikTok execution context.
    /// </summary>
    Task<TData> PostMultipartAsync<TData>(
        TikTokExecutionContext executionContext,
        string path,
        MultipartFormDataContent content,
        CancellationToken cancellationToken = default);
}
