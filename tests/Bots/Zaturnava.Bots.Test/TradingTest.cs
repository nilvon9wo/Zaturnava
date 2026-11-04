using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>How a bot weighs the trades it could make, as Sage does.</summary>
public sealed class TradingTest
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenTheRulesSendAGiftHomeAtOnce_WeighsItWithoutAcceptingIt()
    {
        // Arrange - Green's Raja given back to it, gone home as offered, with nothing left to accept
        DecisionRequest request = Ransoms.Window();
        OfferAction gift = new(Ransoms.Black, Ransoms.Green, [new PieceId(30)], []);
        GameState released = request.State with
        {
            Prisoners = request.State.Prisoners.RemoveAll(held => held.Piece.Id == new PieceId(30)),
        };
        ScoringRules rules = new(ImmutableDictionary<GameAction, int>.Empty.Add(Ransoms.Done, 0))
        {
            Results = ImmutableDictionary<GameAction, GameState>.Empty.Add(gift, released),
            Refused = [new AcceptAction(Ransoms.Green)],
        };

        // Act
        GameAction action = await new SageBot(new Random(7)).Decide(request with { Rules = rules }, this._cancellation);

        // Assert
        Assert.Equal(gift, action);
    }
}
