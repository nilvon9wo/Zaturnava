using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class RandomBotTest
{
    private const int Seed = 7;
    private static readonly SeatId Seat = ScoringRules.Seat;
    private static readonly ScoringRules Rules = new([]);

    [Fact]
    public async Task Decide_WhenGivenLegalActions_ReturnsOneOfThem()
    {
        // Arrange
        ImmutableList<GameAction> legal = [new PassAction(Seat), new PassAction(new SeatId(1))];
        DecisionRequest request = new(Rules, ScoringRules.State(), Seat, legal);
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(action, legal);
    }

    [Fact]
    public async Task Decide_WhenItMayHandItsTurnToAnAlly_KeepsIt()
    {
        // Arrange
        DecisionRequest request =
            new(Rules, ScoringRules.State(), Seat, [new HandOverAction(Seat, new SeatId(2)), new PassAction(Seat)]);
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new PassAction(Seat), action);
    }

    [Fact]
    public async Task Decide_WhenItMayTakeAPieceOfItsOwnTeam_LeavesIt()
    {
        // Arrange - its own Pawn on cell 1, which moving there takes; cell 2 empty
        GameState state = ScoringRules.State() with
        {
            Position = Position.Empty.Place(new CellId(1), new Piece(new PieceId(1), new PieceKindId("pawn"), Seat)),
            Teams = TeamGraph.FromTeams([[Seat]]),
        };
        MoveAction away = new(Seat, new CellId(0), new CellId(2));
        MoveAction taking = new(Seat, new CellId(0), new CellId(1));
        ScoringRules rules = new([])
        {
            Results = ImmutableDictionary<GameAction, GameState>.Empty
                .Add(taking, state with { Position = Position.Empty }),
        };
        DecisionRequest request = new(rules, state, Seat, [taking, away]);
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(away, action);
    }

    [Fact]
    public async Task Decide_WhenItsOnlyMoveOntoItsOwnTeamSwapsWithThePieceThere_PlaysIt()
    {
        // Arrange - its own Pawn on cell 1, which moving there swaps back to cell 0, leaving it on the board
        Piece pawn = new(new PieceId(1), new PieceKindId("pawn"), Seat);
        GameState state = ScoringRules.State() with
        {
            Position = Position.Empty.Place(new CellId(1), pawn),
            Teams = TeamGraph.FromTeams([[Seat]]),
        };
        MoveAction swap = new(Seat, new CellId(0), new CellId(1));
        ScoringRules rules = new([])
        {
            Results = ImmutableDictionary<GameAction, GameState>.Empty
                .Add(swap, state with { Position = Position.Empty.Place(new CellId(0), pawn) }),
        };
        DecisionRequest request = new(rules, state, Seat, [swap]);

        // Act
        GameAction action =
            await new RandomBot(new Random(Seed)).Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(swap, action);
    }

    [Theory]
    [InlineData(2, true, true)]
    [InlineData(1, true, false)]
    [InlineData(2, false, false)]
    [InlineData(2, null, false)]
    public async Task Decide_WhenRetiringItsLoneRajaWouldSaveADraw_RetiresAndElsePlaysOn(
        int enemies,
        bool? isDrawn,
        bool retires
    )
    {
        // Arrange - the bot's Raja alone on 0, against so many enemy pieces; retiring ends the game, drawn or lost,
        // or (null) leaves it going on
        SeatId enemy = new(1);
        Position board = Enumerable.Range(0, enemies).Aggregate(
            Position.Empty.Place(new CellId(0), new Piece(new PieceId(0), new PieceKindId("raja"), Seat)),
            (sofar, index) => sofar.Place(
                new CellId(4 + index),
                new Piece(new PieceId(1 + index), new PieceKindId("pawn"), enemy)
            )
        );
        GameState state = ScoringRules.State() with
        {
            Position = board,
            Teams = TeamGraph.FromTeams([[Seat], [enemy]]),
        };
        RetireAction retire = new(Seat);
        GameOutcome? ended = isDrawn is { } drawn
            ? new(drawn ? [] : [enemy], "only-retired-left", "retired")
            : null;
        ScoringRules rules = new(
            ImmutableDictionary<GameAction, int>.Empty.Add(retire, 0).Add(new PassAction(Seat), 0)
        )
        {
            Results = ImmutableDictionary<GameAction, GameState>.Empty.Add(retire, state with { Outcome = ended }),
        };
        DecisionRequest request = new(rules, state, Seat, rules.LegalActions(state));
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(retires, action == retire);
    }

    [Fact]
    public async Task Decide_WhenItMayRetire_PlaysOn()
    {
        // Arrange
        DecisionRequest request =
            new(Rules, ScoringRules.State(), Seat, [new RetireAction(Seat), new PassAction(Seat)]);
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new PassAction(Seat), action);
    }

    [Theory]
    [InlineData(3, 2, true)]
    [InlineData(2, 2, false)]
    [InlineData(1, 2, false)]
    public async Task Decide_WhenAskedToEndTheGame_AgreesOnlyIfItsTeamLeads(int ours, int theirs, bool agrees)
    {
        // Arrange
        DecisionRequest asked = Proposals.Asked(ours, theirs);

        // Act
        GameAction action = await new RandomBot(new Random(Seed)).Decide(asked, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new AnswerEndAction(Proposals.Black, agrees), action);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        DecisionRequest request = new(Rules, ScoringRules.State(), Seat, [new PassAction(Seat)]);
        RandomBot bot = new(new Random(Seed));

        // Act
        Task<GameAction> deciding = bot.Decide(request, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => deciding);
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithm()
    {
        // Arrange
        RandomBot bot = new(new Random(Seed));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((RandomBot.BotId, "Random", false), (profile.Id, profile.Name, profile.UsesAi));
    }
}
