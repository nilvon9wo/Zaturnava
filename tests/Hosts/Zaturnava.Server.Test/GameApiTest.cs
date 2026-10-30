using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>The server's API as a client sees it: every call through <see cref="HttpGameClient"/>.</summary>
public sealed class GameApiTest(ZaturnavaServer server) : IClassFixture<ZaturnavaServer>
{
    private readonly HttpGameClient _client = server.GameClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Catalog_WhenAsked_OffersTheChaturaji2027RulesetAndItsBots()
    {
        // Arrange - nothing beyond the server

        // Act
        CatalogDto catalog = await this._client.Catalog(this._cancellation);

        // Assert
        RulesetDto ruleset = catalog.Rulesets.Single();
        Assert.Equal(
            (
                ZaturnavaServer.Chaturaji2027,
                4,
                "Black,Yellow,Green,Red",
                "argus,athena,midas,random,sage,solomon",
                true
            ),
            (
                ruleset.Id,
                ruleset.SeatCount,
                string.Join(",", ruleset.SeatNames),
                string.Join(",", catalog.Bots.Select(bot => bot.Id)),
                catalog.Bots.Any(bot => bot.UsesAi)
            )
        );
    }

    [Fact]
    public async Task List_WhenAGameWasCreated_ShowsItsOpenSeatsByName()
    {
        // Arrange
        string gameId = await this.CreateGame();

        // Act
        ImmutableList<GameSummaryDto> games = await this._client.List(this._cancellation);

        // Assert
        GameSummaryDto game = games.Single(listed => listed.Id == gameId);
        Assert.Equal([0], game.OpenSeats);
        Assert.Equal(("Black,Yellow,Green,Red", false), (string.Join(",", game.SeatNames), game.IsOver));
    }

    [Fact]
    public async Task List_WhenASeatWasClaimed_SaysAPersonPlaysIt()
    {
        // Arrange
        string gameId = await this.CreateGame();
        _ = await this._client.Claim(gameId, 0, this._cancellation);

        // Act
        ImmutableList<GameSummaryDto> games = await this._client.List(this._cancellation);

        // Assert
        GameSummaryDto game = games.Single(listed => listed.Id == gameId);
        Assert.Equal((GameSummaryDto.Person, 0), (game.Players[0], game.OpenSeats.Count));
    }

    [Fact]
    public async Task Get_WhenTheGameIsNew_ShowsTheOpeningPositionAndTheRollOff()
    {
        // Arrange
        string gameId = await this.CreateGame();

        // Act
        GameViewDto view = await this._client.Get(gameId, this._cancellation);

        // Assert
        Assert.Equal(
            (64, 4, 32, 0, 7, "open", "bot", "Random", "6"),
            (
                view.Cells.Count,
                view.Cells.Count(cell => cell.Throne is not null),
                view.Pieces.Count,
                view.DecidingSeat,
                view.LegalActions.Count,
                view.Seats[0].Controller,
                view.Seats[1].Controller,
                view.Seats[1].Bot,
                string.Join(",", view.Dice.Select(die => die.Value))
            )
        );
        Assert.Equal("Black goes first, with the 6 (Pawn or Raja) they rolled.", view.Log[^1].Text);
    }

    [Fact]
    public async Task Get_WhenTheGameIsUnknown_IsRefusedAsNotFound()
    {
        // Arrange - nothing beyond the server

        // Act
        Task<GameViewDto> getting = this._client.Get("missing", this._cancellation);

        // Assert
        await AssertRefused(Refusal.NotFound, getting);
    }

    [Fact]
    public async Task Create_WhenFromEachScenarioOfTheRepository_StartsThere()
    {
        // Arrange - development offers the scenarios of the repository's scenarios folder
        CatalogDto catalog = await this._client.Catalog(this._cancellation);

        // Act
        ImmutableList<GameViewDto> started =
        [
            .. await Task.WhenAll(catalog.Scenarios.Select(async scenario =>
            {
                CreateGameRequest request = new(scenario.RulesetId, ZaturnavaServer.OneOpenSeat)
                {
                    Scenario = scenario.Id,
                };
                GameCreatedDto created = await this._client.Create(request, this._cancellation);
                return await this._client.Get(created.GameId, this._cancellation);
            })),
        ];

        // Assert
        GameViewDto backToBack = started[catalog.Scenarios.FindIndex(scenario => scenario.Id == "back-to-back")];
        int d5 = backToBack.Cells.Single(cell => cell.Name == "d5").Id;
        Assert.Equal(
            ("Pawn", 2, 0),
            (
                backToBack.Pieces.Single(piece => piece.Cell == d5).Name,
                backToBack.Pieces.Count(piece => piece.Name == "Pawn" && piece.Owner % 2 == 0),
                backToBack.DecidingSeat
            )
        );
        Assert.All(started, view => Assert.NotEmpty(view.Pieces));
    }

