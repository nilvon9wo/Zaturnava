using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Players;

public sealed class StandingTest
{
    private static readonly PlayerId Ada = PlayerId.New();
    private static readonly PlayerId Bea = PlayerId.New();

    [Fact]
    public void Merged_WhenBothHavePlayed_WeighsEachRatingByItsGamesAndAddsTheRestUp()
    {
        // Arrange
        Standing ada = new(Ada, "chess") { Elo = 1600, Won = 3, Experience = 40, Points = 30 };
        Standing bea = new(Bea, "chess") { Elo = 1200, Lost = 1, Drawn = 0, Experience = 2, Points = 5 };

        // Act
        Standing merged = ada.Merged(bea);

        // Assert
        Assert.Equal((Ada, 1500.0, 3, 1, 0, 42, 35), (merged.Player, merged.Elo, merged.Won, merged.Lost, merged.Drawn,
            merged.Experience, merged.Points));
    }

    [Fact]
    public void Merged_WhenNeitherHasPlayed_KeepsTheirOwnRating()
    {
        // Arrange
        Standing ada = new(Ada, "chess") { Elo = 1600 };

        // Act
        Standing merged = ada.Merged(new Standing(Bea, "chess") { Elo = 1200 });

        // Assert
        Assert.Equal((1600.0, 0), (merged.Elo, merged.Played));
    }
}
