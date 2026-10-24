using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Games;

public sealed class SeatPlanTest
{
    private static readonly BotCatalog Bots =
        new(ImmutableDictionary<string, IDeciding>.Empty.Add(FirstChoiceBot.BotId, new FirstChoiceBot()));

    [Fact]
    public void ToController_WhenOpen_IsAnOpenSeat()
    {
        // Arrange - nothing beyond the plan

        // Act
        SeatController controller = SeatPlan.Open.ToController(Bots);

        // Assert
        Assert.Equal(new OpenSeat(), controller);
    }

    [Fact]
    public void ToController_WhenTheBotIsKnown_IsThatBotsSeat()
    {
        // Arrange
        SeatPlan plan = new(FirstChoiceBot.BotId);

        // Act
        SeatController controller = plan.ToController(Bots);

        // Assert
        Assert.Equal(new BotSeat(FirstChoiceBot.BotId), controller);
    }

    [Fact]
    public void ToController_WhenTheBotIsUnknown_ThrowsUnknownBotException()
    {
        // Arrange
        SeatPlan plan = new("genius");

        // Act
        UnknownBotException exception = Assert.Throws<UnknownBotException>(() => plan.ToController(Bots));

        // Assert
        Assert.Equal("There is no 'genius' bot.", exception.Message);
    }
}
