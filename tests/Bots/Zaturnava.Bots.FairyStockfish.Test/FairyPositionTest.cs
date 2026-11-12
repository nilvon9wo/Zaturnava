using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Bots.FairyStockfish.Test;

public sealed class FairyPositionTest
{
    private static readonly ImmutableList<PieceKind> Kinds =
    [
        new(GridRules.King, [MovementRule.Step(DirectionSets.Any)]),
        new(new PieceKindId("pawn"), [MovementRule.Step(DirectionSets.Forward).MoveOnly()]),
        new(
            new PieceKindId("odd"),
            [MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue).Shunning([new PieceKindId("pawn")])]
        ),
    ];

    [Theory]
    [InlineData(13, 8)]
    [InlineData(8, 11)]
    public void Of_WhenTheBoardIsTooBig_SeesNothing(int width, int height)
    {
        // Arrange
        GridRules rules = new(Kinds, width, height);

        // Act
        FairyPosition? position = FairyPosition.Of(rules, GridRules.State((0, 0, "king", 0)), new SeatId(0));

        // Assert
        Assert.Null(position);
    }

    [Fact]
    public void Of_WhenTheBoardIsNotARectangle_SeesNothing()
    {
        // Arrange - an 8 by 8 grid, short of its last cell
        ShapedTopology board = new([.. Enumerable.Range(0, 63).Select(at => new CellId(at))]);
        GridRules rules = new(Kinds, topology: board);

        // Act
        FairyPosition? position = FairyPosition.Of(rules, GridRules.State((0, 0, "king", 0)), new SeatId(0));

        // Assert
        Assert.Null(position);
    }

    [Fact]
    public void Of_WhenAPieceCannotBeDefined_SeesNothing()
    {
        // Arrange
        GridRules rules = new(Kinds);

        // Act
        FairyPosition? position = FairyPosition.Of(
            rules,
            GridRules.State((0, 0, "king", 0), (5, 5, "odd", 1)),
            new SeatId(0)
        );

        // Assert
        Assert.Null(position);
    }

    [Fact]
    public void Of_WhenThereAreTooManyKinds_SeesNothing()
    {
        // Arrange - Rooks going 2 to 27 squares: 26 kinds
        ImmutableList<PieceKind> rooks =
        [
            .. Enumerable.Range(2, FairyPosition.MostKinds + 1).Select(range => new PieceKind(
                new PieceKindId($"rook{range}"),
                [new MovementRule(DirectionSets.Orthogonal, 1, range, false, true, true)]
            )),
        ];
        GridRules rules = new(rooks);

        // Act
        FairyPosition? position = FairyPosition.Of(
            rules,
            GridRules.State([.. rooks.Select((rook, at) => (at % 8, at / 8, rook.Id.Value, 0))]),
            new SeatId(0)
        );

        // Assert
        Assert.Null(position);
    }

    [Fact]
    public void Of_WhenItCanSeeTheGame_DefinesEachWayOfMovingAndSetsThePiecesOutWhiteToMove()
    {
        // Arrange - Black's Pawn faces south, so is turned round to step forward as White's do
        GridRules rules = new(Kinds);

        // Act
        FairyPosition? position = FairyPosition.Of(
            rules,
            GridRules.State(
                (4, 0, "king", 0),
                (4, 1, "pawn", 0),
                (7, 1, "pawn", 2),
                (0, 3, "pawn", 2),
                (4, 7, "king", 1),
                (3, 6, "pawn", 1)
            ),
            new SeatId(2)
        );

        // Assert
        Assert.NotNull(position);
        Assert.Equal(
            (
                "4a3/3b4/8/8/B7/8/4B2B/4A3 w - - 0 1",
                $"[{position.Name}]\nmaxFile = 8\nmaxRank = 8\n"
                    + "pawn = -\nknight = -\nbishop = -\nrook = -\nqueen = -\nking = -\n"
                    + "castling = false\ndoubleStep = false\nstalemateValue = draw\nextinctionValue = loss\n"
                    + "extinctionPieceTypes = a\ncustomPiece1 = a:WF\ncustomPiece2 = b:mfW\n",
                26
            ),
            (position.Fen, position.Definition, position.Name.Length)
        );
        Assert.StartsWith("zaturnava-", position.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void SquareOf_WhenTheCellIsOnTheTenthRank_NamesItsFileAndRank()
    {
        // Arrange
        CellLayout cell = new SquareGridTopology(12, 10).LayoutOf(new CellId(119));

        // Act
        string square = FairyPosition.SquareOf(cell);

        // Assert
        Assert.Equal("l10", square);
    }
}
