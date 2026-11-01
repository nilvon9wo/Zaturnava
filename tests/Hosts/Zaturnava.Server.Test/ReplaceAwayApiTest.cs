using Microsoft.AspNetCore.Hosting;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// Having a bot play for a player away, at the usual server, and at one where a player is away at once, and anyone
/// may have a bot play for them at once (<see cref="NoAwayGraceServer"/>).
/// </summary>
public sealed class ReplaceAwayApiTest(ZaturnavaServer server, NoAwayGraceServer eager)
    : IClassFixture<ZaturnavaServer>, IClassFixture<NoAwayGraceServer>
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Replace_WhenAPlayerIsAway_SeatsABotForThem()
    {
        // Arrange - Black and Yellow are people, Black to play and away; Yellow arrived first
        HttpGameClient client = eager.GameClient();
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, [null, null, "random", "random"]);
        GameCreatedDto created = await client.Create(request, this._cancellation);
        SeatClaimedDto yellow = await client.Claim(created.GameId, 1, null, null, this._cancellation);
        _ = await client.Claim(created.GameId, 0, null, null, this._cancellation);

        // Act
        GameViewDto view = await client.ReplaceAway(created.GameId, yellow.Token, this._cancellation);

        // Assert
        Assert.Equal(("bot", "human"), (view.Seats[0].Controller, view.Seats[1].Controller));
    }

    [Fact]
    public async Task Replace_WhenNoOneIsAway_IsRefusedAsConflict()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        (string gameId, string token) = await this.SeatedGame(client);

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.ReplaceAway(gameId, token, this._cancellation)
        );

        // Assert
        Assert.Equal(Refusal.Conflict, refused.Refusal);
    }

    [Fact]
    public async Task Replace_WhenTheTokenHoldsNoSeat_IsRefusedAsForbidden()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        (string gameId, _) = await this.SeatedGame(client);

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.ReplaceAway(gameId, "no-ones", this._cancellation)
        );

        // Assert
        Assert.Equal(Refusal.Forbidden, refused.Refusal);
    }

    /// <summary>A game of Black, played by a person, against three bots; and Black's token.</summary>
    private async Task<(string GameId, string Token)> SeatedGame(HttpGameClient client)
    {
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, [null, "random", "random", "random"]);
        GameCreatedDto created = await client.Create(request, this._cancellation);
        SeatClaimedDto claimed = await client.Claim(created.GameId, 0, null, null, this._cancellation);
        return (created.GameId, claimed.Token);
    }
}

/// <summary>The server, where a player is away once they take long, and anyone may have a bot play for them.</summary>
public sealed class NoAwayGraceServer : ZaturnavaServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder.UseSetting(ZaturnavaServices.AwayGraceKey, "0");
        _ = builder.UseSetting(ZaturnavaServices.ReplaceWaitKey, "0");
    }
}
