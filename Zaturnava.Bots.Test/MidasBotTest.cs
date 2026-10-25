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
        ImmutableHashSet<GameAction> chosen = await this.Choices(points);

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
        ImmutableHashSet<GameAction> chosen = await this.Choices(points);

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
        ImmutableHashSet<GameAction> chosen = await this.Choices(points);

        // Assert
        Assert.Subset(ImmutableHashSet.Create(Roll, Small), chosen);
    }

    [Fact]
    public async Task Decide_WhenPassingIsAllThereIs_Passes()
    {
        // Arrange
        ImmutableDictionary<GameAction, int> points = ImmutableDictionary<GameAction, int>.Empty.Add(Pass, 0);

        // Act
        ImmutableHashSet<GameAction> chosen = await this.Choices(points);

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
    [InlineData(2, 2, "accept")]
    [InlineData(1, 2, "decline")]
    public async Task Decide_WhenOfferedARansom_AcceptsIfItGetsBackAsManyPiecesAsItGives(
        int given,
        int asked,
        string answer
    )
    {
        // Arrange
        RansomOffer offer = new(
            new SeatId(1),
            Seat,
            [.. Enumerable.Range(10, given).Select(id => new PieceId(id))],
            [.. Enumerable.Range(20, asked).Select(id => new PieceId(id))]
        );
        DecisionRequest request = Request(ImmutableDictionary<GameAction, int>.Empty.Add(Pass, 0));
        DecisionRequest offered = request with { State = request.State with { Offer = offer } };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(offered, this._cancellation);

        // Assert
        Assert.Equal(answer, action.Describe().Kind);
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
    private async Task<ImmutableHashSet<GameAction>> Choices(ImmutableDictionary<GameAction, int> points)
    {
        DecisionRequest request = Request(points);
        GameAction[] chosen = await Task.WhenAll(
            Enumerable.Range(0, Tries)
                .Select(seed => new MidasBot(new Random(seed)).Decide(request, this._cancellation))
        );
        return [.. chosen];
    }
}
