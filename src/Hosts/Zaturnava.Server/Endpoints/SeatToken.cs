using System.Net.Http.Headers;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Endpoints;

/// <summary>The seat token a request carries, as <c>Authorization: Bearer {token}</c>.</summary>
internal static class SeatToken
{
    /// <exception cref="MissingSeatTokenException">The request carries no token.</exception>
    public static string From(HttpRequest request, GameId game) =>
        OrNull(request) ?? throw new MissingSeatTokenException(game);

    /// <summary>The token, if the request carries one: a watcher's carries none.</summary>
    public static string? OrNull(HttpRequest request) =>
        AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out AuthenticationHeaderValue? header)
        && string.Equals(header.Scheme, GameRoutes.TokenScheme, StringComparison.OrdinalIgnoreCase)
            ? header.Parameter
            : null;
}
