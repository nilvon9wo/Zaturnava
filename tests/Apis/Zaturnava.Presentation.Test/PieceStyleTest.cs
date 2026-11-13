using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class PieceStyleTest
{
    [Theory]
    [InlineData("pawn,boat,raja", true)]
    [InlineData("elephant", true)]
    [InlineData("horse", true)]
    [InlineData("pawn,boat", false)]
    [InlineData("xiangqi-elephant,xiangqi-horse,western-king", false)]
    public void HasChessForms_WhenTheSetChangesAnyOfTheKinds_IsTrue(string kinds, bool expected)
    {
        // Arrange - nothing beyond the kinds

        // Act
        bool hasChessForms = PieceStyle.HasChessForms(kinds.Split(','));

        // Assert
        Assert.Equal(expected, hasChessForms);
    }

    [Fact]
    public void EmblemOf_WhenTheSymbolIsTheElephant_IsTheChaturajiElephant()
    {
        // Act
        string? shape = PieceStyle.EmblemOf("elephant");

        // Assert
        Assert.Equal(PieceStyle.ShapeOf("elephant", PieceSet.Chaturaji), shape);
    }

    [Fact]
    public void EmblemOf_ForEachSymbol_IsAShapeAGlyphOrAGlyphWithWhatItAdds()
    {
        // Arrange - shapes alone; chess glyphs alone; chess glyphs with more drawn round them
        string[] shapes =
        [
            "star", "shield", "fan", "scroll", "tiara", "plumed-cap", "sword", "lance", "camel", "elephant", "giraffe",
            "dots", "catapult", "cannon", "grasshopper", "checker", "viking-helmet", "winged-hat",
        ];
        string[] glyphs = ["king", "queen", "rook", "bishop", "knight", "pawn"];
        string[] both =
        [
            "dragon-king", "dragon-bishop", "dragon-horse", "amazon", "chancellor", "manchu-chariot", "knightrider",
            "zebra", "xiangqi-horse",
        ];

        // Act
        (string? Shape, string? Glyph)[] drawn =
        [
            .. shapes.Concat(glyphs).Concat(both).Select(symbol =>
                (PieceStyle.EmblemOf(symbol), PieceStyle.EmblemGlyphOf(symbol))
            ),
        ];

        // Assert
        Assert.Equal(
            (shapes.Length + glyphs.Length + both.Length, shapes.Length + both.Length, glyphs.Length + both.Length,
                "♜︎"),
            (
                drawn.Distinct().Count(),
                drawn.Count(symbol => symbol.Shape is not null),
                drawn.Count(symbol => symbol.Glyph is not null),
                drawn[shapes.Length + 2].Glyph
            )
        );
    }

    [Fact]
    public void EmblemOf_WhenTheSymbolIsNotKnownHere_IsNone()
    {
        // Act
        (string? shape, string? glyph) = (PieceStyle.EmblemOf("wizard"), PieceStyle.EmblemGlyphOf("wizard"));

        // Assert
        Assert.Equal(((string?)null, (string?)null), (shape, glyph));
    }

    [Fact]
    public void ClassEmblemOf_ForEachClass_IsAShapeOfItsOwnElseNone()
    {
        // Arrange
        string[] classes = ["leader", "infantry", "heavy", "cavalry", "artillery"];

        // Act
        string?[] shapes = [.. classes.Select(PieceStyle.ClassEmblemOf)];

        // Assert
        Assert.Equal(
            (classes.Length, 0, null),
            (
                shapes.OfType<string>().Distinct(StringComparer.Ordinal).Count(),
                shapes.Count(shape => shape == PieceStyle.EmblemOf("star") || shape == PieceStyle.EmblemOf("cannon")),
                PieceStyle.ClassEmblemOf("wizard")
            )
        );
    }

    [Theory]
    [InlineData("", "translate(0 0.24) scale(0.28)")]
    [InlineData("Sh", "translate(-0.1 0.24) scale(0.28)")]
    public void ClassPlacementBeside_WhenThePieceHasLettersOrNot_IsAtTheirLeftElseInTheMiddle(
        string letters,
        string expected
    )
    {
        // Act
        string placement = PieceStyle.ClassPlacementBeside(letters);

        // Assert
        Assert.Equal(expected, placement);
    }

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