    [Fact]
    public async Task Create_WhenTheScenarioIsUnknown_IsRefusedAsInvalid()
    {
        // Arrange
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat)
        {
            Scenario = "nowhere",
        };

        // Act
        Task<GameCreatedDto> creating = this._client.Create(request, this._cancellation);

        // Assert
        await AssertRefused(Refusal.Invalid, creating);
    }

    [Fact]
    public async Task Create_WhenTheRulesetIsUnknown_IsRefusedAsInvalid()
    {
        // Arrange
        CreateGameRequest request = new("checkers", ZaturnavaServer.OneOpenSeat);

        // Act
        Task<GameCreatedDto> creating = this._client.Create(request, this._cancellation);

        // Assert
        await AssertRefused(Refusal.Invalid, creating);
    }

    [Fact]
    public async Task Claim_WhenTheSeatIsTaken_IsRefusedAsConflict()
    {
        // Arrange
        string gameId = await this.CreateGame();
        _ = await this._client.Claim(gameId, 0, this._cancellation);

        // Act
        Task<SeatClaimedDto> claiming = this._client.Claim(gameId, 0, this._cancellation);

        // Assert
        await AssertRefused(Refusal.Conflict, claiming);
    }

    [Fact]
    public async Task Submit_WhenTheSeatPlaysItsOpeningDie_TellsOfTheMoveAndLeavesTheBotsToBeAdvanced()
    {
        // Arrange
        (string gameId, string token, GameViewDto view) = await this.ClaimedGame();
        ActionDto pawnForward = view.LegalActions.Single(
            action => action.Kind == "move" && action.Arguments["from"] == "8"
        );

        // Act
        GameViewDto after = await this._client.Submit(gameId, token, pawnForward, this._cancellation);

        // Assert
        Assert.Equal(
            (1, 2, "Black moved their Pawn from a2 to a3.|Yellow to play."),
            (after.DecidingSeat, after.Version, string.Join("|", after.Log.TakeLast(2).Select(told => told.Text)))
        );
    }

    [Fact]
    public async Task Advance_WhenABotIsDeciding_LetsItMakeOneDecision()
    {
        // Arrange
        (string gameId, string token, GameViewDto view) = await this.ClaimedGame();
        GameViewDto rolled = await this._client.Submit(gameId, token, view.LegalActions[0], this._cancellation);

        // Act
        GameViewDto after = await this._client.Advance(gameId, rolled.Version, this._cancellation);

        // Assert
        Assert.Equal(rolled.Version + 1, after.Version);
    }

    [Fact]
    public async Task Submit_WhenTheTokenHoldsNoSeat_IsRefusedAsForbidden()
    {
        // Arrange
        (string gameId, _, GameViewDto view) = await this.ClaimedGame();

        // Act
        Task<GameViewDto> submitting = this._client.Submit(gameId, "forged", view.LegalActions[0], this._cancellation);

        // Assert
        await AssertRefused(Refusal.Forbidden, submitting);
    }

    [Fact]
    public async Task Submit_WhenTheRulesForbidTheAction_IsRefusedAsIllegal()
    {
        // Arrange
        (string gameId, string token, _) = await this.ClaimedGame();
        ActionDto leap = new("move", Arguments(("actor", "0"), ("from", "0"), ("to", "63")));

        // Act
        Task<GameViewDto> submitting = this._client.Submit(gameId, token, leap, this._cancellation);

        // Assert
        await AssertRefused(Refusal.Illegal, submitting);
    }

    [Fact]
    public async Task Submit_WhenTheActionIsOfNoKnownKind_IsRefusedAsInvalid()
    {
        // Arrange
        (string gameId, string token, _) = await this.ClaimedGame();
        ActionDto fly = new("fly", Arguments(("actor", "0")));

        // Act
        Task<GameViewDto> submitting = this._client.Submit(gameId, token, fly, this._cancellation);

        // Assert
        await AssertRefused(Refusal.Invalid, submitting);
    }

    private static async Task AssertRefused<T>(Refusal expected, Task<T> request)
    {
        GameRequestRefusedException refused = await Assert.ThrowsAsync<GameRequestRefusedException>(() => request);
        Assert.Equal(expected, refused.Refusal);
    }

    private static ImmutableSortedDictionary<string, string> Arguments(
        params (string Name, string Value)[] arguments
    ) =>
        arguments.ToImmutableSortedDictionary(argument => argument.Name, argument => argument.Value);

    private async Task<string> CreateGame()
    {
        CreateGameRequest request = new(ZaturnavaServer.Chaturaji2027, ZaturnavaServer.OneOpenSeat);
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        return created.GameId;
    }

    private async Task<(string GameId, string Token, GameViewDto View)> ClaimedGame()
    {
        string gameId = await this.CreateGame();
        SeatClaimedDto claimed = await this._client.Claim(gameId, 0, this._cancellation);
        GameViewDto view = await this._client.Get(gameId, this._cancellation);
        return (gameId, claimed.Token, view);
    }
}
