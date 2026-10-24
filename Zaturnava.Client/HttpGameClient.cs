using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Client;

/// <summary>
/// A game server reached over HTTP. <paramref name="http"/> carries the server's base address, so
/// which server (local, MonsterASP, anything else) is the caller's configuration, never this class's.
/// </summary>
/// <remarks>
/// A refusal the contracts know (<see cref="RefusalStatus"/>) becomes a
/// <see cref="GameRequestRefusedException"/> with the server's explanation; any other failure, an
/// <see cref="HttpRequestException"/>.
/// </remarks>
public sealed class HttpGameClient(HttpClient http) : IGameServing
{
    private static readonly JsonSerializerOptions Wire = JsonSerializerOptions.Web;

    private readonly HttpClient _http = http;

    public Task<CatalogDto> Catalog(CancellationToken cancellationToken) =>
        this.Send<CatalogDto>(HttpMethod.Get, GameRoutes.Catalog, null, null, cancellationToken);

    public Task<ImmutableList<GameSummaryDto>> List(CancellationToken cancellationToken) =>
        this.Send<ImmutableList<GameSummaryDto>>(HttpMethod.Get, GameRoutes.Games, null, null, cancellationToken);

    public Task<GameCreatedDto> Create([Required] CreateGameRequest request, CancellationToken cancellationToken) =>
        this.Send<GameCreatedDto>(HttpMethod.Post, GameRoutes.Games, request, null, cancellationToken);

    public Task<GameViewDto> Get([Required] string gameId, CancellationToken cancellationToken) =>
        this.Send<GameViewDto>(HttpMethod.Get, GameRoutes.Game(gameId), null, null, cancellationToken);

    public Task<SeatClaimedDto> Claim([Required] string gameId, int seat, CancellationToken cancellationToken) =>
        this.Send<SeatClaimedDto>(HttpMethod.Post, GameRoutes.Seat(gameId, seat), null, null, cancellationToken);

    public Task<GameViewDto> Submit(
        [Required] string gameId,
        [Required] string token,
        [Required] ActionDto action,
        CancellationToken cancellationToken
    ) =>
        this.Send<GameViewDto>(HttpMethod.Post, GameRoutes.Actions(gameId), action, token, cancellationToken);

    private async Task<T> Send<T>(
        HttpMethod method,
        string route,
        object? body,
        string? token,
        CancellationToken cancellationToken
    )
    {
        using HttpRequestMessage request = new(method, route);
        request.Content = body is null ? null : JsonContent.Create(body, body.GetType(), options: Wire);
        request.Headers.Authorization = token is null
            ? null
            : new AuthenticationHeaderValue(GameRoutes.TokenScheme, token);
        using HttpResponseMessage response = await this._http.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await Read<T>(response, cancellationToken)
            : throw await Refused(response, cancellationToken);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<T>(Wire, cancellationToken)
        ?? throw new HttpRequestException($"The game server sent no {typeof(T).Name}.");

    private static async Task<Exception> Refused(HttpResponseMessage response, CancellationToken cancellationToken) =>
        RefusalStatus.RefusalOf((int)response.StatusCode) is { } refusal
            ? new GameRequestRefusedException(refusal, await Explanation(response, cancellationToken))
            : new HttpRequestException(
                $"The game server answered {(int)response.StatusCode} ({response.ReasonPhrase}).",
                null,
                response.StatusCode
            );

    private static async Task<string> Explanation(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        Problem? problem = await response.Content.ReadFromJsonAsync<Problem>(Wire, cancellationToken);
        return problem?.Detail ?? $"The game server refused the request ({(int)response.StatusCode}).";
    }
}

/// <summary>The part of an RFC 9457 problem the client shows: what went wrong, for a person.</summary>
file sealed record Problem(string? Detail);
