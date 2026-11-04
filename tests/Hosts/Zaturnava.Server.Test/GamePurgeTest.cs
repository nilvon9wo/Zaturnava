using Microsoft.AspNetCore.Hosting;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// Games abandoned with a guest at them let go, at a server (<see cref="QuickGamePurgeServer"/>) that keeps them
/// not at all once nothing more happens, and looks for them every few hundredths of a second.
/// </summary>
public sealed class GamePurgeTest(QuickGamePurgeServer server) : IClassFixture<QuickGamePurgeServer>
{
    private readonly HttpGameClient _client = server.GameClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sweep_WhenAGuestLeftTheGame_LetsGoOfIt()
    {
        // Arrange
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat);
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        _ = await this._client.Claim(created.GameId, 0, null, null, this._cancellation);

        // Act - nothing more happens at the game

        // Assert
        Assert.True(await this.Eventually(async () => !await this.IsListed(created.GameId)));
    }

    private async Task<bool> IsListed(string gameId) =>
        (await this._client.List(null, this._cancellation)).Games.Any(game => game.Id == gameId);

    /// <summary>Whether the condition comes true within a few seconds.</summary>
    private async Task<bool> Eventually(Func<Task<bool>> condition)
    {
        DateTimeOffset until = DateTimeOffset.UtcNow.AddSeconds(5);
        bool isTrue = await condition();
        while (!isTrue && DateTimeOffset.UtcNow < until)
        {
            await Task.Delay(50, this._cancellation);
            isTrue = await condition();
        }

        return isTrue;
    }
}

/// <summary>The server, keeping a game with a guest at it not at all once idle, and looking often.</summary>
public sealed class QuickGamePurgeServer : ZaturnavaServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder
            .UseSetting(ZaturnavaServices.GuestGamesKeptKey, "0")
            .UseSetting(ZaturnavaServices.GameSweepKey, "0.05");
    }
}
