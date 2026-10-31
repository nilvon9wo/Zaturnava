using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// How much one address may ask of the server, so that no one client can wear it out for everyone: at most
/// <c>Limits:RequestsPerMinute</c> requests a minute (600 unless configured), and, of those, at most
/// <c>Limits:GamesPerHour</c> new games an hour (60 unless configured). The hosted MCP server's calls to this
/// server count as its caller's (<see cref="SelfPass"/>). A request over either limit is refused with
/// <see cref="Refusal.TooBusy"/> (429).
/// </summary>
internal static class RequestLimits
{
    public const string PerMinuteKey = "Limits:RequestsPerMinute";
    public const string GamesPerHourKey = "Limits:GamesPerHour";

    /// <summary>The policy of the route that creates a game.</summary>
    public const string NewGames = "new-games";

    public const int DefaultPerMinute = 600;
    public const int DefaultGamesPerHour = 60;

    public static IServiceCollection AddRequestLimits(this IServiceCollection services, IConfiguration configuration)
    {
        int perMinute = configuration.GetValue(PerMinuteKey, DefaultPerMinute);
        int gamesPerHour = configuration.GetValue(GamesPerHourKey, DefaultGamesPerHour);
        SelfPass pass = new();
        return services
            .AddSingleton(pass)
            .AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    Window(pass.AddressOf(context), perMinute, TimeSpan.FromMinutes(1))
                );
                _ = options.AddPolicy(
                    NewGames,
                    context => Window(pass.AddressOf(context), gamesPerHour, TimeSpan.FromHours(1))
                );
                options.OnRejected = Refuse;
            });
    }

    private static RateLimitPartition<string> Window(string address, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            address,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 }
        );

    private static ValueTask Refuse(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        HttpContext context = rejected.HttpContext;
        context.Response.StatusCode = RefusalStatus.TooBusy;
        return context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new ProblemDetails
                {
                    Status = RefusalStatus.TooBusy,
                    Title = nameof(Refusal.TooBusy),
                    Detail = "Too many requests from this address of late: wait a minute, and ask again.",
                },
            }
        );
    }
}
