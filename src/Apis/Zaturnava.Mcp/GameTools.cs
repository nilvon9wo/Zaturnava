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
        [Description("A nickname to be shown by at the table, as \"Nickname (guest)\"; at most 20 characters.")]
        string? nickname,
        CancellationToken cancellationToken
    ) =>
        Explained(() => this._games.Claim(gameId, seat, null, nickname, cancellationToken));

    [McpServerTool(Name = "get_game", ReadOnly = true)]
    [Description(
        "A game as it stands: the cells (with where each is drawn), the pieces and their cells, the seats and "
        + "scores, whose decision it is (decidingSeat, null once over), that seat's legal actions, the dice "
        + "it has rolled this turn and not used yet (with what each lets it move), and a log of what has "
        + "happened lately."
    )]
    public Task<GameViewDto> GetGame(string gameId, CancellationToken cancellationToken) =>
        Explained(() => this._games.Get(gameId, cancellationToken));

    [McpServerTool(Name = "game_history", ReadOnly = true)]
    [Description(
        "A game as data to learn from: each seat (who played it, and its score), every action taken in order "
        + "(the dice rolled among them), and how it ended (null while it goes on): the winners, the kind of "
        + "ending, and why."
    )]
    public Task<GameHistoryDto> GameHistory(string gameId, CancellationToken cancellationToken) =>
        Explained(() => this._games.History(gameId, cancellationToken));

    [McpServerTool(Name = "advance_game")]
    [Description(
        "Lets the bot whose turn it is make one decision, if the game is still at the version given (from "
        + "get_game); bots only play when someone advances the game. Returns the game after it."
    )]
    public Task<GameViewDto> AdvanceGame(
        string gameId,
        [Description("The game's version, as get_game gave it.")] int version,
        CancellationToken cancellationToken
    ) =>
        Explained(() => this._games.Advance(gameId, version, cancellationToken));

    [McpServerTool(Name = "submit_action")]
    [Description(
        "Takes an action for the seat your token holds, when it is that seat's decision. Copy the kind and "
        + "arguments of one of get_game's legalActions. Returns the game after it."
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

    [McpServerTool(Name = "read_chat", ReadOnly = true)]
    [Description(
        "What was said at a game's table after message number 'after' (0 for everything): what was said to the "
        + "whole table, and, with your token, what your team said to itself."
    )]
    public Task<ImmutableList<ChatMessageDto>> ReadChat(
        string gameId,
        [Description("The token claim_seat returned, or null to read only what was said to the table.")]
            string? token,
        [Description("The number of the last message already read; 0 for everything.")] int after,
        CancellationToken cancellationToken
    ) =>
        Explained(() => this._games.Chat(gameId, token, after, cancellationToken));

    [McpServerTool(Name = "say")]
    [Description(
        "Says something at a game's table, as the seat your token holds: to the whole table, or to your team "
        + "alone. At most 500 characters."
    )]
    public Task<ChatMessageDto> Say(
        string gameId,
        [Description("The token claim_seat returned.")] string token,
        [Description("Whom to say it to: \"table\" or \"team\".")] string channel,
        [Description("What to say.")] string text,
        CancellationToken cancellationToken
    ) =>
        Explained(() => this._games.Say(gameId, token, new SayRequest(channel, text), cancellationToken));

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
