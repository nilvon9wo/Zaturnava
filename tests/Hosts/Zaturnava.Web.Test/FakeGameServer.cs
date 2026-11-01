using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

/// <summary>
/// A game server the pages can call: it answers with <see cref="View"/> (a two-by-two board, seat 0
/// to move), remembers what it was asked, and fails the next request with <see cref="Failure"/>.
/// </summary>
internal sealed class FakeGameServer : IGameServing
{
    public const string GameId = "game-0001";
    public const string Token = "secret";

    public static readonly ActionDto Pass = Action("pass");
    public static readonly ActionDto Forward = Action("move", (ActionArguments.From, 0), (ActionArguments.To, 1));
    public static readonly ActionDto Leap = Action("move", (ActionArguments.From, 0), (ActionArguments.To, 3));

    public static readonly GameViewDto SeatZeroToMove = new(
        GameId,
        "chaturaji-2027",
        "Chaturaji 2027",
        1,
        1,
        0,
        null,
        [.. Enumerable.Range(0, 4).Select(Cell)],
        [new PieceDto(0, "raja", "Raja", 0, 0)],
        [
            new SeatDto(0, "Black", 0, 0, "human", null, []),
            new SeatDto(1, "Yellow", 1, 5, "bot", "Random", [new PrisonerDto(7, "boat", "Boat", 0)]),
        ],
        [Pass, Forward, Leap],
        [],
        [],
        null,
        false,
        null,
        null,
        null,
        false
    );

    public GameViewDto View { get; set; } = SeatZeroToMove;

    private static readonly ImmutableList<string> SeatNames = ["Black", "Yellow", "Green", "Red"];

    public ImmutableList<GameSummaryDto> Games { get; set; } =
    [
        new(
            GameId,
            "chaturaji-2027",
            1,
            [1, 3],
            ["Ada", GameSummaryDto.Open, "random", GameSummaryDto.Open],
            12
        )
        {
            SeatNames = SeatNames,
        },
    ];

    public Exception? Failure { get; set; }

    public ConcurrentQueue<string> Calls { get; } = new();

    public CreateGameRequest? Created { get; private set; }

    /// <summary>The rulesets the catalog offers after Chaturaji 2027: none, unless a test sets some.</summary>
    public ImmutableList<RulesetDto> MoreRulesets { get; set; } = [];

    /// <summary>The scenarios the catalog offers: none, unless a test sets some.</summary>
    public ImmutableList<ScenarioDto> Scenarios { get; set; } = [];

    public (string GameId, string Token, ActionDto Action)? Submitted { get; private set; }

    /// <summary>The player token the last seat was claimed with; null, if a guest's.</summary>
    public string? ClaimedAs { get; private set; }

    /// <summary>The nickname the last seat was claimed by; null, if none was given.</summary>
    public string? ClaimedBy { get; private set; }

    public Task<CatalogDto> Catalog(CancellationToken cancellationToken) =>
        this.Answer(
            "catalog",
            new CatalogDto(
                [
                    new RulesetDto("chaturaji-2027", "Chaturaji 2027", 4)
                    {
                        SeatNames = SeatNames,
                        SeatTeams = [0, 1, 0, 1],
                    },
                    .. this.MoreRulesets,
                ],
                [
                    new BotDto("random", "Random", "Any move.", UsesAi: false) { Ratings = Rated(1000) },
                    new BotDto("claude", "Claude", "Asks Claude.", UsesAi: true, IsPaid: true),
                    new BotDto("oracle", "Oracle", "Asks a model.", UsesAi: true) { Ratings = Rated(1400) },
                    new BotDto("midas", "Midas", "Greedy.", UsesAi: false) { Ratings = Rated(1100) },
                    new BotDto("solomon", "Solomon", "Fair.", UsesAi: false) { Ratings = Rated(1250) },
                ]
            )
            {
                Scenarios = this.Scenarios,
            }
        );

    /// <summary>A rating at Chaturaji 2027, the only ruleset this server offers.</summary>
    private static ImmutableDictionary<string, int> Rated(int rating) =>
        ImmutableDictionary<string, int>.Empty.Add("chaturaji-2027", rating);

    public Task<ImmutableList<GameSummaryDto>> List(CancellationToken cancellationToken) =>
        this.Answer("list", this.Games);

