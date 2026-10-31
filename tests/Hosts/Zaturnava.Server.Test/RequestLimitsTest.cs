using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// The limits on what one address may ask, at a server (<see cref="LimitedServer"/>) that allows
/// <see cref="LimitedServer.PerMinute"/> requests a minute and one new game an hour.
/// </summary>
public sealed class RequestLimitsTest(LimitedServer server) : IClassFixture<LimitedServer>
{
    private readonly HttpGameClient _client = server.GameClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Requests_WhenOverEitherLimit_AreRefusedAsTooBusy()
    {
        // Arrange
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat);
        _ = await this._client.Create(request, this._cancellation);

        // Act
        Refusal? secondGame = await RefusalOf(() => this._client.Create(request, this._cancellation));
        ImmutableList<Refusal?> asked = [];
        foreach (int _ in Enumerable.Range(0, LimitedServer.PerMinute))
        {
            asked = asked.Add(await RefusalOf(() => this._client.Catalog(this._cancellation)));
        }

        // Assert
        Assert.Equal<(Refusal?, Refusal?, Refusal?)>(
            (Refusal.TooBusy, null, Refusal.TooBusy),
            (secondGame, asked[0], asked[^1])
        );
    }

    private static async Task<Refusal?> RefusalOf<T>(Func<Task<T>> request)
    {
        try
        {
            _ = await request();
            return null;
        }
        catch (GameRequestRefusedException refused)
        {
            return refused.Refusal;
        }
    }
}

/// <summary>The server, allowing few requests a minute from an address, and one new game an hour.</summary>
public sealed class LimitedServer : ZaturnavaServer
{
    public const int PerMinute = 6;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder
            .UseSetting(RequestLimits.PerMinuteKey, $"{PerMinute}")
            .UseSetting(RequestLimits.GamesPerHourKey, "1");
    }
}
