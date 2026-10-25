using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class ArgusBotTest
{
    private const int Tries = 20;
    private static readonly SeatId Mine = LineRules.Seat;
    private static readonly SeatId Theirs = LineRules.Enemy;
    private static readonly SeatId Ally = new(2);
    private static readonly GameAction Pass = new PassAction(Mine);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenTakingWouldLoseMoreThanItGains_DoesNotTake()
    {
        // Arrange - the Pawn on 2 is guarded by the Pawn on 3, which would take the Horse back
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(0, 2)],
            (0, LineRules.Horse, Mine),
            (2, LineRules.Pawn, Theirs),
            (3, LineRules.Pawn, Theirs)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([Pass], chosen);
    }

    [Fact]
    public async Task Decide_WhenTakingGainsMoreThanItLoses_Takes()
    {
        // Arrange - a Horse for a Pawn, rather than a retreat
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(1, 0), LineRules.Move(1, 2)],
            (1, LineRules.Pawn, Mine),
            (2, LineRules.Horse, Theirs),
            (3, LineRules.Pawn, Theirs)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(1, 2)], chosen);
    }

    [Fact]
    public async Task Decide_WhenAPieceIsAttacked_MovesItAway()
    {
        // Arrange
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(0, 1), LineRules.Move(4, 3)],
            (0, LineRules.Pawn, Mine),
            (4, LineRules.Horse, Mine),
            (5, LineRules.Pawn, Theirs)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(4, 3)], chosen);
    }

    [Fact]
    public async Task Decide_WhenAPieceGuardsAnAttackedOne_KeepsItThere()
    {
        // Arrange - the Pawn on 3 makes taking the Horse on 4 cost the Horse that takes it
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(3, 2)],
            (3, LineRules.Pawn, Mine),
            (4, LineRules.Horse, Mine),
            (5, LineRules.Horse, Theirs)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([Pass], chosen);
    }

    [Fact]
    public async Task Decide_WhenAnEnemyRajaCouldTakeItsEmptyThrone_BlocksIt()
    {
        // Arrange
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(5, 6), LineRules.Move(5, 0)],
            (1, LineRules.Raja, Theirs),
            (5, LineRules.Pawn, Mine)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(5, 0)], chosen);
    }

    [Fact]
    public async Task Decide_WhenOnlyAnEnemyPawnCouldReachItsEmptyThrone_LeavesItEmpty()
    {
        // Arrange - a Pawn on the throne would only be taken
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(5, 0)],
            (1, LineRules.Pawn, Theirs),
            (5, LineRules.Pawn, Mine)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([Pass], chosen);
    }

    [Fact]
    public async Task Decide_WhenActionsAreWorthTheSame_PicksAmongThoseThatDoSomething()
    {
        // Arrange
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(3, 2), LineRules.Move(3, 4)],
            (3, LineRules.Pawn, Mine)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(3, 2), LineRules.Move(3, 4)], chosen);
    }

    [Theory]
    [InlineData(new[] { 20 }, new[] { 10 }, "accept")]
    [InlineData(new[] { 21 }, new[] { 11 }, "decline")]
    public async Task Decide_WhenOfferedARansom_AnswersAsSolomonDoes(int[] given, int[] asked, string answer)
    {
        // Arrange - worth 3 for 1; 1 for 3
        DecisionRequest offered = Ransoms.Offered([.. given], [.. asked]);

        // Act
        GameAction action = await new ArgusBot(new Random(1)).Decide(offered, this._cancellation);

        // Assert
        Assert.Equal(answer, action.Describe().Kind);
    }

    [Fact]
    public async Task Decide_WhenItsTurnIsOver_GivesItsAllysPieceBack()
    {
        // Arrange
        DecisionRequest request = Ransoms.Window();

        // Act
        GameAction action = await new ArgusBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new OfferAction(Ransoms.Black, Ransoms.Green, [new PieceId(30)], []), action);
    }

    [Fact]
    public async Task Decide_WhenNothingFairIsLeftToOfferAndItHasNoThrone_IsDone()
    {
        // Arrange
        DecisionRequest request = Ransoms.WindowWithout(30, 20, 21);

        // Act
        GameAction action = await new ArgusBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Ransoms.Done, action);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        DecisionRequest request = Ransoms.Window();
        ArgusBot bot = new(new Random(1));

        // Act
        Task<GameAction> deciding = bot.Decide(request, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => deciding);
    }

    [Fact]
    public async Task Decide_WhenAMoveWouldBlockAnAllysAttack_MovesElsewhere()
    {
        // Arrange - the ally's Boat on 7 has the enemy Raja on 3 in its line, which the Horse would cut on 5
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(0, 1), LineRules.Move(0, 5)],
            (0, LineRules.Horse, Mine),
            (3, LineRules.Raja, Theirs),
            (7, LineRules.Boat, Ally)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(0, 1)], chosen);
    }

    [Fact]
    public async Task Decide_WhenAPieceBlocksAnAllysAttack_MovesItOutOfTheWay()
    {
        // Arrange - the Horse on 5 cuts the ally's Boat on 7 off from the enemy Raja on 3
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(5, 6), LineRules.Move(5, 1)],
            (5, LineRules.Horse, Mine),
            (3, LineRules.Raja, Theirs),
            (7, LineRules.Boat, Ally)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(5, 1)], chosen);
    }

    [Fact]
    public async Task Decide_WhenItCouldTakeWhatItsAllyThreatens_TakesItItself()
    {
        // Arrange - the enemy Raja on 3 is in the line of the ally's Boat on 7, and next to the Horse
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(2, 1), LineRules.Move(2, 3)],
            (2, LineRules.Horse, Mine),
            (3, LineRules.Raja, Theirs),
            (7, LineRules.Boat, Ally)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(2, 3)], chosen);
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithmCalledArgus()
    {
        // Arrange
        ArgusBot bot = new(new Random(1));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((ArgusBot.BotId, "Argus", false), (profile.Id, profile.Name, profile.UsesAi));
    }

    /// <summary>Every action the bot chooses, over many tries with different seeds.</summary>
    private static async Task<ImmutableHashSet<GameAction>> Choices(DecisionRequest request)
    {
        GameAction[] chosen = await Task.WhenAll(
            Enumerable.Range(0, Tries)
                .Select(seed => new ArgusBot(new Random(seed)).Decide(request, CancellationToken.None))
        );
        return [.. chosen];
    }
}
