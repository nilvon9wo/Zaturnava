using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Bots;

public sealed class BotCatalogTest
{
    private readonly BotCatalog _catalog = new([new NamedBot("zebra"), new NamedBot("aardvark")]);

    [Fact]
    public void Bots_WhenListed_AreInOrderOfId()
    {
        // Arrange - nothing beyond the catalog

        // Act
        ImmutableList<BotProfile> bots = this._catalog.Bots;

        // Assert
        Assert.Equal(["aardvark", "zebra"], bots.Select(bot => bot.Id));
    }

    [Fact]
    public void Find_WhenTheBotIsKnown_FindsIt()
    {
        // Arrange - nothing beyond the catalog

        // Act
        IDeciding bot = this._catalog.Find("zebra");

        // Assert
        Assert.Equal("zebra", ((NamedBot)bot).Profile.Id);
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

/// <summary>A bot known only by its id; it never decides.</summary>
file sealed class NamedBot(string id) : IBotPlaying
{
    public BotProfile Profile { get; } = new(id, id, id, UsesAi: false);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
