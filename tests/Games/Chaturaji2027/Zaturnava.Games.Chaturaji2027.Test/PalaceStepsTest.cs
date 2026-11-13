using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Steps along palace lines, given here to Chaturaji 2027's Boat (its own move a leap two squares diagonally), and in
/// one test forward only. Black's palace is a1 to c3. What a piece gains is told apart from its own moves by the rules
/// without steppers.
/// </summary>
public sealed class PalaceStepsTest
{
    private const int BoatDie = 1;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    private static readonly Piece BlackBoat = new(new PieceId(1), Chaturaji2027Pieces.Boat, Black);
    private static readonly Piece BlackRaja = new(new PieceId(2), Chaturaji2027Pieces.Raja, Black);
    private static readonly Piece YellowPawn = new(new PieceId(3), Chaturaji2027Pieces.Pawn, Yellow);

    private static readonly ImmutableList<CellId> BlackPalace =
    [
        .. Enumerable.Range(0, 3).SelectMany(row => Enumerable.Range(0, 3).Select(column => Cell(column, row))),
    ];

    private static readonly Chaturaji2027Rules Plain = RulesWith([], []);

    private readonly Chaturaji2027Rules _rules = RulesWith([Chaturaji2027Pieces.Boat], []);

    [Fact]
    public void AttacksOf_WhenTheThroneIsInTheMiddleAndTheStepperInACorner_ReachesTheThroneDiagonally()
    {
        // Arrange - the throne b2, the Boat on a1
        GameState state = BlackToMove(Cell(1, 1), (Cell(0, 0), BlackBoat));

        // Act
        string gained = Gained(this._rules, state);

        // Assert
        Assert.Equal("b2", gained);
    }

    [Fact]
    public void AttacksOf_WhenTheThroneIsInACorner_StepsAlongEachLineThroughTheCellToTheCellsItPasses()
    {
        // Arrange - the throne a1, the Boat on b2: on the lines from c2 and from b3, a knight's move off the throne,
        // each passing b2 and then b1 or a2; on those from c3 and b2 itself, straight to a1
        GameState state = BlackToMove(Cell(0, 0), (Cell(1, 1), BlackBoat));

        // Act
        string gained = Gained(this._rules, state);

        // Assert
        Assert.Equal("a1,a2,b1,b3,c2,c3", gained);
    }

    [Fact]
    public void AttacksOf_WhenTheStepperGoesForwardOnly_StepsOnlyTowardItsFront()
    {
        // Arrange - the throne b2, the Boat on it
        Chaturaji2027Rules forward = RulesWith([Chaturaji2027Pieces.Boat], [Chaturaji2027Pieces.Boat]);
        GameState state = BlackToMove(Cell(1, 1), (Cell(1, 1), BlackBoat));

        // Act
        string gained = Gained(forward, state);

        // Assert
        Assert.Equal("a3,b3,c3", gained);
    }

    public static TheoryData<string, GameState> NoSteps =>
        new()
        {
            { "outside the palace", BlackToMove(Cell(1, 1), (Cell(4, 4), BlackBoat)) },
            { "no throne set", BlackToMove(null, (Cell(0, 0), BlackBoat)) },
            { "no thrones at all", BlackToMove(Cell(1, 1), (Cell(0, 0), BlackBoat)) with { Thrones = null } },
            { "no palaces", BlackToMove(Cell(1, 1), (Cell(0, 0), BlackBoat)) with { Palaces = null } },
            { "not a stepper", BlackToMove(Cell(1, 1), (Cell(0, 0), BlackRaja)) },
        };

    [Theory]
    [MemberData(nameof(NoSteps), DisableDiscoveryEnumeration = true)]
    public void AttacksOf_WhenThePieceMayNotStepAlongAPalaceLine_GainsNothing(string why, GameState state)
    {
        // Arrange - as given

        // Act
        string gained = Gained(this._rules, state);

        // Assert
        Assert.Equal(string.Empty, gained);
        Assert.NotEmpty(why);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegalActions_WhenTheStepperMayStepToTheThrone_OffersTheStepAndTakesAnEnemyThere(bool isTaking)
    {
        // Arrange - the throne b2, the Boat on a1, a Yellow Pawn on b2 if taking
        GameState start = BlackToMove(Cell(1, 1), (Cell(0, 0), BlackBoat));
        GameState state = isTaking ? start with { Position = start.Position.Place(Cell(1, 1), YellowPawn) } : start;

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Contains(new MoveAction(Black, Cell(0, 0), Cell(1, 1)), actions);
    }

    [Fact]
    public void LegalActions_WhenItsOwnPieceStandsOnTheNextCell_OffersNoStepThere()
    {
        // Arrange - the throne b2, the Boat on a1, Black's Raja on b2
        GameState state = BlackToMove(Cell(1, 1), (Cell(0, 0), BlackBoat), (Cell(1, 1), BlackRaja));

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Cell(0, 0), Cell(1, 1)), actions);
    }

    /// <summary>
    /// The cells Black's pieces attack under <paramref name="rules"/>, but not without steppers, by name.
    /// </summary>
    private static string Gained(Chaturaji2027Rules rules, GameState state)
    {
        ImmutableHashSet<CellId> plain = [.. Plain.AttacksOf(state, Black).Select(attack => attack.Target)];
        return string.Join(
            ',',
            rules.AttacksOf(state, Black)
                .Select(attack => attack.Target)
                .Where(target => !plain.Contains(target))
                .Distinct()
                .Select(Grid.NameOf)
                .Order(StringComparer.Ordinal)
        );
    }

    private static CellId Cell(int column, int row) => Grid.CellAt(column, row);

    private static Chaturaji2027Rules RulesWith(
        ImmutableHashSet<PieceKindId> steppers,
        ImmutableHashSet<PieceKindId> forwardOnly
    ) =>
        new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Chaturaji2027Variant.Standard with
            {
                PalaceSteppers = steppers,
                ForwardPalaceSteppers = forwardOnly,
            }
        );

    /// <summary>
    /// Black to move a Boat, its palace a1 to c3 and its throne <paramref name="throne"/>, if set, with the pieces
    /// placed.
    /// </summary>
    private static GameState BlackToMove(CellId? throne, params (CellId Cell, Piece Piece)[] placements) =>
        new Chaturaji2027Rules().CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [BoatDie],
            Palaces = ImmutableDictionary<SeatId, ImmutableList<CellId>>.Empty.Add(Black, BlackPalace),
            Thrones = throne is { } set ? Plain.Thrones.SetItem(Black, set) : Plain.Thrones.Remove(Black),
            Position = placements.Aggregate(
                Position.Empty,
                (position, placed) => position.Place(placed.Cell, placed.Piece)
            ),
        };
}
