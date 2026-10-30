using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class BotRatingsTest
{
    [Fact]
    public void Rated_WhenAsked_RatesEveryBotAtChaturaji2027WithRandomAt1000()
    {
        // Arrange - nothing beyond the ratings kept

        // Act
        ImmutableDictionary<string, int> rated = BotRatings.Rated["chaturaji-2027"];

        // Assert
        Assert.Equal(
            ("argus,athena,midas,random,sage,solomon", 1000),
            (string.Join(",", rated.Keys.Order(StringComparer.Ordinal)), rated[RandomBot.BotId])
        );
    }
}
