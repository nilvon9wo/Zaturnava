using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Mcp.Test;

/// <summary>
/// A game server behind the tools: it answers every request with a fixed reply, remembers the last
/// request that carried data, and fails the next request with <see cref="Failure"/>.
/// </summary>
internal sealed class StubGameServer : IGameServing
{
    public const string GameId = "g1";
    public const string Token = "secret";
    private const int GameHistoryFormat = 1;

    public static readonly GameViewDto View =
        new(
            GameId,
            "chaturaji-2027",
            "Chaturaji 2027",
            3,
            2,
            0,
            null,
            [],
            [],
            [],
            [],
            [],
            [],
            null,
            false,
            null,
            null,
            null,
            false
        );

    public Exception? Failure { get; set; }

    public object? Received { get; private set; }

    public Task<CatalogDto> Catalog(CancellationToken cancellationToken) =>
        this.Answer(
            new CatalogDto(
                [new RulesetDto("chaturaji-2027", "Chaturaji 2027", 4)],
                [new BotDto("random", "Random", "Any move.", UsesAi: false)]
            )
        );

    public Task<ImmutableList<GameSummaryDto>> List(CancellationToken cancellationToken) =>
        this.Answer(
            ImmutableList.Create(
                new GameSummaryDto(GameId, "chaturaji-2027", 1, [0], [GameSummaryDto.Open, "random"], 0)
            )
        );

    public Task<GameCreatedDto> Create([Required] CreateGameRequest request, CancellationToken cancellationToken) =>
        this.Answer(new GameCreatedDto(GameId), request);

    public Task<GameViewDto> Get([Required] string gameId, CancellationToken cancellationToken) =>
        this.Answer(View, gameId);

    public Task<GameHistoryDto> History([Required] string gameId, CancellationToken cancellationToken) =>
        this.Answer(
            new GameHistoryDto(
                GameHistoryFormat,
                GameId,
                "chaturaji-2027",
                [new HistorySeatDto(0, "Black", 0, "random", 3)],
                [],
                new HistoryOutcomeDto([0], "agreement", "every player agreed to end it, so points decide")
            ),
            gameId
        );

    public Task<ImmutableList<GameHistoryDto>> Histories(CancellationToken cancellationToken) =>
        throw new NotSupportedException("The tools offer no histories in bulk.");

    public Task<GameViewDto> Advance([Required] string gameId, int version, CancellationToken cancellationToken) =>
        this.Answer(View, (gameId, version));

    public Task<SeatClaimedDto> Claim([Required] string gameId, int seat, CancellationToken cancellationToken) =>
        this.Answer(new SeatClaimedDto(Token), (gameId, seat));

    public Task<GameViewDto> Submit(
        [Required] string gameId,
        [Required] string token,
        [Required] ActionDto action,
        CancellationToken cancellationToken
    ) =>
        this.Answer(View, (gameId, token, action));

    private Task<T> Answer<T>(T answer, object? received = null)
    {
        this.Received = received ?? this.Received;
        Exception? failure = this.Failure;
        this.Failure = null;
        return failure is null ? Task.FromResult(answer) : Task.FromException<T>(failure);
    }
}
