using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Decisions;

public sealed class DecisionRequestTest
{
    private static readonly SeatId Seat = new(0);

    [Fact]
    public void Rules_WhenAsked_AreTheRulesTheGameIsPlayedBy()
    {
        // Arrange
        UnplayableRules rules = new();
        GameState state = new(Position.Empty, [], TeamGraph.FromTeams([]), Seat, 1, [], []);

        // Act
        DecisionRequest request = new(rules, state, Seat, [new PassAction(Seat)]);

        // Assert
        Assert.Same(rules, request.Rules);
    }
}

/// <summary>Rules that are only ever handed on, never applied.</summary>
file sealed class UnplayableRules : IRulesApplying
{
    public string Id => throw new NotSupportedException();

    public string Name => throw new NotSupportedException();

    public ICellNavigating Topology => throw new NotSupportedException();

    public ImmutableDictionary<SeatId, CellId> Thrones => throw new NotSupportedException();

    public GameState CreateInitialState() => throw new NotSupportedException();

    public ImmutableList<GameAction> LegalActions([Required] GameState state) => throw new NotSupportedException();

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        throw new NotSupportedException();

    public bool MayOffer([Required] GameState state) => throw new NotSupportedException();

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayRejoin([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) =>
        throw new NotSupportedException();

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => throw new NotSupportedException();

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
        throw new NotSupportedException();

    public bool IsLeader(PieceKindId kind) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();
}
