using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class SolomonBotTest
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(new[] { 20 }, new[] { 10 }, "accept")]
    [InlineData(new[] { 20, 21 }, new[] { 30 }, "accept")]
    [InlineData(new[] { 21 }, new[] { 11 }, "decline")]
    public async Task Decide_WhenOfferedARansom_AcceptsIfItIsWithinAPointOfEven(
        int[] given,
        int[] asked,
        string answer
    )
    {
        // Arrange - worth 3 for 1; 4 for 5; 1 for 3
        DecisionRequest offered = Ransoms.Offered([.. given], [.. asked]);

        // Act
        GameAction action = await new SolomonBot(new Random(1)).Decide(offered, this._cancellation);

        // Assert
        Assert.Equal(answer, action.Describe().Kind);
    }

    [Fact]
    public async Task Decide_WhenAskedForAnEnemyLeader_DeclinesEvenWithinAPoint()
    {
        // Arrange - its Horse and Green's Pawn back, worth 4, for Yellow's Raja, worth 5
        DecisionRequest offered = Ransoms.HoldingYellowsRaja(Ransoms.Offered([20, 21], [12]));

        // Act
        GameAction action = await new SolomonBot(new Random(1)).Decide(offered, this._cancellation);

        // Assert
        Assert.Equal("decline", action.Describe().Kind);
    }

    [Theory]
    [InlineData("pawn", "horse", true)]
    [InlineData("horse", "pawn", false)]
    public async Task Decide_WhenAnEvenTradeIsOpen_OffersItOnlyIfItsTeamIsBehindOnTheBoard(
        string ours,
        string theirs,
        bool offers
    )
    {
        // Arrange - no ally's piece to give back; a Horse for a Horse, worth the same to both
        DecisionRequest request =
            Ransoms.On(Ransoms.WindowWithout(30), (ours, Ransoms.Black), (theirs, Ransoms.Yellow));
        OfferAction even = new(Ransoms.Black, Ransoms.Yellow, [new PieceId(11)], [new PieceId(20)]);

        // Act
        GameAction action = await new SolomonBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(offers, action == even);
    }

    [Fact]
    public async Task Decide_WhenItWouldPassItsTurn_GivesItsAllysPieceBackFirst()
    {
        // Arrange - passing, it gets no window to offer after
        DecisionRequest request = Ransoms.Passing();

        // Act
        GameAction action = await new SolomonBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new OfferAction(Ransoms.Black, Ransoms.Green, [new PieceId(30)], []), action);
    }

    [Fact]
    public async Task Decide_WhenItsTurnIsOver_GivesItsAllysPieceBackFirst()
    {
        // Arrange
        DecisionRequest request = Ransoms.Window();

        // Act
        GameAction action = await new SolomonBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new OfferAction(Ransoms.Black, Ransoms.Green, [new PieceId(30)], []), action);
    }

    [Fact]
    public async Task Decide_WhenNoGiftIsLeft_OffersTheMostValuableEvenTrade()
    {
        // Arrange - a Horse for a Horse, rather than a Pawn for a Pawn
        DecisionRequest request = Ransoms.WindowWithout(30);

        // Act
        GameAction action = await new SolomonBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new OfferAction(Ransoms.Black, Ransoms.Yellow, [new PieceId(11)], [new PieceId(20)]), action);
    }

    [Fact]
    public async Task Decide_WhenNothingFairIsLeftToOffer_IsDone()
    {
        // Arrange - no ally's prisoner to give back, and no pair alike in worth
        DecisionRequest request = Ransoms.WindowWithout(30, 20, 21);

        // Act
        GameAction action = await new SolomonBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Ransoms.Done, action);
    }

    [Fact]
    public async Task Decide_WhenItHasOfferedThisTurn_IsDone()
    {
        // Arrange
        DecisionRequest request = Ransoms.Window(Ransoms.Offer(Ransoms.Green, [30], []));

        // Act
        GameAction action = await new SolomonBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Ransoms.Done, action);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        DecisionRequest request = Ransoms.Window();
        SolomonBot bot = new(new Random(1));

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
            (0, LineRules.Horse, LineRules.Seat),
            (3, LineRules.Raja, LineRules.Enemy),
            (7, LineRules.Boat, new SeatId(2))
        );

        // Act
        GameAction[] chosen = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(seed => new SolomonBot(new Random(seed)).Decide(request, this._cancellation))
        );

        // Assert
        Assert.Equal([LineRules.Move(0, 1)], chosen.ToImmutableHashSet());
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
        GameAction action = await new SolomonBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Proposals.Black, agrees), action);
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithmCalledSolomon()
    {
        // Arrange
        SolomonBot bot = new(new Random(1));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((SolomonBot.BotId, "Solomon", false), (profile.Id, profile.Name, profile.UsesAi));
    }
}
