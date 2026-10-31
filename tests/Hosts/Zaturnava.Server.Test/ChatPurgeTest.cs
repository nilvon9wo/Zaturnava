using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// Talk let go once its players leave, at a server (<see cref="QuickPurgeServer"/>) that counts a table left as
/// soon as no player is heard from, and looks for such tables every few hundredths of a second.
/// </summary>
public sealed class ChatPurgeTest(QuickPurgeServer server) : IClassFixture<QuickPurgeServer>
{
    private readonly HttpGameClient _client = server.GameClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sweep_WhenThePlayersHaveLeft_LetsGoOfTheTalk()
    {
        // Arrange
        (string gameId, string token) = await this.SeatedGame();
        _ = await this._client.Say(gameId, token, new SayRequest(ChatChannels.Table, "Bye"), this._cancellation);

        // Act - nobody at the table asks again; a watcher is not at it

        // Assert
        Assert.True(await this.Eventually(async () => (await this.Watched(gameId)).IsEmpty));
    }

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

    private Task<ImmutableList<ChatMessageDto>> Watched(string gameId) =>
        this._client.Chat(gameId, null, 0, this._cancellation);

    /// <summary>A game with a person at Black.</summary>
    private async Task<(string GameId, string Token)> SeatedGame()
    {
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat);
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        SeatClaimedDto claimed = await this._client.Claim(created.GameId, 0, this._cancellation);
        return (created.GameId, claimed.Token);
    }
}

/// <summary>The server, counting a table left as soon as no player is heard from, and looking often.</summary>
public sealed class QuickPurgeServer : ZaturnavaServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder
            .UseSetting(ZaturnavaServices.ChatLeftAfterKey, "0")
            .UseSetting(ZaturnavaServices.ChatSweepKey, "0.05");
    }
}
