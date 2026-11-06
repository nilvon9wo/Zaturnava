using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class QuickTerrainTest
{
    [Fact]
    public void Dealt_WhenDealtByManySeeds_DealsEveryLayoutAboutAsOftenAsAnother()
    {
        // Arrange
        ImmutableList<int> seeds = [.. Enumerable.Range(0, 8000)];

        // Act
        ImmutableDictionary<int, int> counts = seeds
            .GroupBy(QuickTerrain.Dealt)
            .ToImmutableDictionary(terrain => terrain.Key, terrain => terrain.Count());

        // Assert
        Assert.Equal(Enumerable.Range(0, 8), counts.Keys.Order());
        Assert.All(counts.Values, count => Assert.InRange(count, 850, 1150));
    }

    [Fact]
    public void Dealt_WhenDealtByTheSameSeed_IsTheSame()
    {
        // Arrange - nothing beyond the seed

        // Act
        (int first, int again) = (QuickTerrain.Dealt(2027), QuickTerrain.Dealt(2027));

        // Assert
        Assert.Equal(first, again);
    }

    [Theory]
    [InlineData(0, Rivers.None, RiverCourse.ThroughKingdoms, "No rivers.")]
    [InlineData(
        1,
        Rivers.NorthSouth,
        RiverCourse.ThroughKingdoms,
        "A river runs north to south, through the kingdoms."
    )]
    [InlineData(6, Rivers.EastWest, RiverCourse.AroundKingdoms, "A river runs east to west, around the kingdoms.")]
    [InlineData(
        3,
        Rivers.Both,
        RiverCourse.ThroughKingdoms,
        "Rivers run north to south and east to west, through the kingdoms."
    )]
    public void BoardOfAndDescribed_WhenGivenATerrain_AreItsRiversAndCourse(
        int terrain,
        Rivers rivers,
        RiverCourse course,
        string described
    )
    {
        // Arrange
        QuickGame game = QuickGame.Medium;

        // Act
        GrandBoard board = QuickTerrain.BoardOf(game, terrain);

        // Assert
        Assert.Equal(
            (game.Side, game.KingdomWidth, rivers, course, described),
            (board.Width, board.KingdomWidth, board.Rivers, board.Course, QuickTerrain.Described(terrain))
        );
    }

    [Fact]
    public void LinesOf_WhenTheGameHasANorthSouthRiver_DrawItDownTheMiddle()
    {
        // Arrange
        QuickGame game = QuickGame.Small;

        // Act
        ImmutableList<BoardLine> lines = QuickTerrain.LinesOf(game, 1);

        // Assert
        Assert.All(lines, line => Assert.Equal((2.5, 2.5), (line.FromX, line.ToX)));
    }
}
