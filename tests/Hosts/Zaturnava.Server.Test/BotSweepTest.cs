using Microsoft.AspNetCore.Hosting;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// Games left waiting for one of the server's bots played on, at a server (<see cref="QuickBotSweepServer"/>) that
/// plays them once idle at all, and looks for them every few hundredths of a second (the user's report of 2026-11-20).
/// </summary>
public sealed class BotSweepTest(QuickBotSweepServer server) : IClassFixture<QuickBotSweepServer>
{
    private readonly HttpGameClient _client = server.GameClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sweep_WhenNoOneAdvancesTheBotsDue_PlaysThemUntilThePersonMustDecide()
    {
        // Arrange - three bots to move first, then a guest, the last seat; the server lets no bot move as it begins
        CreateGameRequest request = new(
            ZaturnavaServer.Chaturaji2027,
            [ZaturnavaServer.Random, ZaturnavaServer.Random, ZaturnavaServer.Random, null]
        );
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        _ = await this._client.Claim(created.GameId, 3, null, null, this._cancellation);

        // Act - no one advances the game

        // Assert
        Assert.True(await this.Eventually(async () => await this.DecidingOf(created.GameId) == 3));
    }

    private async Task<int?> DecidingOf(string gameId) =>
        (await this._client.List(null, this._cancellation)).Games.Single(game => game.Id == gameId).DecidingSeat;

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

/// <summary>The server, playing on a game waiting for a bot as soon as it is idle at all, and looking often.</summary>
public sealed class QuickBotSweepServer : ZaturnavaServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder
            .UseSetting(ZaturnavaServices.BotIdleKey, "0")
            .UseSetting(ZaturnavaServices.BotSweepKey, "0.05");
    }
}
