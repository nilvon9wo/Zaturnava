using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Players;

public sealed class GlickoTest
{
    [Fact]
    public void After_WhenGlickmansExampleIsPlayed_RatesAsHisPaperDoes()
    {
        // Arrange - the worked example of Glickman's "Example of the Glicko-2 system" (2013): 1500, 200, 0.06,
        // beating 1400 (30), losing to 1550 (100) and 1700 (300); tau his 0.5
        Glicko player = new(1500, 200, 0.06);

        // Act
        Glicko after = player.After(
            player.Rating,
            [(new Glicko(1400, 30, 0.06), 1), (new Glicko(1550, 100, 0.06), 0), (new Glicko(1700, 300, 0.06), 0)]
        );

        // Assert - his 1464.06, 151.52 and 0.05999, as far as his rounding lets them be told (worked exactly, as other
        // implementations of it find, 1464.0506, 151.5165, 0.059996)
        Assert.Equal(
            "1464.1 151.52 0.0600",
            string.Create(
                CultureInfo.InvariantCulture,
                $"{after.Rating:0.0} {after.Deviation:0.00} {after.Volatility:0.0000}"
            )
        );
    }

    [Fact]
    public void After_WhenANewPlayerBeatsOneFarStronger_RisesFarAndGrowsMoreVolatile()
    {
        // Arrange - an upset so great that the volatility's search starts from it
        Glicko player = Glicko.First;

        // Act
        Glicko after = player.After(player.Rating, [(new Glicko(2700, 30, 0.06), 1)]);

        // Assert
        Assert.True(
            after.Rating > 1700 && after.Volatility > Glicko.First.Volatility,
            $"{after.Rating} {after.Volatility}"
        );
    }

    [Fact]
    public void TeamOf_WhenAsked_IsTheMeanRatingAndTheRootMeanSquareDeviation()
    {
        // Arrange
        Glicko first = new(1400, 30, 0.06);
        Glicko second = new(1600, 40, 0.06);

        // Act
        Glicko team = Glicko.TeamOf([first, second]);

        // Assert - the root mean square of 30 and 40 is the square root of 1250
        Assert.Equal((1500, Math.Sqrt(1250)), (team.Rating, team.Deviation));
    }
}
