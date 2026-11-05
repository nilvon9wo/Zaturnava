using System.Collections.Immutable;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpSampleBot;

/// <summary>
/// The server's MCP tools, as a bot uses them: create a game, claim a seat, read the game, act, and let the server's
/// own bots move. Every tool is in docs/bots/mcp.md; each is one route of the HTTP API, and answers with that route's
/// JSON, as text. A refused request comes back as the tool's error: "Refused (Conflict): why", say.
/// </summary>
internal sealed class GameTools(McpClient client)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly McpClient _client = client;

    public Task<Catalog> Catalog(CancellationToken cancellationToken) =>
        this.Call<Catalog>("catalog", ImmutableDictionary<string, object?>.Empty, cancellationToken);

    public async Task<string> Create(string rulesetId, IEnumerable<string?> bots, CancellationToken cancellationToken)
    {
        GameCreated created = await this.Call<GameCreated>(
            "create_game",
            ImmutableDictionary<string, object?>.Empty.Add("rulesetId", rulesetId).Add("bots", bots.ToArray()),
            cancellationToken
        );
        return created.GameId;
    }

    /// <summary>
    /// Claims the seat, as an AI's, returning its token: shown once, and the only way to act for the seat. With a
    /// <paramref name="playerToken"/> (an AI token from a player's profile, say), the seat is that player's.
    /// </summary>
    public async Task<string> Claim(
        string gameId,
        int seat,
        string nickname,
        string? playerToken,
        CancellationToken cancellationToken
    )
    {
        SeatClaimed claimed = await this.Call<SeatClaimed>(
            "claim_seat",
            ImmutableDictionary<string, object?>.Empty
                .Add("gameId", gameId)
                .Add("seat", seat)
                .Add("nickname", nickname)
                .Add("playerToken", playerToken),
            cancellationToken
        );
        return claimed.Token;
    }

    public Task<GameView> Get(string gameId, CancellationToken cancellationToken) =>
        this.Call<GameView>(
            "get_game",
            ImmutableDictionary<string, object?>.Empty.Add("gameId", gameId),
            cancellationToken
        );

    public Task<GameView> Act(
        string gameId,
        GameAction action,
        string seatToken,
        CancellationToken cancellationToken
    ) =>
        this.Call<GameView>(
            "submit_action",
            ImmutableDictionary<string, object?>.Empty
                .Add("gameId", gameId)
                .Add("token", seatToken)
                .Add("kind", action.Kind)
                .Add("arguments", action.Arguments),
            cancellationToken
        );

    /// <summary>Has the bot (or timer) whose decision it is make one, if the game is still at <c>version</c>.</summary>
    public Task<GameView> Advance(string gameId, int version, CancellationToken cancellationToken) =>
        this.Call<GameView>(
            "advance_game",
            ImmutableDictionary<string, object?>.Empty.Add("gameId", gameId).Add("version", version),
            cancellationToken
        );

    private async Task<T> Call<T>(
        string tool,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken
    )
    {
        CallToolResult result = await this._client.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
        string text = string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));
        return result.IsError == true
            ? throw RefusedException.From(text)
            : JsonSerializer.Deserialize<T>(text, Json)
                ?? throw new RefusedException("Empty", $"The tool {tool} sent nothing back.");
    }
}

/// <summary>A request the server refused: which way (<see cref="Refusal"/>: Conflict, TooBusy...), and why.</summary>
internal sealed class RefusedException(string refusal, string message) : Exception(message)
{
    private const string Marker = "Refused (";

    public string Refusal { get; } = refusal;

    /// <summary>The refusal a tool's error tells of, "Refused (Conflict): why"; any other error is "Unknown".</summary>
    public static RefusedException From(string error)
    {
        int at = error.IndexOf(Marker, StringComparison.Ordinal);
        int end = at < 0 ? -1 : error.IndexOf(')', at);
        return end < 0
            ? new RefusedException("Unknown", error)
            : new RefusedException(error[(at + Marker.Length)..end], error);
    }
}
