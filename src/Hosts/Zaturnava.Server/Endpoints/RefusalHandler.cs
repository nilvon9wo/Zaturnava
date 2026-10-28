using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>
/// Answers a refused request with an RFC 9457 problem, at the status <see cref="RefusalStatus"/>
/// gives its <see cref="Refusal"/>; anything else is left to fail as a server error.
/// </summary>
/// <remarks>
/// Every argument the application checks (Stipulate's <see cref="ArgumentException"/>s) comes from
/// the request, so one it rejects is the request's fault.
/// </remarks>
internal sealed class RefusalHandler(IProblemDetailsService problems) : IExceptionHandler
{
    private readonly IProblemDetailsService _problems = problems;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    ) =>
        RefusalOf(exception) is { } refusal
        && await this.Answer(httpContext, exception, refusal);

    private static Refusal? RefusalOf(Exception exception) =>
        exception switch
        {
            GameRequestException refused => FromFailure(refused.Failure),
            IllegalActionException => Refusal.Illegal,
            MalformedActionException or ArgumentException or BadHttpRequestException => Refusal.Invalid,
            _ => null,
        };

    private static Refusal FromFailure(GameRequestFailure failure) =>
        failure switch
        {
            GameRequestFailure.NotFound => Refusal.NotFound,
            GameRequestFailure.Forbidden => Refusal.Forbidden,
            GameRequestFailure.Conflict => Refusal.Conflict,
            _ => Refusal.Invalid,
        };

    private ValueTask<bool> Answer(HttpContext httpContext, Exception exception, Refusal refusal)
    {
        int status = RefusalStatus.CodeOf(refusal);
        httpContext.Response.StatusCode = status;
        return this._problems.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = refusal.ToString(),
                    Detail = exception.Message,
                },
            }
        );
    }
}
