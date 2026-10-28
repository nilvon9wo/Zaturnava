using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class PieceStyleTest
{
    [Theory]
    [InlineData("raja", "\u265A\uFE0E")]
    [InlineData("elephant", "\u265C\uFE0E")]
    [InlineData("horse", "\u265E\uFE0E")]
    [InlineData("pawn", "\u265F\uFE0E")]
    public void GlyphOf_WhenTheKindMovesLikeAChessPieceInTheChessSet_IsThatPiece(string kind, string expected)
    {
        // Arrange
        PieceDto piece = new(0, kind, "Any", 0, 0);

        // Act
        string glyph = PieceStyle.GlyphOf(piece, PieceSet.Chess);

        // Assert
        Assert.Equal(expected, glyph);
    }

    [Theory]
    [InlineData(PieceSet.Chaturaji)]
    [InlineData(PieceSet.Chess)]
    public void GlyphOf_WhenTheKindIsUnknown_IsTheFirstLetterOfItsName(PieceSet set)
    {
        // Arrange
        PieceDto piece = new(0, "wazir", "Wazir", 0, 0);

        // Act
        string glyph = PieceStyle.GlyphOf(piece, set);

        // Assert
        Assert.Equal("W", glyph);
    }

    [Theory]
    [InlineData("raja", PieceSet.Chaturaji, "M-0.25 0.18")]
    [InlineData("elephant", PieceSet.Chaturaji, "M-0.14 0A")]
    [InlineData("horse", PieceSet.Chaturaji, "M0.18 0.26")]
    [InlineData("boat", PieceSet.Chaturaji, "M-0.27 0.1")]
    [InlineData("boat", PieceSet.Chess, "M-0.27 0.1")]
    public void ShapeOf_WhenTheKindIsDrawnAsAShape_IsItsPath(string kind, PieceSet set, string expected)
    {
        // Arrange - nothing beyond the style

        // Act
        string? shape = PieceStyle.ShapeOf(kind, set);

        // Assert
        Assert.StartsWith(expected, shape, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("horse", PieceSet.Chess)]
    [InlineData("pawn", PieceSet.Chaturaji)]
    public void ShapeOf_WhenTheKindIsDrawnAsAGlyph_IsNull(string kind, PieceSet set)
    {
        // Arrange - nothing beyond the style

        // Act
        string? shape = PieceStyle.ShapeOf(kind, set);

        // Assert
        Assert.Null(shape);
    }

    [Theory]
    [InlineData(PieceSet.Chess, "Black took Red's Rook; only Kings and Knights left, and a Horseshoe.")]
    [InlineData(PieceSet.Chaturaji, "Black took Red's Elephant; only Rajas and Horses left, and a Horseshoe.")]
    public void Named_WhenTheTextNamesPieces_NamesThemAsTheSetDoes(PieceSet set, string expected)
    {
        // Arrange
        const string text = "Black took Red's Elephant; only Rajas and Horses left, and a Horseshoe.";

        // Act
        string named = PieceStyle.Named(text, set);

        // Assert
        Assert.Equal(expected, named);
    }
}
