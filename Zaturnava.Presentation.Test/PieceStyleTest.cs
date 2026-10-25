using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class PieceStyleTest
{
    [Theory]
    [InlineData("raja", "♚︎")]
    [InlineData("elephant", "♜︎")]
    [InlineData("horse", "♞︎")]
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

    [Fact]
    public void ShapeOf_WhenTheKindIsTheBoat_IsASailingBoatsPath()
    {
        // Arrange - nothing beyond the style

        // Act
        string? shape = PieceStyle.ShapeOf("boat");

        // Assert
        Assert.StartsWith("M-0.27 0.1", shape, StringComparison.Ordinal);
    }

    [Fact]
    public void ShapeOf_WhenTheKindIsDrawnAsAGlyph_IsNull()
    {
        // Arrange - nothing beyond the style

        // Act
        string? shape = PieceStyle.ShapeOf("horse");

        // Assert
        Assert.Null(shape);
    }
}
