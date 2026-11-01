using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Rules;

public sealed class RulesApplyingTest
{
    [Fact]
    public void Game_WhenTheRulesetSaysNoOther_IsItsOwnAtNoSize()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        (string Id, string Name, string? Size) game = GameOf(rules);

        // Assert
        Assert.Equal(("solo", "Solo", null), game);
    }

    /// <summary>The game any rules play, as the interface tells it.</summary>
    private static (string Id, string Name, string? Size) GameOf<TRules>(TRules rules)
        where TRules : IRulesApplying =>
        (rules.GameId, rules.GameName, rules.Size);
}

/// <summary>Rules of a game played at one size, asked only what they are called.</summary>
file sealed class OneSizeRules : IRulesApplying
{
    public string Id => "solo";

    public string Name => "Solo";

    public ICellNavigating Topology => throw new NotSupportedException();

    public ImmutableDictionary<SeatId, CellId> Thrones => throw new NotSupportedException();

    public GameState CreateInitialState() => throw new NotSupportedException();

    public ImmutableList<GameAction> LegalActions([Required] GameState state) => throw new NotSupportedException();

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        throw new NotSupportedException();

    public bool MayOffer([Required] GameState state) => throw new NotSupportedException();

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) =>
        throw new NotSupportedException();

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayRejoin([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) =>
        throw new NotSupportedException();

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => throw new NotSupportedException();

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) =>
        throw new NotSupportedException();

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
        throw new NotSupportedException();

    public bool IsLeader(PieceKindId kind) => throw new NotSupportedException();

    public bool IsHeavy(PieceKindId kind) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => throw new NotSupportedException();
}
