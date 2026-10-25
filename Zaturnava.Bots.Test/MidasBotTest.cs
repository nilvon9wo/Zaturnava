using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class MidasBotTest
{
    private const int Tries = 20;
    private static readonly SeatId Seat = ScoringRules.Seat;
    private static readonly GameAction Pass = new PassAction(Seat);
    private static readonly GameAction Roll = new RollAction(Seat);
    private static readonly GameAction Small = Move(1);
    private static readonly GameAction Big = Move(2);
    private static readonly GameAction AlsoBig = Move(3);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenOneActionScoresTheMost_AlwaysTakesIt()
    {
        // Arrange
        ImmutableDictionary<GameAction, int> points =
            ImmutableDictionary<GameAction, int>.Empty.Add(Pass, 0).Add(Small, 1).Add(Big, 5).Add(Roll, 0);

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(points);

        // Assert
        Assert.Equal([Big], chosen);
    }

    [Fact]
    public async Task Decide_WhenActionsTieForTheMost_TakesOnlyThose()
    {
        // Arrange
        ImmutableDictionary<GameAction, int> points =
            ImmutableDictionary<GameAction, int>.Empty.Add(Small, 1).Add(Big, 5).Add(AlsoBig, 5);

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(points);

        // Assert
        Assert.Subset(ImmutableHashSet.Create(Big, AlsoBig), chosen);
    }

    [Fact]
    public async Task Decide_WhenNothingScores_NeverPasses()
    {
        // Arrange
        ImmutableDictionary<GameAction, int> points =
            ImmutableDictionary<GameAction, int>.Empty.Add(Pass, 0).Add(Roll, 0).Add(Small, 0);

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(points);

        // Assert
        Assert.Subset(ImmutableHashSet.Create(Roll, Small), chosen);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(20)]
    public async Task Decide_WhenAPawnMayBePromoted_PromotesItIntoItsTeamsPieceNotAnEnemys(int teams)
    {
        // Arrange - its Pawn 40 on the edge; Green's Raja 30 is its ally's, Yellow's Horse 11 an enemy's
        DecisionRequest request = Ransoms.Promoting(teams);

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([new PromoteAction(Seat, new CellId(7), new PieceId(teams))], chosen);
    }

    [Fact]
    public async Task Decide_WhenItsPieceMayEscape_LetsItEscape()
    {
        // Arrange - Yellow holds its Horse 20
        DecisionRequest request = Ransoms.Escaping();

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(request);

        // Assert
        Assert.Equal([new EscapeAction(Seat, new PieceId(20), new CellId(7))], chosen);
    }

    [Fact]
    public async Task Decide_WhenPassingIsAllThereIs_Passes()
    {
        // Arrange
        ImmutableDictionary<GameAction, int> points = ImmutableDictionary<GameAction, int>.Empty.Add(Pass, 0);

        // Act
        ImmutableHashSet<GameAction> chosen = await Choices(points);

        // Assert
        Assert.Equal([Pass], chosen);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        DecisionRequest request = Request(ImmutableDictionary<GameAction, int>.Empty.Add(Pass, 0));
        MidasBot bot = new(new Random(1));

        // Act
        Task<GameAction> deciding = bot.Decide(request, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => deciding);
    }

    [Theory]
    [InlineData(20, 10, "accept")]
    [InlineData(20, 11, "accept")]
    [InlineData(21, 30, "decline")]
    public async Task Decide_WhenOfferedARansom_AcceptsIfItGetsBackAtLeastTheWorthItGives(
        int given,
        int asked,
        string answer
    )
    {
        // Arrange
        DecisionRequest offered = Ransoms.Offered([given], [asked]);

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(offered, this._cancellation);

        // Assert
        Assert.Equal(answer, action.Describe().Kind);
    }

    [Theory]
    [InlineData(false, "decline")]
    [InlineData(true, "accept")]
    public async Task Decide_WhenOfferedAProfitByAnEnemyTeam_AcceptsOnlyIfThatTeamHasAPieceLeft(
        bool hasAPiece,
        string answer
    )
    {
        // Arrange - its Horse back for a Pawn: worth 2 more, but it would bring back a team with nothing
        (string, SeatId)[] pieces = hasAPiece
            ? [("pawn", Seat), ("pawn", Ransoms.Yellow)]
            : [("pawn", Seat)];
        DecisionRequest offered = Ransoms.On(Ransoms.Offered([20], [10]), pieces);

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(offered, this._cancellation);

        // Assert
        Assert.Equal(answer, action.Describe().Kind);
    }

    [Fact]
    public async Task Decide_WhenItsTurnIsOver_OffersTheTradeThatGainsTheMost()
    {
        // Arrange - its Pawn for its own Horse back gains 2
        DecisionRequest request = Ransoms.Window();

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new OfferAction(Seat, Ransoms.Yellow, [new PieceId(10)], [new PieceId(20)]), action);
    }

    [Fact]
    public async Task Decide_WhenNoTradeGainsAnything_IsDone()
    {
        // Arrange - without its own Horse 20 to ask back, every trade loses or breaks even
        DecisionRequest request = Ransoms.WindowWithout(20);

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Ransoms.Done, action);
    }

    [Fact]
    public async Task Decide_WhenItHasOfferedThisTurn_IsDone()
    {
        // Arrange
        DecisionRequest request = Ransoms.Window(Ransoms.Offer(Ransoms.Yellow, [10], [20]));

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Ransoms.Done, action);
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithmCalledMidas()
    {
        // Arrange
        MidasBot bot = new(new Random(1));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((MidasBot.BotId, "Midas", false), (profile.Id, profile.Name, profile.UsesAi));
    }

    private static MoveAction Move(int to) => new(Seat, new CellId(0), new CellId(to));

    private static DecisionRequest Request(ImmutableDictionary<GameAction, int> points)
    {
        ScoringRules rules = new(points);
        return new DecisionRequest(rules, ScoringRules.State(), Seat, rules.LegalActions(ScoringRules.State()));
    }

    /// <summary>Everything Midas chose, over many tries with different dice.</summary>
    private static Task<ImmutableHashSet<GameAction>> Choices(ImmutableDictionary<GameAction, int> points) =>
        Choices(Request(points));

    private static async Task<ImmutableHashSet<GameAction>> Choices(DecisionRequest request)
    {
        GameAction[] chosen = await Task.WhenAll(
            Enumerable.Range(0, Tries)
                .Select(seed => new MidasBot(new Random(seed)).Decide(request, CancellationToken.None))
        );
        return [.. chosen];
    }
}
