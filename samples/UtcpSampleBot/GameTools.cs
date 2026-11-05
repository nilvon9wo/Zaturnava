using System.Collections.Immutable;

namespace UtcpSampleBot;

/// <summary>
/// The server's UTCP tools, as a bot uses them: create a game, claim a seat, read the game, act, and let the server's
/// own bots move. Every tool is in docs/bots/utcp.md; each is one route of the HTTP API, called directly, as the
/// manual says.
/// </summary>
internal sealed class GameTools(UtcpClient utcp)
{
    private readonly UtcpClient _utcp = utcp;

    public Task<Catalog> Catalog(CancellationToken cancellationToken) =>
        this._utcp.Call<Catalog>("catalog", ImmutableDictionary<string, object?>.Empty, cancellationToken);

    public async Task<string> Create(string rulesetId, IEnumerable<string?> bots, CancellationToken cancellationToken)
    {
        GameCreated created = await this._utcp.Call<GameCreated>(
            "create_game",
            ImmutableDictionary<string, object?>.Empty.Add("body", new CreateGame(rulesetId, [.. bots])),
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
        SeatClaimed claimed = await this._utcp.Call<SeatClaimed>(
            "claim_seat",
            ImmutableDictionary<string, object?>.Empty
                .Add("gameId", gameId)
                .Add("seat", seat)
                .Add("body", new ClaimSeat(nickname, IsAi: true))
                .Add("Authorization", playerToken is null ? null : $"Bearer {playerToken}"),
            cancellationToken
        );
        return claimed.Token;
    }

    public Task<GameView> Get(string gameId, CancellationToken cancellationToken) =>
        this._utcp.Call<GameView>(
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
        this._utcp.Call<GameView>(
            "submit_action",
            ImmutableDictionary<string, object?>.Empty
                .Add("gameId", gameId)
                .Add("body", action)
                .Add("Authorization", $"Bearer {seatToken}"),
            cancellationToken
        );

    /// <summary>Has the bot (or timer) whose decision it is make one, if the game is still at <c>version</c>.</summary>
    public Task<GameView> Advance(string gameId, int version, CancellationToken cancellationToken) =>
        this._utcp.Call<GameView>(
            "advance_game",
            ImmutableDictionary<string, object?>.Empty.Add("gameId", gameId).Add("body", new Advance(version)),
            cancellationToken
        );
}
