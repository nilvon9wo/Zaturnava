using System.Collections.Immutable;
using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Mcp;

/// <summary>
/// What an AI can do at the game server: the same requests as any other client, each one tool. A
/// refused request becomes the tool's error, with the server's reason, so the AI can correct itself.
/// </summary>
[McpServerToolType]
public sealed class GameTools(IGameServing games)
{
    private readonly IGameServing _games = games;

    [McpServerTool(Name = "catalog", ReadOnly = true)]
    [Description("The rulesets games can be created with (and their seat counts), and the bots that can take a seat.")]
    public Task<CatalogDto> Catalog(CancellationToken cancellationToken) =>
        Explained(() => this._games.Catalog(cancellationToken));

    [McpServerTool(Name = "list_games", ReadOnly = true)]
    [Description("The games on the server, with the seats still open for a player to claim.")]
    public Task<ImmutableList<GameSummaryDto>> ListGames(CancellationToken cancellationToken) =>
        Explained(() => this._games.List(cancellationToken));

    [McpServerTool(Name = "create_game")]
    [Description(
        "Creates a game. For each seat in order, give a bot's id to have it play that seat, or null to leave "
        + "the seat open for a player (you, with claim_seat)."
    )]
    public Task<GameCreatedDto> CreateGame(
        [Description("A ruleset id, from catalog.")] string rulesetId,
        [Description("One entry per seat: a bot id, or null for an open seat.")] string?[] bots,
        CancellationToken cancellationToken
    ) =>
        Explained(() => this._games.Create(new CreateGameRequest(rulesetId, [.. bots]), cancellationToken));

    [McpServerTool(Name = "claim_seat")]
    [Description(
        "Claims an open seat. Returns the token that holds it: keep it, as every action for the seat needs it, "
        + "and the server cannot show it again."
    )]
    public Task<SeatClaimedDto> ClaimSeat(
        string gameId,
        [Description("The seat's number, from 0.")] int seat,
        CancellationToken cancellationToken
    ) =>
        Explained(() => this._games.Claim(gameId, seat, cancellationToken));

    [McpServerTool(Name = "get_game", ReadOnly = true)]
    [Description(
        "A game as it stands: the cells (with where each is drawn), the pieces and their cells, the seats and "
        + "scores, whose decision it is (decidingSeat, null once over), and that seat's legal actions."
    )]
    public Task<GameViewDto> GetGame(string gameId, CancellationToken cancellationToken) =>
        Explained(() => this._games.Get(gameId, cancellationToken));

    [McpServerTool(Name = "submit_action")]
    [Description(
        "Takes an action for the seat your token holds, when it is that seat's decision. Copy the kind and "
        + "arguments of one of get_game's legalActions. Returns the game after it, and after any bots replied."
    )]
    public Task<GameViewDto> SubmitAction(
        string gameId,
        [Description("The token claim_seat returned.")] string token,
        [Description("The action's kind, as in legalActions.")] string kind,
        [Description("The action's arguments, as in legalActions.")] Dictionary<string, string> arguments,
        CancellationToken cancellationToken
    ) =>
        Explained(
            () => this._games.Submit(
                gameId,
                token,
                new ActionDto(kind, arguments.ToImmutableSortedDictionary()),
                cancellationToken
            )
        );

    private static async Task<T> Explained<T>(Func<Task<T>> request)
    {
        try
        {
            return await request();
        }
        catch (GameRequestRefusedException refused)
        {
            throw new McpException($"Refused ({refused.Refusal}): {refused.Message}", refused);
        }
        catch (HttpRequestException unreachable)
        {
            throw new McpException($"The game server could not be reached: {unreachable.Message}", unreachable);
        }
    }
}
