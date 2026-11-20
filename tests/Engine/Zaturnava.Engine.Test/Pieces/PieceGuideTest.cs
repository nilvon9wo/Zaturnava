using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Pieces;

public sealed class PieceGuideTest
{
    [Theory]
    [InlineData("Western", true)]
    [InlineData("Makruk", true)]
    [InlineData("Shogi", false)]
    public void IsOf_WhenAskedOfAFamily_IsOfItsOwnGameAndThoseThatPlayItToo(string family, bool expected)
    {
        // Arrange - the Rook: Western chess's, which Makruk plays too, as its Ruea
        PieceGuide rook = new(new PieceKind(new PieceKindId("rook"), [])) { Origin = "Western", AlsoIn = ["Makruk"] };

        // Act
        bool isOf = rook.IsOf(family);

        // Assert
        Assert.Equal(expected, isOf);
    }
}
