namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class ScoringTest
{
    [Fact]
    public void Standard_WhenRead_ScoresAsTheRulesSay()
    {
        // Arrange - rules.md §5, §8 and §10

        // Act
        Scoring scoring = Scoring.Standard;

        // Assert
        Assert.Equal(new Scoring(1, 2, 1, 2, 4, 2, 1, 2, 10), scoring);
    }

    [Fact]
    public void Of_WhenTheAssemblyKeepsNoTableForTheGame_ThrowsArgumentException()
    {
        // Arrange - nothing beyond the assembly

        // Act
        static Scoring Reading() => Scoring.Of(typeof(Scoring).Assembly, "no-such-game");

        // Assert
        _ = Assert.Throws<ArgumentException>(Reading);
    }
}
