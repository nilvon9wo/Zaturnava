using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class CroesusBotTest
{
    private const int Tries = 20;
    private static readonly SeatId Mine = LineRules.Seat;
    private static readonly SeatId Theirs = LineRules.Enemy;
    private static readonly GameAction Pass = new PassAction(Mine);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenNothingScores_MakesOnlyAMoveLeavingNoPieceNewlyOpen()
    {
        // Arrange - the Horse on 2 stepping to 4 would stand beside the enemy Pawn on 5; the Pawn on 0 may step safely
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(0, 1), LineRules.Move(2, 3), LineRules.Move(2, 4)],
            (0, LineRules.Pawn, Mine),
            (2, LineRules.Horse, Mine),
            (5, LineRules.Pawn, Theirs)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(0, 1), LineRules.Move(2, 3)], chosen.OrderBy(action => action.ToString()));
    }

    [Fact]
    public async Task Decide_WhenEveryMoveLeavesAPieceOpen_MovesAnywayRatherThanPass()
    {
        // Arrange - the Horse's one move takes it beside the enemy Pawn
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(2, 4)],
            (2, LineRules.Horse, Mine),
            (5, LineRules.Pawn, Theirs)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(2, 4)], chosen);
    }

    [Fact]
    public async Task Decide_WhenATakingScores_TakesItThoughItLosesMore()
    {
        // Arrange - the Pawn on 2 is guarded by the Pawn on 3, which would take the Horse back
        DecisionRequest request = LineRules.Request(
            [Pass, LineRules.Move(0, 1), LineRules.Move(0, 2)],
            (0, LineRules.Horse, Mine),
            (2, LineRules.Pawn, Theirs),
            (3, LineRules.Pawn, Theirs)
        );

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([LineRules.Move(0, 2)], chosen);
    }

    [Theory]
    [InlineData(20, 10, "accept")]
    [InlineData(21, 30, "decline")]
    public async Task Decide_WhenOfferedARansom_AcceptsAsMidasDoes(int given, int asked, string answer)
    {
        // Arrange
        DecisionRequest offered = Ransoms.Offered([given], [asked]);

        // Act
        GameAction action = await new CroesusBot(new Random(1)).Decide(offered, this._cancellation);

        // Assert
        Assert.Equal(answer, action.Describe().Kind);
    }

    [Fact]
    public async Task Decide_WhenItsTurnIsOver_OffersTheTradeThatGainsTheMost()
    {
        // Arrange - accepting costs Yellow 2 points a prisoner, so the Horse 11 for its own Horse 20 gains the most
        DecisionRequest request = Ransoms.ScoringRansoms(Ransoms.Window(), -2);

        // Act
        GameAction action = await new CroesusBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new OfferAction(Mine, Ransoms.Yellow, [new PieceId(11)], [new PieceId(20)]), action);
    }

    [Theory]
    [InlineData(3, 2, true)]
    [InlineData(1, 2, false)]
    public async Task Decide_WhenAskedToEndTheGame_AgreesOnlyIfItsTeamLeads(int ours, int theirs, bool agrees)
    {
        // Arrange
        DecisionRequest asked = Proposals.Asked(ours, theirs);

        // Act
        GameAction action = await new CroesusBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Proposals.Black, agrees), action);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        DecisionRequest request = LineRules.Request([Pass]);
        CroesusBot bot = new(new Random(1));

        // Act
        Task<GameAction> deciding = bot.Decide(request, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => deciding);
    }

    [Fact]
    public async Task Seize_WhenNothingGains_SeizesNothing()
    {
        // Arrange
        DecisionRequest request = LineRules.Request([Pass]);

        // Act
        GameAction? seized = await new CroesusBot(new Random(1)).Seize(request, this._cancellation);

        // Assert
        Assert.Null(seized);
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithmCalledCroesus()
    {
        // Arrange
        CroesusBot bot = new(new Random(1));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((CroesusBot.BotId, "Croesus", false), (profile.Id, profile.Name, profile.UsesAi));
    }

    /// <summary>Everything Croesus chose, over many tries with different dice.</summary>
    private static async Task<ImmutableHashSet<GameAction>> Choices(DecisionRequest request)
    {
        GameAction[] chosen = await Task.WhenAll(
            Enumerable.Range(0, Tries)
                .Select(seed => new CroesusBot(new Random(seed)).Decide(request, CancellationToken.None))
        );
        return [.. chosen];
    }
}
