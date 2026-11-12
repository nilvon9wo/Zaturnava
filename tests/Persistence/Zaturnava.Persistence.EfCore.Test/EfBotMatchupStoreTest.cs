using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Test;

public sealed class EfBotMatchupStoreTest : IDisposable
{
    private readonly SqliteGameDatabase _database = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    public void Dispose() => this._database.Dispose();

    [Fact]
    public async Task Add_WhenAPairingIsPlayedAgain_AddsToItsGamesAndKeepsTheOthersApart()
    {
        // Arrange
        await new EfBotMatchupStore(this._database.NewContext()).Add(
            new BotMatchup("chess", "argus", "sage") { Won = 1, Drawn = 2 },
            this._cancellation
        );
        await new EfBotMatchupStore(this._database.NewContext()).Add(
            new BotMatchup("go", "argus", "sage") { Lost = 1 },
            this._cancellation
        );

        // Act
        await new EfBotMatchupStore(this._database.NewContext()).Add(
            new BotMatchup("chess", "argus", "sage") { Won = 1, Lost = 3 },
            this._cancellation
        );

        // Assert
        ImmutableList<BotMatchup> kept =
            await new EfBotMatchupStore(this._database.NewContext()).Matchups(this._cancellation);
        Assert.Equal(
            [
                new BotMatchup("chess", "argus", "sage") { Won = 2, Lost = 3, Drawn = 2 },
                new BotMatchup("go", "argus", "sage") { Lost = 1 },
            ],
            kept.OrderBy(matchup => matchup.Ruleset, StringComparer.Ordinal)
        );
    }
}
