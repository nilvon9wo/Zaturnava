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
        // Arrange - its own Pawn on cell 1; cell 2 empty
        GameState state = ScoringRules.State() with
        {
            Position = Position.Empty.Place(new CellId(1), new Piece(new PieceId(1), new PieceKindId("pawn"), Seat)),
            Teams = TeamGraph.FromTeams([[Seat]]),
        };
        MoveAction away = new(Seat, new CellId(0), new CellId(2));
        DecisionRequest request = new(Rules, state, Seat, [new MoveAction(Seat, new CellId(0), new CellId(1)), away]);
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(away, action);
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
