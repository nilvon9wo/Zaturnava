using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class PalaceSquaresTest
{
    [Theory]
    [InlineData(3, 3, "1 1")]
    [InlineData(4, 3, "1 1,2 1")]
    [InlineData(4, 2, "1 1,2 1")]
    [InlineData(3, 2, "1 1")]
    [InlineData(2, 2, "")]
    [InlineData(4, 1, "")]
    [InlineData(0, 0, "")]
    public void EdgeCentres_WhenEverySquareOutsideIsFree_IsAnyRoomInsideElseAlongTheEdgeMostlyInside(
        int width,
        int depth,
        string expected
    )
    {
        // Arrange
        ImmutableList<ArmySquare> kingdom = Kingdom(width, depth);

        // Act
        ImmutableList<ArmySquare> centres = PalaceSquares.EdgeCentres(kingdom, _ => true);

        // Assert
        Assert.Equal(expected, Text(centres));
    }

    [Fact]
    public void EdgeCentres_WhenASquareOutsideIsNotFree_StandsNoPalaceOnIt()
    {
        // Arrange - a1 to d2, a3 another army's
        ImmutableList<ArmySquare> kingdom = Kingdom(4, 2);

        // Act
        ImmutableList<ArmySquare> centres =
            PalaceSquares.EdgeCentres(kingdom, square => square != new ArmySquare(0, 2));

        // Assert
        Assert.Equal("2 1", Text(centres));
    }

    [Fact]
    public void EdgeCentres_WhenTooLittleOfThePalaceWouldBeInTheKingdom_IsNone()
    {
        // Arrange - a1, b2 and c1: three squares of the nine about b2
        ImmutableList<ArmySquare> kingdom = [new(0, 0), new(1, 1), new(2, 0)];

        // Act
        ImmutableList<ArmySquare> centres = PalaceSquares.EdgeCentres(kingdom, _ => true);

        // Assert
        Assert.Empty(centres);
    }

    [Theory]
    [InlineData(4, 2, 8, 8, "1 1,2 1")]
    [InlineData(4, 2, 7, 7, "1 1,2 1")]
    [InlineData(6, 2, 8, 8, "")]
    [InlineData(3, 2, 7, 5, "")]
    [InlineData(3, 2, 8, 6, "1 1")]
    public void EdgeCentres_OnABoard_StandsEachWhereNoOtherArmysPalaceCouldShareASquare(
        int width,
        int depth,
        int across,
        int up,
        string expected
    )
    {
        // Arrange - the user's words of 2026-11-19: "when palaces overflow kingdoms, it should be impossible for
        // palaces to overlap"
        ImmutableList<ArmySquare> kingdom = Kingdom(width, depth);

        // Act
        ImmutableList<ArmySquare> centres = PalaceSquares.EdgeCentres(kingdom, across, up);

        // Assert - every seat's palace about any of them, against every other seat's, shares no square
        ImmutableHashSet<(int, int)> PlacedAbout(ArmySquare centre, int seat)
        {
            ImmutableList<ArmyPiece> palace =
                [.. PalaceSquares.Window(centre).Select(square => new ArmyPiece(Square, square.Column, square.Row))];
            ImmutableList<ImmutableList<ArmyPiece>> armies =
                [.. Enumerable.Range(0, Pinwheel.SeatCount).Select(each => each == seat ? palace : [])];
            return [.. Pinwheel.EachOwn(across, up, armies).Select(placed => (placed.Column, placed.Row))];
        }

        ImmutableList<ImmutableHashSet<(int, int)>> Placed(int seat) =>
            [.. centres.Select(centre => PlacedAbout(centre, seat))];
        ImmutableList<ImmutableList<ImmutableHashSet<(int, int)>>> seats =
            [.. Enumerable.Range(0, Pinwheel.SeatCount).Select(Placed)];
        Assert.Equal(expected, Text(centres));
        Assert.All(
            Enumerable.Range(0, Pinwheel.SeatCount).SelectMany(seat =>
                Enumerable.Range(seat + 1, Pinwheel.SeatCount - seat - 1).Select(other => (seat, other))
            ),
            pair => Assert.DoesNotContain(
                seats[pair.seat],
                palace => seats[pair.other].Any(otherPalace => otherPalace.Overlaps(palace))
            )
        );
    }

    private static readonly Engine.Pieces.PieceKindId Square = new("square");

    private static ImmutableList<ArmySquare> Kingdom(int width, int depth) =>
    [
        .. Enumerable.Range(0, depth).SelectMany(row =>
            Enumerable.Range(0, width).Select(column => new ArmySquare(column, row))
        ),
    ];

    private static string Text(IEnumerable<ArmySquare> squares) =>
        string.Join(",", squares.Select(square => $"{square.Column} {square.Row}"));
}