    public Task<GameCreatedDto> Create([Required] CreateGameRequest request, CancellationToken cancellationToken)
    {
        this.Created = request;
        return this.Answer("create", new GameCreatedDto(GameId));
    }

    public Task<GameViewDto> Get([Required] string gameId, CancellationToken cancellationToken) =>
        this.Answer("get " + gameId, this.View);

    public Task<GameHistoryDto> History([Required] string gameId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The page asks for no history.");

    public Task<ImmutableList<GameHistoryDto>> Histories(CancellationToken cancellationToken) =>
        throw new NotSupportedException("The page asks for no histories.");

    public Task<GameViewDto> Advance([Required] string gameId, int version, CancellationToken cancellationToken) =>
        this.Answer($"advance {gameId} {version.ToString(CultureInfo.InvariantCulture)}", this.View);

    public Task<SeatClaimedDto> Claim(
        [Required] string gameId,
        int seat,
        string? playerToken,
        ClaimSeatRequest? claim,
        CancellationToken cancellationToken
    )
    {
        this.ClaimedAs = playerToken;
        this.ClaimedBy = claim?.Nickname;
        return this.Answer($"claim {gameId} {seat.ToString(CultureInfo.InvariantCulture)}", new SeatClaimedDto(Token));
    }

    public Task<GameViewDto> FillOpenSeats(
        [Required] string gameId,
        [Required] string token,
        CancellationToken cancellationToken
    ) =>
        this.Answer($"fill {gameId} {token}", this.View);

    /// <summary>Each seat said anew to be held, in order: "{game} {seat} {token} {player token} {nickname}".</summary>
    public ConcurrentQueue<string> Identified { get; } = new();

    public Task Identify(
        [Required] string gameId,
        int seat,
        string? playerToken,
        [Required] IdentifySeatRequest request,
        CancellationToken cancellationToken
    )
    {
        string held = $"{gameId} {seat.ToString(CultureInfo.InvariantCulture)} {request.Token}";
        this.Identified.Enqueue($"{held} {playerToken} {request.Nickname}");
        return this.Answer("identify", true);
    }

    public Task<GameViewDto> Submit(
        [Required] string gameId,
        [Required] string token,
        [Required] ActionDto action,
        CancellationToken cancellationToken
    )
    {
        this.Submitted = (gameId, token, action);
        return this.Answer("submit", this.View);
    }

    /// <summary>What was said at the table: what a test sets, and what the page says, as seat 0.</summary>
    public ImmutableList<ChatMessageDto> Said { get; set; } = [];

    /// <summary>
    /// The talk's requests, apart from <see cref="Calls"/>, as a table makes them besides its game's; each
    /// "chat {after}" (with " watching" for no token) or "say {channel} {text}".
    /// </summary>
    public ConcurrentQueue<string> Talk { get; } = new();

    /// <summary>Fails the talk's next request, apart from <see cref="Failure"/>.</summary>
    public Exception? TalkFailure { get; set; }

    public Task<ImmutableList<ChatMessageDto>> Chat(
        [Required] string gameId,
        string? token,
        int after,
        CancellationToken cancellationToken
    ) =>
        this.Talked<ImmutableList<ChatMessageDto>>(
            $"chat {after.ToString(CultureInfo.InvariantCulture)}{(token is null ? " watching" : string.Empty)}",
            () => [.. this.Said.Where(message => message.Number > after)]
        );

    public Task<ChatMessageDto> Say(
        [Required] string gameId,
        [Required] string token,
        [Required] SayRequest request,
        CancellationToken cancellationToken
    ) =>
        this.Talked(
            $"say {request.Channel} {request.Text}",
            () =>
            {
                ChatMessageDto said =
                    new(this.Said.Count + 1, 0, request.Channel, request.Text, DateTimeOffset.UnixEpoch);
                this.Said = this.Said.Add(said);
                return said;
            }
        );

    /// <summary>The services offered to sign in with: none, unless a test sets some.</summary>
    public ImmutableList<string> Providers { get; set; } = [];

    /// <summary>Who each player token stands for: no one, unless a test says.</summary>
    public ImmutableDictionary<string, PlayerDto> Players { get; set; } = [];

    /// <summary>Fails signing in's next request, apart from <see cref="Failure"/>.</summary>
    public Exception? SignInFailure { get; set; }

    public Task<ImmutableList<string>> SignInProviders(CancellationToken cancellationToken) =>
        this.SignedIn(() => this.Providers);

    public Task<PlayerDto?> Me([Required] string token, CancellationToken cancellationToken) =>
        this.SignedIn(() => this.Players.GetValueOrDefault(token));

    public Task<PlayerDto?> Rename(
        [Required] string token,
        [Required] string name,
        CancellationToken cancellationToken
    ) =>
        this.SignedIn(() => this.Changed(token, player => player with { Name = name }));

    public Task<string?> LinkCode([Required] string token, CancellationToken cancellationToken) =>
        this.SignedIn(() => this.Players.ContainsKey(token) ? "code-" + token : null);

    public Task<PlayerDto?> Unlink(
        [Required] string token,
        [Required] string provider,
        CancellationToken cancellationToken
    ) =>
        this.SignedIn(() =>
            this.Changed(token, player => player with { Services = player.Services?.Remove(provider) })
        );

    public Task<PlayerDto?> SetLinks(
        [Required] string token,
        [Required] ImmutableList<string> links,
        CancellationToken cancellationToken
    ) =>
        this.SignedIn(() => this.Changed(token, player => player with { Links = links }));

    public Task<string?> AiToken([Required] string token, CancellationToken cancellationToken) =>
        this.SignedIn(() => this.Players.ContainsKey(token) ? "ai-" + token : null);

    /// <summary>The profiles anyone may see, by player id: none, unless a test sets some.</summary>
    public ImmutableDictionary<string, ProfileDto> Profiles { get; set; } = [];

    public Task<ProfileDto?> Profile([Required] string playerId, CancellationToken cancellationToken) =>
        this.SignedIn(() => this.Profiles.GetValueOrDefault(playerId));

    /// <summary>Each player token signed out or deleted, in order: "out {token}", "delete {token}".</summary>
    public ConcurrentQueue<string> LetGo { get; } = new();

    public Task SignOut([Required] string token, CancellationToken cancellationToken) =>
        this.SignedIn(() =>
        {
            this.LetGo.Enqueue("out " + token);
            this.Players = this.Players.Remove(token);
            return true;
        });

    public Task<bool> DeleteMe([Required] string token, CancellationToken cancellationToken) =>
        this.SignedIn(() =>
        {
            this.LetGo.Enqueue("delete " + token);
            bool isKnown = this.Players.ContainsKey(token);
            this.Players = this.Players.Remove(token);
            return isKnown;
        });

    /// <summary>The player the token stands for, changed, and kept so; none, if it stands for no one.</summary>
    private PlayerDto? Changed(string token, Func<PlayerDto, PlayerDto> change)
    {
        if (this.Players.GetValueOrDefault(token) is not { } player)
        {
            return null;
        }

        PlayerDto changed = change(player);
        this.Players = this.Players.SetItem(token, changed);
        return changed;
    }

    private Task<T> SignedIn<T>(Func<T> answer)
    {
        Exception? failure = this.SignInFailure;
        this.SignInFailure = null;
        return failure is null ? Task.FromResult(answer()) : Task.FromException<T>(failure);
    }

    private Task<T> Talked<T>(string call, Func<T> answer)
    {
        this.Talk.Enqueue(call);
        Exception? failure = this.TalkFailure;
        this.TalkFailure = null;
        return failure is null ? Task.FromResult(answer()) : Task.FromException<T>(failure);
    }

    private static ActionDto Action(string kind, params (string Name, int Cell)[] cells) =>
        new(
            kind,
            cells.Select(cell => (cell.Name, Value: cell.Cell.ToString(CultureInfo.InvariantCulture)))
                .Append((Name: ActionArguments.Actor, Value: "0"))
                .ToImmutableSortedDictionary(argument => argument.Name, argument => argument.Value)
        );

    private Task<T> Answer<T>(string call, T answer)
    {
        this.Calls.Enqueue(call);
        Exception? failure = this.Failure;
        this.Failure = null;
        return failure is null ? Task.FromResult(answer) : Task.FromException<T>(failure);
    }

    /// <summary>Cell <paramref name="id"/> of a two-by-two board; cell 1 is seat 1's throne.</summary>
    private static CellDto Cell(int id) =>
        new(id, id % 2, Math.DivRem(id, 2).Quotient, "square", id == 1 ? 1 : null, $"c{id}");
}
