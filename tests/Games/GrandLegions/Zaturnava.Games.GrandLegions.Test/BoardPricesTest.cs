using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class BoardPricesTest
{
    [Fact]
    public void On_WhenTheBoardIsEightByEight_IsTheSheetsValuesButTheLances()
    {
        // Arrange - the medium game's board, 8 × 8, its kingdoms' front row the sheet's row 2; the sheet counts the
        // Lance's promotion from row 1, 5 rows (premium 12 / 5, 2), the board from its front row, 4 (12 / 4, 3)
        GrandBoard board = QuickTerrain.BoardOf(QuickGame.Medium, 0);

        // Act
        ImmutableDictionary<PieceKindId, int> prices = BoardPrices.On(board);

        // Assert
        Assert.Equal(
            GrandLegionsPieces.Catalogue.ToImmutableDictionary(piece => piece.Id, piece => piece.Value)
                .SetItem(GrandLegionsPieces.Lance, 57),
            prices
        );
    }

    [Theory]
    [InlineData("grand-legions-small", 10, 70, 12, 1)]
    [InlineData("grand-legions-medium", 14, 70, 9, 4)]
    [InlineData("grand-legions-large", 22, 70, 7, 8)]
    public void On_WhenTheBoardIsLargerOrSmaller_PricesReachAndPromotionByItsDimensions(
        string game,
        int rook,
        int king,
        int pawn,
        int giraffe
    )
    {
        // Arrange - a Rook reaches further on a larger board; a King no further; a Pawn has further to promote
        GrandBoard board = QuickTerrain.BoardOf(QuickGame.All.Single(quick => quick.Id == game), 0);

        // Act
        ImmutableDictionary<PieceKindId, int> prices = BoardPrices.On(board);

        // Assert
        Assert.Equal(
            (rook, king, pawn, giraffe),
            (
                prices[GrandLegionsPieces.Rook],
                prices[GrandLegionsPieces.WesternKing],
                prices[GrandLegionsPieces.WesternPawn],
                prices[GrandLegionsPieces.Giraffe]
            )
        );
    }

    [Fact]
    public void On_WhenAPieceReachesNothingFromTheCentre_StillCostsAPoint()
    {
        // Arrange - the Giraffe, 4 on 8 × 8, leaps 4 + 1: from the centre of 6 × 6, off the board every way
        GrandBoard board = QuickTerrain.BoardOf(QuickGame.Small, 0);

        // Act
        int price = BoardPrices.On(board)[GrandLegionsPieces.Giraffe];

        // Assert
        Assert.Equal(1, price);
    }

    [Fact]
    public void On_WhenTheBoardIsNotSquare_PricesPromotionAsEverySeatFacesSinceBothWaysCame()
    {
        // Arrange - the Western Pawn's Queen 6 rows up a board 9 high, kingdoms 3 deep, but 9 rows across it, 12 wide
        GrandBoard wide = new(12, 9, 4, 3);
        GrandBoard tall = new(9, 12, 4, 3);
        int before = BoardPrices.BothWaysEdition - 1;
        PieceKindId pawn = GrandLegionsPieces.WesternPawn;

        // Act
        bool isSameSince = BoardPrices.On(wide)[pawn] == BoardPrices.On(tall)[pawn];
        bool wasSameBefore = BoardPrices.On(wide, before)[pawn] == BoardPrices.On(tall, before)[pawn];

        // Assert
        Assert.Equal((true, false), (isSameSince, wasSameBefore));
    }

    [Fact]
    public void On_WhenTheBoardHasRivers_PricesAsWithout()
    {
        // Arrange
        GrandBoard board = QuickTerrain.BoardOf(QuickGame.Large, 0);

        // Act
        ImmutableDictionary<PieceKindId, int> prices = BoardPrices.On(board with { Rivers = Rivers.Both });

        // Assert
        Assert.Equal(BoardPrices.On(board), prices);
    }

    [Fact]
    public void Catalogue_WhenGivenABoard_PricesEachPieceOnIt()
    {
        // Arrange
        GrandBoard board = QuickTerrain.BoardOf(QuickGame.Large, 0);

        // Act
        ImmutableList<GrandPiece> catalogue = BoardPrices.Catalogue(board);

        // Assert
        Assert.Equal(
            BoardPrices.On(board),
            catalogue.ToImmutableDictionary(piece => piece.Id, piece => piece.Value)
        );
    }
}
