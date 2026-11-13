using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class PalaceConfinementTest
{
    /// <summary>Seat 0's palace, cells 0 to 8; seat 1's, cells 20 to 28.</summary>
    private static readonly ImmutableDictionary<SeatId, ImmutableList<CellId>> Palaces =
        ImmutableDictionary<SeatId, ImmutableList<CellId>>.Empty
            .Add(new SeatId(0), [.. Enumerable.Range(0, 9).Select(cell => new CellId(cell))])
            .Add(new SeatId(1), [.. Enumerable.Range(20, 9).Select(cell => new CellId(cell))]);

    [Theory]
    [InlineData("janggi-guard", 4, 5, true)]
    [InlineData("janggi-guard", 4, 10, false)]
    [InlineData("xiangqi-advisor", 24, 25, true)]
    [InlineData("xiangqi-advisor", 24, 10, false)]
    [InlineData("janggi-general", 8, 9, false)]
    [InlineData("janggi-guard", 10, 11, true)]
    [InlineData("janggi-guard", 10, 4, true)]
    [InlineData("western-king", 4, 10, true)]
    public void MayMove_WhenAPieceMovesFromWhereItStands_KeepsAConfinedOneInThePalaceItIsIn(
        string kind,
        int from,
        int to,
        bool mayMove
    )
    {
        // Arrange - a confined piece in a palace stays in it; one outside every palace moves freely, into one too
        GameState state = Game(Palaces);
        Piece piece = new(new PieceId(1), new PieceKindId(kind), new SeatId(0));

        // Act
        bool isAllowed = PalaceConfinement.MayMove(state, new Move(piece, new CellId(from), new CellId(to), null));

        // Assert
        Assert.Equal(mayMove, isAllowed);
    }

    [Theory]
    [InlineData("xiangqi-general", "western-king", true)]
    [InlineData("xiangqi-general", "queen", false)]
    [InlineData("janggi-general", "western-king", false)]
    public void MayMove_WhenAGeneralLeavesItsPalaceOntoAPiece_LetsOnlyTheXiangqiGeneralFlyOntoALeader(
        string general,
        string target,
        bool mayMove
    )
    {
        // Arrange - the piece stands on cell 40, outside every palace
        Piece taken = new(new PieceId(2), new PieceKindId(target), new SeatId(1));
        GameState state = Game(Palaces) with { Position = Position.Empty.Place(new CellId(40), taken) };
        Piece piece = new(new PieceId(1), new PieceKindId(general), new SeatId(0));

        // Act
        bool isAllowed = PalaceConfinement.MayMove(state, new Move(piece, new CellId(4), new CellId(40), null));

        // Assert
        Assert.Equal(mayMove, isAllowed);
    }

    [Fact]
    public void MayMove_WhenTheGameHasNoPalaces_LetsAConfinedPieceGoAnywhere()
    {
        // Arrange
        GameState state = Game(null);
        Piece guard = new(new PieceId(1), GrandLegionsPieces.JanggiGuard, new SeatId(0));

        // Act
        bool isAllowed = PalaceConfinement.MayMove(state, new Move(guard, new CellId(4), new CellId(40), null));

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void Confined_WhenAsked_IsEveryPieceOnlyItsPalaceHolds()
    {
        // Arrange - nothing beyond the catalogue

        // Act
        ImmutableHashSet<PieceKindId> confined = PalaceConfinement.Confined;

        // Assert - the Janggi Soldier only gains a power in a palace
        Assert.Equal(
            "janggi-general|janggi-guard|xiangqi-advisor|xiangqi-general",
            string.Join('|', confined.Select(kind => kind.Value).Order(StringComparer.Ordinal))
        );
    }

    private static GameState Game(ImmutableDictionary<SeatId, ImmutableList<CellId>>? palaces) =>
        new(Position.Empty, [], TeamGraph.FromTeams([]), null, 0, [], []) { Palaces = palaces };
}
