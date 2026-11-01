using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SampleBot;

/// <summary>
/// The HTTP API, as a bot uses it: create a game, claim a seat, read the game, act, and let the server's own bots
/// move. Every route is in docs/bots/http-api.md. The hosted MCP server, for AI assistants, calls these same
/// routes: an assistant's tools and this class do the same things.
/// </summary>
internal sealed class GameApi(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http = http;

    public Task<Catalog> Catalog(CancellationToken cancellationToken) =>
        this.Send<Catalog>(HttpMethod.Get, "api/catalog", null, null, cancellationToken);

    public async Task<string> Create(string rulesetId, IEnumerable<string?> bots, CancellationToken cancellationToken)
    {
        GameCreated created = await this.Send<GameCreated>(
            HttpMethod.Post,
            "api/games",
            new CreateGame(rulesetId, [.. bots]),
            null,
            cancellationToken
        );
        return created.GameId;
    }

    /// <summary>
    /// Claims the seat, returning its token: shown once, and the only way to act for the seat. With a
    /// <paramref name="playerToken"/> (an AI token from a player's profile, say), the seat is that player's.
    /// </summary>
    public async Task<string> Claim(
        string gameId,
        int seat,
        ClaimSeat claim,
        string? playerToken,
        CancellationToken cancellationToken
    )
    {
        SeatClaimed claimed = await this.Send<SeatClaimed>(
            HttpMethod.Post,
            $"api/games/{gameId}/seats/{seat.ToString(CultureInfo.InvariantCulture)}",
            claim,
            playerToken,
            cancellationToken
        );
        return claimed.Token;
    }

    public Task<GameView> Get(string gameId, CancellationToken cancellationToken) =>
        this.Send<GameView>(HttpMethod.Get, $"api/games/{gameId}", null, null, cancellationToken);

    public Task<GameView> Act(
        string gameId,
        GameAction action,
        string seatToken,
        CancellationToken cancellationToken
    ) =>
        this.Send<GameView>(HttpMethod.Post, $"api/games/{gameId}/actions", action, seatToken, cancellationToken);

    /// <summary>Has the bot (or timer) whose decision it is make one, if the game is still at <c>version</c>.</summary>
    public Task<GameView> Advance(string gameId, int version, CancellationToken cancellationToken) =>
        this.Send<GameView>(
            HttpMethod.Post,
            $"api/games/{gameId}/advance",
            new Advance(version),
            null,
            cancellationToken
        );

    private async Task<T> Send<T>(
        HttpMethod method,
        string route,
        object? body,
        string? token,
        CancellationToken cancellationToken
    )
    {
        using HttpRequestMessage request = new(method, new Uri(route, UriKind.Relative))
        {
            Content = body is null ? null : JsonContent.Create(body, body.GetType(), options: Json),
        };
        request.Headers.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage response = await this._http.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
                ?? throw new RefusedException(response.StatusCode, "The server sent nothing back.")
            : throw await Refusal(response, cancellationToken);
    }

    private static async Task<RefusedException> Refusal(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        string text = await response.Content.ReadAsStringAsync(cancellationToken);
        Problem? problem = text.StartsWith('{') ? JsonSerializer.Deserialize<Problem>(text, Json) : null;
        return new RefusedException(response.StatusCode, $"{problem?.Title}: {problem?.Detail ?? text}");
    }
}

/// <summary>A request the server refused (docs/bots/http-api.md#refusals).</summary>
internal sealed class RefusedException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}
