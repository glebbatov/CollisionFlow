using CollisionFlow.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CollisionFlow.Api.ErrorHandling;

/// <summary>
/// Turns a broken business rule into a well-formed HTTP response.
/// </summary>
/// <remarks>
/// Controllers reject illegal transitions before they reach the domain, so this
/// handler should rarely fire. It exists so that if a future code path forgets to
/// ask permission first, the caller still receives a documented 422 and a trace id
/// rather than a stack trace and a 500.
/// </remarks>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            // Not ours. Let the pipeline handle it as an unexpected failure.
            return false;
        }

        _logger.LogWarning(
            domainException,
            "Business rule rejected a request to {Path}.",
            httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "The request was understood but violates a business rule.",
            Detail = domainException.Message,
            Instance = httpContext.Request.Path,
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
