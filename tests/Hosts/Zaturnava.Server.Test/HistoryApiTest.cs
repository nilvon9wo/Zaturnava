using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>Games as data to learn from, as a client downloads them.</summary>
public sealed class HistoryApiTest(ZaturnavaServer server) : IClassFixture<ZaturnavaServer>
{
    private const int Seed = 11;
    private const int MaximumSteps = 10_000;

    private readonly ZaturnavaServer _server = server;
    private readonly HttpGameClient _client = server.GameClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task History_WhenTheGameGoesOn_TellsWhoPlaysEachSeatAndNoOutcome()
    {
        // Arrange
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );
        _ = await this._client.Claim(created.GameId, 0, null, this._cancellation);

        // Act
        GameHistoryDto history = await this._client.History(created.GameId, this._cancellation);

        // Assert
        Assert.Equal(
            (1, created.GameId, "person,random,random,random", "Black,Yellow,Green,Red", null),
            (
                history.Format,
                history.GameId,
                string.Join(',', history.Seats.Select(seat => seat.Player)),
                string.Join(',', history.Seats.Select(seat => seat.Name)),
                history.Outcome
            )
        );
    }

    [Fact]
    public async Task Histories_WhenAGameHasEnded_TellHowItDidButLeaveOutGamesGoingOn()
    {
        // Arrange
        GameRecord ended = await this.EndedGame();
        GameCreatedDto goingOn = await this._client.Create(
            new CreateGameRequest(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat),
            this._cancellation
        );

        // Act
        ImmutableList<GameHistoryDto> histories = await this._client.Histories(this._cancellation);

        // Assert
        GameHistoryDto history = histories.Single(listed => listed.GameId == ended.Id.Value);
        string[] endings =
        [
            Chaturaji2027Endings.OneTeamLeft, Chaturaji2027Endings.OnlyRajasLeft, Chaturaji2027Endings.Agreement,
            Chaturaji2027Endings.TeamGaveUp, Chaturaji2027Endings.Idle,
        ];
        Assert.Equal(
            (false, true, ended.Actions.Count, "argus,argus,argus,argus"),
            (
                histories.Any(listed => listed.GameId == goingOn.GameId),
                endings.Contains(history.Outcome?.Ending),
                history.Actions.Count,
                string.Join(',', history.Seats.Select(seat => seat.Player))
            )
        );
    }

    /// <summary>A game Argus played out on every seat, kept as the server keeps games.</summary>
    private async Task<GameRecord> EndedGame()
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
            [.. Enumerable.Repeat<SeatController>(new BotSeat(ArgusBot.BotId), 4)],
            played.Actions,
            0
        );
        await this._server.Services.GetRequiredService<IGamePersisting>().Add(record, this._cancellation);
        return record;
    }
}
