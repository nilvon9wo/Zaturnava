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

    [Fact]
    public void Bots_WhenRated_SayHowStronglyEachPlaysEachRuleset()
    {
        // Arrange - the zebra rated at two rulesets, the aardvark at one
        BotCatalog catalog = new(
            [new NamedBot("zebra"), new NamedBot("aardvark")],
            ImmutableDictionary<string, ImmutableDictionary<string, int>>.Empty
                .Add("chess", ImmutableDictionary<string, int>.Empty.Add("zebra", 1200).Add("aardvark", 900))
                .Add("go", ImmutableDictionary<string, int>.Empty.Add("zebra", 1500))
        );

        // Act
        ImmutableList<BotProfile> bots = catalog.Bots;

        // Assert
        Assert.Equal(
            ("chess=900", "chess=1200,go=1500"),
            (Ratings(bots[0]), Ratings(bots[1]))
        );
    }

    private static string Ratings(BotProfile bot) =>
        string.Join(
            ",",
            bot.Ratings
                .OrderBy(rating => rating.Key, StringComparer.Ordinal)
                .Select(rating => $"{rating.Key}={rating.Value}")
        );
}

/// <summary>A bot known only by its id; it never decides.</summary>
file sealed class NamedBot(string id) : IBotPlaying
{
    public BotProfile Profile { get; } = new(id, id, id, UsesAi: false);

    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
