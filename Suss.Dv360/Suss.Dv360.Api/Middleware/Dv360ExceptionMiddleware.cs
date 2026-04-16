using Microsoft.AspNetCore.Mvc;
using Suss.Dv360.Client.Exceptions;
using System.Text.Json;

namespace Suss.Dv360.Api.Middleware;

/// <summary>
/// Intercepts <see cref="Dv360ApiException"/> and <see cref="TimeoutException"/> thrown
/// anywhere in the pipeline and converts them to RFC 7807 ProblemDetails responses.
/// </summary>
public sealed class Dv360ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<Dv360ExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Dv360ExceptionMiddleware(RequestDelegate next, ILogger<Dv360ExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Dv360ApiException ex)
        {
            _logger.LogError(ex,
                "DV360 API error: {Message} (HTTP {StatusCode})",
                ex.Message, ex.HttpStatusCode);

            await WriteDv360ProblemAsync(context, ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "DV360 report polling timed out");

            context.Response.StatusCode  = StatusCodes.Status504GatewayTimeout;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title  = "Report Generation Timeout",
                Detail = ex.Message
            };
            await JsonSerializer.SerializeAsync(context.Response.Body, problem, _jsonOptions);
        }
    }

    private static async Task WriteDv360ProblemAsync(HttpContext context, Dv360ApiException ex)
    {
        // 4xx from DV360 are caller errors — pass through unchanged.
        // 5xx from DV360 become 502: we are the proxy, DV360 is the upstream.
        // null HttpStatusCode means the exception had no Google API inner exception → 500.
        var status = ex.HttpStatusCode switch
        {
            400 => StatusCodes.Status400BadRequest,
            401 => StatusCodes.Status401Unauthorized,
            403 => StatusCodes.Status403Forbidden,
            404 => StatusCodes.Status404NotFound,
            409 => StatusCodes.Status409Conflict,
            429 => StatusCodes.Status429TooManyRequests,
            >= 500 => StatusCodes.Status502BadGateway,
            null   => StatusCodes.Status500InternalServerError,
            _      => StatusCodes.Status502BadGateway
        };

        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title  = "DV360 API Error",
            Detail = ex.Message
        };

        if (ex.GoogleErrorBody is not null)
            problem.Extensions["googleError"] = ex.GoogleErrorBody;

        if (ex.HttpStatusCode.HasValue)
            problem.Extensions["upstreamStatus"] = ex.HttpStatusCode.Value;

        await JsonSerializer.SerializeAsync(context.Response.Body, problem, _jsonOptions);
    }
}
