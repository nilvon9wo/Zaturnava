using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Bots;

public sealed class BotCatalogTest
{
    private readonly BotCatalog _catalog = new(
        ImmutableDictionary<string, IDeciding>.Empty
            .Add("zebra", new FirstChoiceBot())
            .Add("aardvark", new FirstChoiceBot())
    );

    [Fact]
    public void BotIds_WhenListed_AreInOrder()
    {
        // Arrange - nothing beyond the catalog

        // Act
        ImmutableList<string> botIds = this._catalog.BotIds;

        // Assert
        Assert.Equal(["aardvark", "zebra"], botIds);
    }

    [Fact]
    public void Find_WhenTheBotIsUnknown_ThrowsUnknownBotException()
    {
        // Arrange - nothing beyond the catalog

        // Act
        UnknownBotException exception = Assert.Throws<UnknownBotException>(() => this._catalog.Find("genius"));

        // Assert
        Assert.Equal("There is no 'genius' bot.", exception.Message);
    }
}
