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
            [GameSummaryDto.Person, GameSummaryDto.Open, "random", GameSummaryDto.Open],
            12
        )
        {
            SeatNames = SeatNames,
        },
    ];

    public Exception? Failure { get; set; }

    public ConcurrentQueue<string> Calls { get; } = new();

    public CreateGameRequest? Created { get; private set; }

    /// <summary>The scenarios the catalog offers: none, unless a test sets some.</summary>
    public ImmutableList<ScenarioDto> Scenarios { get; set; } = [];

    public (string GameId, string Token, ActionDto Action)? Submitted { get; private set; }

    public Task<CatalogDto> Catalog(CancellationToken cancellationToken) =>
        this.Answer(
            "catalog",
            new CatalogDto(
                [new RulesetDto("chaturaji-2027", "Chaturaji 2027", 4) { SeatNames = SeatNames }],
                [
                    new BotDto("random", "Random", "Any move.", UsesAi: false),
                    new BotDto("claude", "Claude", "Asks Claude.", UsesAi: true, IsPaid: true),
                    new BotDto("oracle", "Oracle", "Asks a model.", UsesAi: true),
                    new BotDto("midas", "Midas", "Greedy.", UsesAi: false),
                    new BotDto("solomon", "Solomon", "Fair.", UsesAi: false),
                ]
            )
            {
                Scenarios = this.Scenarios,
            }
        );

    public Task<ImmutableList<GameSummaryDto>> List(CancellationToken cancellationToken) =>
        this.Answer("list", this.Games);

    public Task<GameCreatedDto> Create([Required] CreateGameRequest request, CancellationToken cancellationToken)
    {
        this.Created = request;
        return this.Answer("create", new GameCreatedDto(GameId));
    }

    public Task<GameViewDto> Get([Required] string gameId, CancellationToken cancellationToken) =>
        this.Answer("get " + gameId, this.View);

    public Task<GameViewDto> Advance([Required] string gameId, int version, CancellationToken cancellationToken) =>
        this.Answer($"advance {gameId} {version.ToString(CultureInfo.InvariantCulture)}", this.View);

    public Task<SeatClaimedDto> Claim([Required] string gameId, int seat, CancellationToken cancellationToken) =>
        this.Answer($"claim {gameId} {seat.ToString(CultureInfo.InvariantCulture)}", new SeatClaimedDto(Token));

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
