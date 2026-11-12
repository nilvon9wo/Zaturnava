using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>The server rating its bots in the background, one short game after another.</summary>
public sealed class BotRatingJobTest(RatingServer server) : IClassFixture<RatingServer>
{
    private readonly RatingServer _server = server;
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Start_WhenTheBotsAreRated_PlaysThemAgainstEachOtherAndKeepsEachGameAtAnOfferedRuleset()
    {
        // Arrange
        IBotMatchupPersisting matchups = this._server.Services.GetRequiredService<IBotMatchupPersisting>();
        ImmutableHashSet<string> rulesets =
        [
            .. (await this._server.GameClient().Catalog(this._cancellation)).Rulesets.Select(ruleset => ruleset.Id),
        ];

        // Act
        ImmutableList<BotMatchup> kept = await Eventually(() => matchups.Matchups(this._cancellation), 2);

        // Assert
        Assert.Equal(
            (true, true),
            (
                kept.Count >= 2,
                kept.All(matchup => rulesets.Contains(matchup.Ruleset) && matchup.Played == 1)
            )
        );
    }

    /// <summary>What <paramref name="read"/> gives once it holds <paramref name="count"/>, in half a minute.</summary>
    internal static async Task<ImmutableList<T>> Eventually<T>(Func<Task<ImmutableList<T>>> read, int count)
    {
        DateTimeOffset until = DateTimeOffset.UtcNow.AddSeconds(30);
        ImmutableList<T> found = await read();
        while (found.Count < count && DateTimeOffset.UtcNow < until)
        {
            await Task.Delay(50);
            found = await read();
        }

        return found;
    }
}

/// <summary>The server rating its bots although every look at the games kept fails.</summary>
public sealed class FailingBotRatingJobTest(FailingRatingServer server) : IClassFixture<FailingRatingServer>
{
    private readonly FailingRatingServer _server = server;

    [Fact]
    public async Task Start_WhenTheGamesKeptCannotBeRead_GoesOnTryingAfterEachFailure()
    {
        // Arrange
        FailingStore store = (FailingStore)this._server.Services.GetRequiredService<IBotMatchupPersisting>();

        // Act
        ImmutableList<int> tries = await BotRatingJobTest.Eventually(
            () => Task.FromResult<ImmutableList<int>>([.. Enumerable.Range(0, store.Tries)]),
            3
        );

        // Assert
        Assert.True(tries.Count >= 3);
    }
}

/// <summary>The server, its bots playing each other to be rated, in games of a few decisions, one on another.</summary>
public class RatingServer : ZaturnavaServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder
            .UseSetting(ZaturnavaServices.BotRatingOnKey, "true")
            .UseSetting(ZaturnavaServices.BotRatingPauseKey, "0.01")
            .UseSetting(ZaturnavaServices.BotRatingStepsKey, "4")
            .UseSetting(ZaturnavaServices.BotRatingGamesKey, "1");
    }
}

/// <summary>The rating server, with games kept in a store that fails every time (<see cref="FailingStore"/>).</summary>
public sealed class FailingRatingServer : RatingServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder.ConfigureTestServices(services => services.AddSingleton<IBotMatchupPersisting, FailingStore>());
    }
}

/// <summary>A store of the bots' games that cannot be read or written, counting how often it was tried.</summary>
public sealed class FailingStore : IBotMatchupPersisting
{
    private int _tries;

    public int Tries => Volatile.Read(ref this._tries);

    public Task<ImmutableList<BotMatchup>> Matchups(CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref this._tries);
        throw new InvalidOperationException("The store is down.");
    }

    public Task Add([Required] BotMatchup games, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("The store is down.");
}
