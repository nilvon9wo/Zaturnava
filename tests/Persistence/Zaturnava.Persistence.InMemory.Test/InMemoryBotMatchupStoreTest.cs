using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory.Test;

public sealed class InMemoryBotMatchupStoreTest
{
    private readonly InMemoryBotMatchupStore _store = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Add_WhenAPairingIsPlayedAgain_AddsToItsGamesAndKeepsTheOthersApart()
    {
        // Arrange
        await this._store.Add(new BotMatchup("chess", "argus", "sage") { Won = 1, Drawn = 2 }, this._cancellation);
        await this._store.Add(new BotMatchup("go", "argus", "sage") { Lost = 1 }, this._cancellation);

        // Act
        await this._store.Add(new BotMatchup("chess", "argus", "sage") { Won = 1, Lost = 3 }, this._cancellation);

        // Assert
        ImmutableList<BotMatchup> kept = await this._store.Matchups(this._cancellation);
        Assert.Equal(
            [
                new BotMatchup("chess", "argus", "sage") { Won = 2, Lost = 3, Drawn = 2 },
                new BotMatchup("go", "argus", "sage") { Lost = 1 },
            ],
            kept.OrderBy(matchup => matchup.Ruleset, StringComparer.Ordinal)
        );
    }
}
