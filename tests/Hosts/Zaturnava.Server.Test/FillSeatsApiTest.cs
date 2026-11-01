using Microsoft.AspNetCore.Hosting;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// Filling a game's open seats with bots, at the usual server (where the first to arrive must wait a minute first)
/// and at one where no one need wait (<see cref="NoFillWaitServer"/>).
/// </summary>
public sealed class FillSeatsApiTest(ZaturnavaServer server, NoFillWaitServer eager)
    : IClassFixture<ZaturnavaServer>, IClassFixture<NoFillWaitServer>
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Get_WhenAGameWaitsForItsSeats_SaysWhoMayFillThemAndWhen()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        (string gameId, _) = await this.WaitingGame(client);

        // Act
        GameViewDto view = await client.Get(gameId, this._cancellation);

        // Assert
        Assert.Equal(
            (0, true, true),
            (
                view.Vacancy?.FirstArrival,
                view.Vacancy?.SecondsUntilFirst > 0,
                view.Vacancy?.SecondsUntilAnyone > view.Vacancy?.SecondsUntilFirst
            )
        );
    }

    [Fact]
    public async Task Fill_WhenTooSoon_IsRefusedAsConflict()
    {
        // Arrange
        HttpGameClient client = server.GameClient();
        (string gameId, string token) = await this.WaitingGame(client);

        // Act
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() =>
            client.FillOpenSeats(gameId, token, this._cancellation)
        );

        // Assert
        Assert.Equal(Refusal.Conflict, refused.Refusal);
    }

    [Fact]
    public async Task Fill_WhenThePlayerMay_SeatsBotsSoTheGameBegins()
    {
        // Arrange
        HttpGameClient client = eager.GameClient();
        (string gameId, string token) = await this.WaitingGame(client);

        // Act
        GameViewDto view = await client.FillOpenSeats(gameId, token, this._cancellation);

        // Assert
        Assert.Equal(
            ("human,bot,bot,bot", null),
            (string.Join(",", view.Seats.Select(seat => seat.Controller)), view.Vacancy)
        );
    }

    /// <summary>A game of Black and three open seats, Black claimed; and Black's token.</summary>
    private async Task<(string GameId, string Token)> WaitingGame(HttpGameClient client)
    {
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, [null, null, null, null]);
        GameCreatedDto created = await client.Create(request, this._cancellation);
        SeatClaimedDto claimed = await client.Claim(created.GameId, 0, null, null, this._cancellation);
        return (created.GameId, claimed.Token);
    }
}

/// <summary>The server, where the first to arrive at a game may fill its open seats at once.</summary>
public sealed class NoFillWaitServer : ZaturnavaServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder.UseSetting(ZaturnavaServices.FillWaitKey, "0");
    }
}
