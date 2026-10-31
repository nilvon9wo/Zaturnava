using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

/// <summary>
/// The catalogue, checked against pieces.md: each piece's reach from d4 on an empty 8 × 8 board, its own player
/// facing up the board, as the values count it; and the moves that set its pieces apart.
/// </summary>
public sealed class GrandLegionsPiecesTest
{
    private static readonly SquareGridTopology Board = new(8, 8);
    private static readonly CellId D4 = Board.CellAt(3, 3);
    private static readonly Seat MySeat = new(new SeatId(0), new Direction(0, 1));
    private static readonly SeatId TheirSeat = new(1);
    private static readonly MoveGenerator Generator = new(Board, GrandLegionsPieces.Kinds);

    public static TheoryData<string, int> Reaches => new()
    {
        { "dragon-king", 18 },
        { "fairy-general", 16 },
        { "western-king", 8 },
        { "xiangqi-general", 4 },
        { "janggi-general", 4 },
        { "gold-general", 6 },
        { "silver-general", 5 },
        { "shogi-pawn", 1 },
        { "western-pawn", 3 },
        { "janggi-guard", 4 },
        { "noble", 5 },
        { "janggi-soldier", 3 },
        { "minister", 4 },
        { "courtier", 4 },
        { "makruk-pawn", 3 },
        { "xiangqi-soldier", 1 },
        { "xiangqi-advisor", 4 },
        { "amazon", 35 },
        { "queen", 27 },
        { "chancellor", 22 },
        { "archbishop", 21 },
        { "dragon-bishop", 21 },
        { "dragon-horse", 17 },
        { "lance", 4 },
        { "rook", 14 },
        { "bishop", 13 },
        { "knightrider", 12 },
        { "zebra", 8 },
        { "knight", 8 },
        { "camel", 8 },
        { "xiangqi-horse", 8 },
        { "janggi-elephant", 8 },
        { "giraffe", 4 },
        { "alfil", 4 },
        { "threeleaper", 4 },
        { "xiangqi-elephant", 4 },
        { "war-engine", 4 },
        { "xiangqi-cannon", 14 },
        { "janggi-cannon", 0 },
        { "grasshopper", 0 },
    };

    [Theory]
    [MemberData(nameof(Reaches))]
    public void Reach_WhenAloneOnTheBoard_IsWhatItsValueCounts(string kind, int expected)
    {
        // Arrange
        Position position = Position.Empty.Place(D4, Mine(new PieceKindId(kind)));

        // Act
        int reach = Reach(position);

        // Assert
        Assert.Equal(expected, reach);
    }

    [Theory]
    [InlineData("janggi-cannon", 10)]
    [InlineData("grasshopper", 8)]
    public void Reach_WhenAHopperHasAPieceOnEveryCellNextToIt_IsWhatItsValueCounts(string kind, int expected)
    {
        // Arrange
        Position position = Surrounded(Position.Empty.Place(D4, Mine(new PieceKindId(kind))));

        // Act
        int reach = Reach(position);

        // Assert
        Assert.Equal(expected, reach);
    }

    [Theory]
    [InlineData("xiangqi-horse", 0, 1, 6)]
    [InlineData("janggi-elephant", 1, 0, 6)]
    [InlineData("xiangqi-elephant", 1, 1, 3)]
    [InlineData("knight", 0, 1, 8)]
    public void Reach_WhenAPieceStandsNextToIt_BlocksOnlyTheLameLeapsThatPassIt(
        string kind,
        int columns,
        int rows,
        int expected
    )
    {
        // Arrange
        Position position = Position.Empty
            .Place(D4, Mine(new PieceKindId(kind)))
            .Place(Board.CellAt(3 + columns, 3 + rows), Theirs(GrandLegionsPieces.Rook, 2));

        // Act
        int reach = Reach(position);

        // Assert
        Assert.Equal(expected, reach);
    }

