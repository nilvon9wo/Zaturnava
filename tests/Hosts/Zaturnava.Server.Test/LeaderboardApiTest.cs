using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>
/// Where players stand, at a server (<see cref="QuickStandingServer"/>) that counts the games over every few
/// hundredths of a second.
/// </summary>
public sealed class LeaderboardApiTest(QuickStandingServer server) : IClassFixture<QuickStandingServer>
{
    private const int Seed = 5;
    private const int MaximumSteps = 10_000;

    private readonly QuickStandingServer _server = server;
    private readonly HttpGameClient _client = server.GameClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Leaderboard_WhenAPlayerFinishedAGameAgainstBots_ListsThemAsHavingPlayedItOnceWithTheirPoints()
    {
        // Arrange
        Player ada = new(new PlayerId("leaderboard-ada"), "Leaderboard Ada", []);
        await this._server.Services.GetRequiredService<IPlayerPersisting>().Add(ada, this._cancellation);
        int points = await this.AddEndedGame(ada.Id);

        // Act
        StandingDto? standing = await this.Eventually(ada.Name);

        // Assert
        Assert.Equal(
            (1, points),
            (standing is null ? 0 : standing.Won + standing.Lost + standing.Drawn, standing?.Points)
        );
    }

    [Fact]
    public async Task Leaderboards_WhenAPlayerFinishedAGameAgainstBotsAlone_ListThemAgainstBotsAndTheBotsByName()
    {
        // Arrange
        Player bea = new(new PlayerId("leaderboard-bea"), "Leaderboard Bea", []);
        await this._server.Services.GetRequiredService<IPlayerPersisting>().Add(bea, this._cancellation);
        _ = await this.AddEndedGame(bea.Id);
        _ = await this.Eventually(bea.Name);
        string game = new Chaturaji2027Rules().GameId;

        // Act
        ImmutableList<StandingDto> againstBots = await this._client.AgainstBotsLeaderboard(game, this._cancellation);
        ImmutableList<BotStandingDto> bots = await this._client.BotLeaderboard(game, this._cancellation);

        // Assert
        StandingDto standing = Assert.Single(againstBots, standing => standing.Name == bea.Name);
        BotStandingDto argus = Assert.Single(bots, bot => bot.Name == new ArgusBot(new Random(Seed)).Profile.Name);
        Assert.Equal(
            (1, 1, true),
            (
                standing.WonAgainstBots + standing.LostAgainstBots + standing.DrawnAgainstBots,
                argus.Place,
                argus.Won + argus.Lost + argus.Drawn >= 3
                    && argus.WonAgainstPeople + argus.LostAgainstPeople + argus.DrawnAgainstPeople == 0
            )
        );
    }

    [Fact]
    public async Task Leaderboard_WhenNoOneHasPlayedTheGame_ListsNoOne()
    {
        // Arrange - nothing beyond the server

        // Act
        ImmutableList<StandingDto> leaders = await this._client.Leaderboard("unplayed", this._cancellation);

        // Assert
        Assert.Empty(leaders);
    }

    /// <summary>
    /// A game Argus played out on every seat, the first of them held by <paramref name="player"/>; the points that seat
    /// scored.
    /// </summary>
    private async Task<int> AddEndedGame(PlayerId player)
    {
        ImmutableList<IDeciding> players =
            [.. Enumerable.Range(0, 4).Select(seat => new ArgusBot(new Random(Seed + seat)))];
        Played played = await DecisionLoop.PlayAll(
            new Chaturaji2027Rules(),
            players,
            new RandomDice(new Random(Seed)),
            MaximumSteps,
            this._cancellation
        );
        GameRecord record = new(
            GameId.New(),
            ZaturnavaServer.Chaturaji2027,
            [new HumanSeat("hash", player), .. Enumerable.Repeat<SeatController>(new BotSeat(ArgusBot.BotId), 3)],
            played.Actions,
            0
        )
        {
            IsOver = true,
        };
        await this._server.Services.GetRequiredService<IGamePersisting>().Add(record, this._cancellation);
        return played.State.Scores.GetValueOrDefault(new SeatId(0));
    }

    /// <summary>Where the player named <paramref name="name"/> stands, once listed within a few seconds.</summary>
    private async Task<StandingDto?> Eventually(string name)
    {
        DateTimeOffset until = DateTimeOffset.UtcNow.AddSeconds(5);
        StandingDto? standing = await this.StandingOf(name);
        while (standing is null && DateTimeOffset.UtcNow < until)
        {
            await Task.Delay(50, this._cancellation);
            standing = await this.StandingOf(name);
        }

        return standing;
    }

    private async Task<StandingDto?> StandingOf(string name) =>
        (await this._client.Leaderboard(new Chaturaji2027Rules().GameId, this._cancellation))
            .FirstOrDefault(standing => standing.Name == name);
}

/// <summary>The server, counting the games over often.</summary>
public sealed class QuickStandingServer : ZaturnavaServer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        _ = builder.UseSetting(ZaturnavaServices.StandingSweepKey, "0.05");
    }
}
