using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class DealtGameTest
{
    private static readonly SeatId Black = new(0);

    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    /// <summary>Chaturaji 2027's army, but for Black's, whose Raja and Boat have swapped places.</summary>
    private static readonly ImmutableList<Placement> Swapped = Pinwheel.EachOwn(
        Chaturaji2027Setup.Side,
        [
            Army(Chaturaji2027Pieces.Raja, Chaturaji2027Pieces.Boat),
            .. Enumerable.Repeat(Army(Chaturaji2027Pieces.Boat, Chaturaji2027Pieces.Raja), 3),
        ]
    );

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with { Dealer = _ => Swapped }
    );

    [Fact]
    public void Deal_WhenTheVariantDealsNothing_LeavesTheGameAsItIs()
    {
        // Arrange
        Chaturaji2027Rules rules = new();
        GameState initial = rules.CreateInitialState();

        // Act
        GameState dealt = rules.Deal(initial, 4);

        // Assert
        Assert.Equal((false, initial), (rules.IsDealt, dealt));
    }

    [Fact]
    public void Deal_WhenTheVariantDeals_StartsEachPieceWhereDealtAndEachLeaderOnItsThrone()
    {
        // Arrange
        GameState initial = this._rules.CreateInitialState();

        // Act
        GameState dealt = this._rules.Deal(initial, 4);

        // Assert
        Assert.True(this._rules.IsDealt);
        Assert.Equal(Pinwheel.PositionOf(Grid, Swapped).Occupants, dealt.Position.Occupants);
        Assert.Equal(dealt.Position, dealt.Start);
        Assert.Equal(Grid.CellAt(0, 0), this._rules.ThronesOf(dealt)[Black]);
        Assert.Equal(Grid.CellAt(3, 0), this._rules.ThronesOf(initial)[Black]);
    }

    [Fact]
    public void TimerOf_WhenADealtPrisonerGoesHome_SendsItWhereItWasDealt()
    {
        // Arrange - Black's Raja, dealt to the corner, goes home from Yellow
        GameState dealt = this._rules.Deal(this._rules.CreateInitialState(), 4);
        CellId home = Grid.CellAt(0, 0);
        Piece raja = dealt.Position.PieceAt(home)!;
        Prisoner prisoner = new(raja, new SeatId(1));
        GameState state = dealt with
        {
            Position = new Position(dealt.Position.Occupants.Remove(home)),
            Prisoners = [prisoner],
            Returning = new ReturnsDue([prisoner], Black),
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
        };

        // Act
        DecisionTimer? timer = this._rules.TimerOf(state);

        // Assert
        Assert.Equal(new PlaceAction(Black, home), timer?.Default);
    }

    [Fact]
    public void Apply_WhenADealtGameIsPlayedOutByAnyLegalAction_EndsTheGame()
    {
        // Arrange - dice and choices alike at random, the same each run
        GameState state = this._rules.Deal(this._rules.CreateInitialState(), 4);
        Random random = new(2027);

        // Act
        for (int step = 0; step < 20_000 && state.Outcome is null; step++)
        {
            ImmutableList<GameAction> legal = this._rules.LegalActions(state);
            GameAction action = state.AwaitedDice is { } dice
                ? new DiceAction(
                    state.DecidingSeat!.Value,
                    [.. Enumerable.Range(0, dice.Count).Select(_ => random.Next(1, dice.Sides + 1))]
                )
                : legal[random.Next(legal.Count)];
            state = this._rules.Apply(state, action).State;
        }

        // Assert
        Assert.NotNull(state.Outcome);
    }

    private static ImmutableList<ArmyPiece> Army(PieceKindId corner, PieceKindId inner) =>
    [
        new(corner, 0, 0),
        new(Chaturaji2027Pieces.Horse, 1, 0),
        new(Chaturaji2027Pieces.Elephant, 2, 0),
        new(inner, 3, 0),
        .. Enumerable.Range(0, 4).Select(column => new ArmyPiece(Chaturaji2027Pieces.Pawn, column, 1)),
    ];
}
