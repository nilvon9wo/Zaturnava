using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>The pinwheel on a board 6 cells across and 4 up, as Grand Legions may set one.</summary>
public sealed class PinwheelTest
{
    private const int Width = 6;
    private const int Height = 4;

    private static readonly PieceKindId Square = new("square");

    [Theory]
    [InlineData(0, "0,0 5,3")]
    [InlineData(1, "0,3 5,0")]
    [InlineData(2, "5,3 0,0")]
    [InlineData(3, "5,0 0,3")]
    public void At_WhenTheBoardIsNotSquare_PutsEachSeatsCornerOnItsOwnAndItsFarCornerAcrossTheBoard(
        int seat,
        string expected
    )
    {
        // Arrange - the seat's own corner, and its far corner, as it sees the board
        (int across, int up) = Pinwheel.FrameOf(Width, Height, seat);

        // Act
        Placement near = Pinwheel.At(Width, Height, seat, new ArmyPiece(Square, 0, 0));
        Placement far = Pinwheel.At(Width, Height, seat, new ArmyPiece(Square, across - 1, up - 1));

        // Assert
        Assert.Equal(expected, $"{near.Column},{near.Row} {far.Column},{far.Row}");
    }

    [Fact]
    public void EachOwn_WhenTheBoardIsSquare_IsAtOnASquareBoard()
    {
        // Arrange
        ArmyPiece piece = new(Square, 1, 0);

        // Act
        Placement placed = Pinwheel.EachOwn(8, [[], [piece]])[0];

        // Assert
        Assert.Equal(Pinwheel.At(8, 8, 1, piece), placed);
    }
}
