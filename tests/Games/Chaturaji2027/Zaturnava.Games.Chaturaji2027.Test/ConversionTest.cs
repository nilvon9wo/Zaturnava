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
/// Converting, as the Evangelist does, given here to Chaturaji 2027's Raja: Black moves its Raja from d1 to d2; Yellow
/// and Red are its enemies, Green its ally.
/// </summary>
public sealed class ConversionTest
{
    private const int RajaDie = 5;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);
    private static readonly CellId From = Cell(3, 0);
    private static readonly CellId To = Cell(3, 1);

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with { Converters = [Chaturaji2027Pieces.Raja] }
    );

    [Fact]
    public void Apply_WhenTheConverterBracketsLinesOfOneEnemysPieces_MakesThemItsPlayersAndTakesNone()
    {
        // Arrange - Yellow's Horses on d3 and d4, Black's Pawn on d5; Yellow's Horse on e3, Black's Pawn on f4;
        // Black's Pawn on c2, right next to it
        GameState state = BlackToMove(
            (Cell(3, 2), Horse(10, Yellow)),
            (Cell(3, 3), Horse(11, Yellow)),
            (Cell(3, 4), Pawn(12, Black)),
            (Cell(4, 2), Horse(13, Yellow)),
            (Cell(5, 3), Pawn(14, Black)),
            (Cell(2, 1), Pawn(15, Black))
        ) with
        {
            TurnsSinceCapture = 5,
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, From, To));

        // Assert
        Assert.Equal(
            ("10,11,13", "10,11,13", 0, 0),
            (
                string.Join(
                    ',',
                    transition.Events.OfType<PieceConverted>().Select(converted => converted.Piece.Id.Value).Order()
                ),
                string.Join(
                    ',',
                    transition.State.Position.Occupants.Values
                        .Where(piece => piece.Kind == Chaturaji2027Pieces.Horse && piece.Owner == Black)
                        .Select(piece => piece.Id.Value)
                        .Order()
                ),
                transition.Events.OfType<PieceCaptured>().Count(),
                transition.State.TurnsSinceCapture
            )
        );
    }

    [Fact]
    public void Apply_WhenNoLineEndsInOneOfItsPlayersPiecesUnbroken_ConvertsNothing()
    {
        // Arrange - west: Yellow's Horses on c2, b2 and a2, to the edge; north: Green's Pawn, an ally's, on d3, with
        // Black's on d4; east: Yellow's Horse on e2 and Raja on f2, with Black's Pawn on g2; north-west: Yellow's
        // Horse on c3 and Red's on b4, with Black's Pawn on a5; north-east: Yellow's Horse on e3, with f4 empty
        GameState state = BlackToMove(
            (Cell(2, 1), Horse(10, Yellow)),
            (Cell(1, 1), Horse(11, Yellow)),
            (Cell(0, 1), Horse(12, Yellow)),
            (Cell(3, 2), Pawn(13, Green)),
            (Cell(3, 3), Pawn(14, Black)),
            (Cell(4, 1), Horse(15, Yellow)),
            (Cell(5, 1), new Piece(new PieceId(16), Chaturaji2027Pieces.Raja, Yellow)),
            (Cell(6, 1), Pawn(17, Black)),
            (Cell(2, 2), Horse(18, Yellow)),
            (Cell(1, 3), Horse(19, Red)),
            (Cell(0, 4), Pawn(20, Black)),
            (Cell(4, 2), Horse(21, Yellow))
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, From, To));

        // Assert
        Assert.Empty(transition.Events.OfType<PieceConverted>());
    }

    [Fact]
    public void GuideOf_WhenTheKindConverts_TellsSo()
    {
        // Arrange - nothing beyond the rules

        // Act
        PieceGuide guide = this._rules.GuideOf(Chaturaji2027Pieces.Raja);

        // Assert
        Assert.Contains(guide.SpecialPowers, power => power.StartsWith("Converts", StringComparison.Ordinal));
    }

    private static CellId Cell(int column, int row) => Grid.CellAt(column, row);

    private static Piece Horse(int id, SeatId owner) => new(new PieceId(id), Chaturaji2027Pieces.Horse, owner);

    private static Piece Pawn(int id, SeatId owner) => new(new PieceId(id), Chaturaji2027Pieces.Pawn, owner);

    /// <summary>
    /// Black to move a Raja, its Raja on d1 and the other Rajas far off (Yellow's on h8, Green's on h1, Red's on a8),
    /// with the pieces placed.
    /// </summary>
    private static GameState BlackToMove(params (CellId Cell, Piece Piece)[] placements) =>
        new Chaturaji2027Rules().CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Dice = [RajaDie],
            Position = placements.Aggregate(
                Position.Empty
                    .Place(From, new Piece(new PieceId(1), Chaturaji2027Pieces.Raja, Black))
                    .Place(Cell(7, 7), new Piece(new PieceId(2), Chaturaji2027Pieces.Raja, Yellow))
                    .Place(Cell(7, 0), new Piece(new PieceId(3), Chaturaji2027Pieces.Raja, Green))
                    .Place(Cell(0, 7), new Piece(new PieceId(4), Chaturaji2027Pieces.Raja, Red)),
                (position, placed) => position.Place(placed.Cell, placed.Piece)
            ),
        };
}
