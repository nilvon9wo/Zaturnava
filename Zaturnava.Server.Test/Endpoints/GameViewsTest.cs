using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Endpoints;
using Net.NowhereAtAll.Zaturnava.Variants.Classic;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Endpoints;

public sealed class GameViewsTest
{
    [Fact]
    public void ViewOf_WhenTheGameIsOver_ShowsNoDecidingSeat()
    {
        // Arrange
        ClassicRules rules = new();
        GameRecord record = new(
            new GameId("over"),
            rules.Id,
            [new OpenSeat(), new OpenSeat(), new OpenSeat(), new OpenSeat()],
            [],
            0
        );
        GameSnapshot over = new(record, rules, rules.CreateInitialState() with { DecidingSeat = null }, [], []);

        // Act
        GameViewDto view = GameViews.ViewOf(over);

        // Assert
        Assert.Null(view.DecidingSeat);
    }
}