    [Fact]
    public void MovesFrom_WhenAJanggiCannonsOnlyScreenIsACannon_CannotMove()
    {
        // Arrange
        Position position = Position.Empty
            .Place(D4, Mine(GrandLegionsPieces.JanggiCannon))
            .Place(Board.CellAt(3, 4), Theirs(GrandLegionsPieces.XiangqiCannon, 2));

        // Act
        ImmutableList<Move> moves = Generator.MovesFrom(position, D4, MySeat, _ => true);

        // Assert
        Assert.Empty(moves);
    }

    [Fact]
    public void MovesFrom_WhenAXiangqiCannonHasAScreen_TakesOnlyByHoppingIt()
    {
        // Arrange - a screen on d5, and an enemy beyond it on d7
        Position position = Position.Empty
            .Place(D4, Mine(GrandLegionsPieces.XiangqiCannon))
            .Place(Board.CellAt(3, 4), Theirs(GrandLegionsPieces.Rook, 2))
            .Place(Board.CellAt(3, 6), Theirs(GrandLegionsPieces.Rook, 3));

        // Act
        ImmutableList<Move> captures =
            [.. Generator.MovesFrom(position, D4, MySeat, _ => true).Where(move => move.Captured is not null)];

        // Assert
        Assert.Equal("d7", Board.NameOf(Assert.Single(captures).To));
    }

    [Fact]
    public void Catalogue_WhenListed_HasEveryPieceOnceInItsClass()
    {
        // Arrange - nothing beyond the catalogue

        // Act
        ImmutableDictionary<PieceClass, int> counts = GrandLegionsPieces.Catalogue
            .GroupBy(piece => piece.Class)
            .ToImmutableDictionary(group => group.Key, group => group.Count());

        // Assert
        Assert.Equal(
            (40, 7, 10, 9, 11, 3),
            (
                GrandLegionsPieces.ById.Count,
                counts[PieceClass.Leader],
                counts[PieceClass.Infantry],
                counts[PieceClass.Heavy],
                counts[PieceClass.Cavalry],
                counts[PieceClass.Artillery]
            )
        );
    }

    [Fact]
    public void Catalogue_WhenAPiecePromotes_PromotesIntoPiecesOfTheCatalogue()
    {
        // Arrange - nothing beyond the catalogue

        // Act
        ImmutableList<PieceKindId> into = [.. GrandLegionsPieces.Catalogue.SelectMany(piece => piece.PromotesTo)];

        // Assert
        Assert.All(into, kind => Assert.True(GrandLegionsPieces.ById.ContainsKey(kind)));
    }

    [Fact]
    public void Catalogue_WhenItsClassIsMostValuable_ListsTheMostValuableFirst()
    {
        // Arrange - nothing beyond the catalogue

        // Act
        ImmutableList<ImmutableList<int>> values =
        [
            .. GrandLegionsPieces.Catalogue
                .GroupBy(piece => piece.Class)
                .Select(group => group.Select(piece => piece.Value).ToImmutableList()),
        ];

        // Assert
        Assert.All(values, inClass => Assert.Equal(inClass.OrderDescending(), inClass));
    }

    /// <summary>The cells the piece on d4 could move or capture on, each once.</summary>
    private static int Reach(Position position) =>
        Generator.MovesFrom(position, D4, MySeat, _ => true)
            .Select(move => move.To)
            .Concat(Generator.AttacksFrom(position, D4, MySeat).Select(attack => attack.Target))
            .Distinct()
            .Count();

    /// <summary>The position, with an enemy Rook on every cell next to d4.</summary>
    private static Position Surrounded(Position position) =>
        Board.Resolve(DirectionSets.Any, MySeat.Forward)
            .Select((direction, index) => (Cell: Board.Step(D4, direction)!.Value, Id: 10 + index))
            .Aggregate(position, (around, next) => around.Place(next.Cell, Theirs(GrandLegionsPieces.Rook, next.Id)));

    private static Piece Mine(PieceKindId kind) => new(new PieceId(1), kind, MySeat.Id);

    private static Piece Theirs(PieceKindId kind, int id) => new(new PieceId(id), kind, TheirSeat);
}
