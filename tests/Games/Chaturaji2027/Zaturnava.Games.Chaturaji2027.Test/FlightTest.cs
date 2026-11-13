using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Fleeing the board from the far corner, given here to Chaturaji 2027's Raja (as the Tafl King has it): Black's far
/// corner is h8, Yellow's h1.
/// </summary>
public sealed class FlightTest
{
    private const int RajaDie = 5;
    private const int BoatDie = 1;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);
    private static readonly CellId BlackCorner = Grid.CellAt(7, 7);
    private static readonly CellId YellowCorner = Grid.CellAt(7, 0);
    private static readonly CellId BlackThrone = Grid.CellAt(3, 0);

    /// <summary>Black's Raja, 3, which starts on d1.</summary>
    private static readonly Piece BlackRaja = new(new PieceId(3), Chaturaji2027Pieces.Raja, Black);

    private static readonly Piece YellowRaja = new(new PieceId(11), Chaturaji2027Pieces.Raja, Yellow);
    private static readonly Piece BlackPawn = new(new PieceId(4), Chaturaji2027Pieces.Pawn, Black);

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with { Fleers = [Chaturaji2027Pieces.Raja] }
    );

    [Theory]
    [InlineData("on its corner", true)]
    [InlineData("elsewhere", false)]
    [InlineData("on a die that moves it not", false)]
    [InlineData("at the start of the turn", true)]
    [InlineData("retired", false)]
    [InlineData("an enemy's, on its own corner", false)]
    public void LegalActions_WhenAPieceThatMayFleeStandsSo_OffersItsFlightOnlyFromItsOwnFarCorner(
        string how,
        bool mayFlee
    )
    {
        // Arrange
        (CellId raja, int[] dice) = how switch
        {
            "elsewhere" => (Grid.CellAt(6, 7), [RajaDie]),
            "on a die that moves it not" => (BlackCorner, [BoatDie]),
            "at the start of the turn" => (BlackCorner, []),
            "an enemy's, on its own corner" => (Grid.CellAt(0, 7), [RajaDie]),
            _ => (BlackCorner, (int[])[RajaDie]),
        };
        GameState state = BlackToMove(dice, raja, YellowCorner) with
        {
            Retired = how == "retired" ? [Black] : [],
        };
        ImmutableList<FleeAction> expected = mayFlee ? [new FleeAction(Black, BlackCorner)] : [];

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(expected, actions.OfType<FleeAction>());
    }

    [Fact]
    public void Apply_WhenAPieceFlees_TakesItOffTheBoardForNoOneToHoldUsingItsDie()
    {
        // Arrange
        GameState state = BlackToMove([RajaDie], BlackCorner, Grid.CellAt(0, 7));

        // Act
        Transition transition = this._rules.Apply(state, new FleeAction(Black, BlackCorner));

        // Assert
        Assert.Equal(
            (new PieceFled(BlackRaja, BlackCorner, Black), false, BlackRaja, 0, true, false),
            (
                transition.Events[0],
                transition.State.Position.PieceAt(BlackCorner) is not null,
                Assert.Single(transition.State.Fled),
                transition.State.Prisoners.Count,
                transition.State.DecidingSeat != Black,
                transition.State.Outcome is not null
            )
        );
    }

    [Fact]
    public void Apply_WhenTheLastPieceOnTheBoardFlees_EndsTheGameAsAnyMoveLeavingOneTeamWould()
    {
        // Arrange - Black's Raja is all it has on the board; as a prisoner, a piece that fled holds no ground
        GameState start = BlackToMove([RajaDie], BlackCorner, Grid.CellAt(0, 7));
        GameState state = start with
        {
            Position = new Position(start.Position.Occupants.Remove(Grid.CellAt(3, 3))),
        };

        // Act
        Transition transition = this._rules.Apply(state, new FleeAction(Black, BlackCorner));

        // Assert
        Assert.Equal("only one team has pieces left", transition.State.Outcome?.Reason);
    }

    [Theory]
    [InlineData(RajaDie, RajaDie, true)]
    [InlineData(RajaDie, BoatDie, false)]
    public void LegalActions_WhenAPieceFledAndDoublesAreRolled_LetsItComeBackAsAPrisonerEscapes(
        int first,
        int second,
        bool mayReturn
    )
    {
        // Arrange - Black's Raja fled; Black's Pawn plays on
        GameState state = Fled([first, second]);
        ImmutableList<EscapeAction> expected = mayReturn ? [new EscapeAction(Black, BlackRaja.Id, BlackThrone)] : [];

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(expected, actions.OfType<EscapeAction>());
    }

    [Fact]
    public void Apply_WhenAPieceThatFledComesBack_PutsItHomeHeldByNoOne()
    {
        // Arrange
        GameState state = Fled([RajaDie, RajaDie]);

        // Act
        Transition transition = this._rules.Apply(state, new EscapeAction(Black, BlackRaja.Id, BlackThrone));

        // Assert
        Assert.Equal(
            (new PieceEscaped(BlackRaja, BlackThrone, Black), BlackRaja, 0),
            (
                transition.Events[0],
                transition.State.Position.PieceAt(BlackThrone),
                transition.State.Fled.Count
            )
        );
    }

    [Fact]
    public void GuideOf_WhenTheKindMayFlee_TellsSo()
    {
        // Arrange - nothing beyond the rules

        // Act
        PieceGuide guide = this._rules.GuideOf(Chaturaji2027Pieces.Raja);

        // Assert
        Assert.Contains(
            guide.SpecialPowers,
            power => power.StartsWith("Escape: on its own far corner", StringComparison.Ordinal)
        );
    }

    /// <summary>Black's Raja fled the board; Black's Pawn on d4, Yellow's Raja on a8; Black to move.</summary>
    private static GameState Fled(int[] dice) =>
        BlackToMove(dice, Grid.CellAt(0, 7)) with
        {
            Fled = [BlackRaja],
        };

    /// <summary>
    /// Black to move with <paramref name="dice"/>: its Raja on one cell, its Pawn on d4, and Yellow's Raja on another.
    /// </summary>
    private static GameState BlackToMove(int[] dice, CellId blackRaja, CellId yellowRaja)
    {
        GameState state = BlackToMove(dice, yellowRaja);
        return state with { Position = state.Position.Place(blackRaja, BlackRaja) };
    }

    /// <summary>Black to move with <paramref name="dice"/>: its Pawn on d4, and Yellow's Raja on a cell.</summary>
    private static GameState BlackToMove(int[] dice, CellId yellowRaja) =>
        new Chaturaji2027Rules().CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [.. dice],
            Position = Position.Empty.Place(Grid.CellAt(3, 3), BlackPawn).Place(yellowRaja, YellowRaja),
        };
}
