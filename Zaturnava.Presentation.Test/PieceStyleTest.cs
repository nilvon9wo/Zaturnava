using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class PieceStyleTest
{
    [Theory]
    [InlineData("raja", "♚︎")]
    [InlineData("elephant", "♜︎")]
    [InlineData("horse", "♞︎")]
    [InlineData("boat", "♝︎")]
    [InlineData("pawn", "♟︎")]
    public void GlyphOf_WhenTheKindMovesLikeAChessPiece_IsThatPiece(string kind, string expected)
    {
        // Arrange
        PieceDto piece = new(0, kind, "Any", 0, 0);

        // Act
        string glyph = PieceStyle.GlyphOf(piece);

        // Assert
        Assert.Equal(expected, glyph);
    }

    [Fact]
    public void GlyphOf_WhenTheKindIsUnknown_IsTheFirstLetterOfItsName()
    {
        // Arrange
        PieceDto piece = new(0, "wazir", "Wazir", 0, 0);

        // Act
        string glyph = PieceStyle.GlyphOf(piece);

        // Assert
        Assert.Equal("W", glyph);
    }
}
