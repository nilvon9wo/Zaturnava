using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Client;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Server.Test;

/// <summary>The server's API as a client sees it: every call through <see cref="HttpGameClient"/>.</summary>
public sealed class GameApiTest(ZaturnavaServer server) : IClassFixture<ZaturnavaServer>
{
    private const int GameQueryMax = 100;

    private readonly HttpGameClient _client = server.GameClient();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Catalog_WhenAsked_OffersChaturaji2027FirstThenGrandLegionsQuickGamesAndTheBots()
    {
        // Arrange - nothing beyond the server

        // Act
        CatalogDto catalog = await this._client.Catalog(this._cancellation);

        // Assert
        RulesetDto ruleset = catalog.Rulesets[0];
        Assert.Equal(
            "chaturaji-2027,grand-legions-small,grand-legions-medium,grand-legions-large",
            string.Join(",", catalog.Rulesets.Select(offered => offered.Id))
        );
        Assert.Equal(
            (
                ZaturnavaServer.Chaturaji2027,
                4,
                "Black,Yellow,Green,Red",
                "0,1,0,1",
                "argus,athena,midas,pythia,random,sage,solomon,tactician",
                true
            ),
            (
                ruleset.Id,
                ruleset.SeatCount,
                string.Join(",", ruleset.SeatNames),
                string.Join(",", ruleset.SeatTeams),
                string.Join(",", catalog.Bots.Select(bot => bot.Id)),
                catalog.Bots.Any(bot => bot.UsesAi)
            )
        );
    }

    [Fact]
    public async Task Catalog_WhenAsked_TellsEachRulesetsGameAndSizeAndWhetherAPreview()
    {
        // Arrange - nothing beyond the server

        // Act
        CatalogDto catalog = await this._client.Catalog(this._cancellation);

        // Assert
        Assert.Equal(
            "chaturaji-2027/Chaturaji 2027//False,grand-legions/Grand Legions/small/True,"
            + "grand-legions/Grand Legions/medium/True,grand-legions/Grand Legions/large/True",
            string.Join(
                ",",
                catalog.Rulesets.Select(offered =>
                    $"{offered.Game}/{offered.GameName}/{offered.Size}/{offered.IsPreview}"
                )
            )
        );
    }

    [Fact]
    public async Task List_WhenAskedForOneGame_ListsItsGamesAtEverySizeAndNoOther()
    {
        // Arrange
        string chaturaji = await this.CreateGame();
        GameCreatedDto grand = await this._client.Create(
            new CreateGameRequest("grand-legions-small", [null, "random", "random", "random"]),
            this._cancellation
        );

        // Act
        GamePageDto page = await this._client.List(
            new GameListQuery(PageSize: GameQueryMax, Game: "grand-legions"),
            this._cancellation
        );

        // Assert
        Assert.Equal(
            (false, true),
            (page.Games.Any(game => game.Id == chaturaji), page.Games.Any(game => game.Id == grand.GameId))
        );
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(null, true, true)]
    public async Task List_WhenAskedForPreviewsOrNot_ListsOnlyThoseOrOnlyOthersOrBoth(
        bool? preview,
        bool listsChaturaji,
        bool listsGrand
    )
    {
        // Arrange
        string chaturaji = await this.CreateGame();
        GameCreatedDto grand = await this._client.Create(
            new CreateGameRequest("grand-legions-small", [null, "random", "random", "random"]),
            this._cancellation
        );

        // Act
        GamePageDto page = await this._client.List(
            new GameListQuery(PageSize: GameQueryMax, Preview: preview),
            this._cancellation
        );

        // Assert
        Assert.Equal(
            (listsChaturaji, listsGrand),
            (page.Games.Any(game => game.Id == chaturaji), page.Games.Any(game => game.Id == grand.GameId))
        );
    }

    [Fact]
    public async Task Create_WhenTheArmiesOfAGameDealtAlikeAreAskedFor_DealsThemAgainAlike()
    {
        // Arrange - no bots, so that nothing moves before the armies are compared
        GameCreatedDto first = await this._client.Create(
            new CreateGameRequest("grand-legions-small", [null, null, null, null]) { AreArmiesAlike = true },
            this._cancellation
        );

        // Act
        GameCreatedDto again = await this._client.Create(
            new CreateGameRequest("grand-legions-small", [null, null, null, null]) { ArmiesOf = first.GameId },
            this._cancellation
        );

        // Assert
        GameViewDto before = await this._client.Get(first.GameId, this._cancellation);
        GameViewDto after = await this._client.Get(again.GameId, this._cancellation);
        Assert.Equal(
            (true, true, Armies(before)),
            (after.IsDealt, after.AreArmiesAlike, Armies(after))
        );
    }

    [Fact]
    public async Task Catalog_WhenAsked_TellsWhichGamesMayHaveTheirArmiesDrafted()
    {
        // Arrange - nothing beyond the server

        // Act
        CatalogDto catalog = await this._client.Catalog(this._cancellation);

        // Assert
        Assert.Equal(
            "False,True,True,True",
            string.Join(",", catalog.Rulesets.Select(offered => offered.IsDraftable))
        );
    }

    [Fact]
    public async Task Get_WhenTheArmiesAreToBeDrafted_OffersAThroneOrAnArmyFilledWithTheBudgetLeft()
    {
        // Arrange
        (string gameId, _) = await this.DraftedGame();

        // Act
        GameViewDto view = await this._client.Get(gameId, this._cancellation);

        // Assert
        Assert.Equal(
            (true, 4, true, true, true),
            (
                view.IsDrafted,
                view.DraftLeft!.Count,
                view.DraftLeft.All(left => left > 0),
                view.LegalActions.Where(action => action.Kind == "throne")
                    .All(action => action.Choice == "Your throne"),
                view.LegalActions.Any(action => action.Label == "Fill the rest of my army at random, and be done")
            )
        );
    }

    [Fact]
    public async Task Advance_WhenTheBotsHaveTheirArmiesFilled_LeavesTheThroneSetterPiecesByPriceOrBeingDone()
    {
        // Arrange - a throne set; each bot, in turn, has its army filled
        (string gameId, string token) = await this.DraftedGame();
        GameViewDto view = await this._client.Get(gameId, this._cancellation);
        ActionDto throne = view.LegalActions.First(action => action.Kind == "throne");
        GameViewDto set = await this._client.Submit(gameId, token, throne, this._cancellation);
        GameViewDto first = await this._client.Advance(gameId, set.Version, this._cancellation);
        GameViewDto second = await this._client.Advance(gameId, first.Version, this._cancellation);

        // Act
        GameViewDto after = await this._client.Advance(gameId, second.Version, this._cancellation);

        // Assert
        ActionDto draft = after.LegalActions.First(action => action.Kind == "draft");
        Assert.Equal(
            (true, true),
            (
                draft.Choice!.Contains(", ", StringComparison.Ordinal),
                after.LegalActions.Any(action =>
                    action.Label == $"Done drafting: keep the {after.DraftLeft![0]} points left as my score"
                )
            )
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_WhenTheGameWasOrWasNotForPlayersOnly_SaysSo(bool isForPlayers)
    {
        // Arrange
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest("chaturaji-2027", [null, "random", "random", "random"])
            {
                IsForPlayers = isForPlayers,
            },
            this._cancellation
        );

        // Act
        GameViewDto view = await this._client.Get(created.GameId, this._cancellation);

        // Assert
        Assert.Equal(isForPlayers, view.IsForPlayers);
    }

    [Fact]
    public async Task List_WhenAGameWasCreated_ShowsItsOpenSeatsByNameAndWhenItWasCreated()
    {
        // Arrange
        string gameId = await this.CreateGame();

        // Act
        GamePageDto page = await this._client.List(null, this._cancellation);

        // Assert
        GameSummaryDto game = page.Games.Single(listed => listed.Id == gameId);
        Assert.Equal([0], game.OpenSeats);
        Assert.Equal(("Black,Yellow,Green,Red", false), (string.Join(",", game.SeatNames), game.IsOver));
        Assert.Equal((true, game.Created), (game.Created is not null, game.LastChanged));
    }

    [Fact]
    public async Task List_WhenAskedByFilters_ListsOnlyTheGamesThatMatch()
    {
        // Arrange
        string open = await this.CreateGame();
        string claimed = await this.CreateGame();
        _ = await this._client.Claim(claimed, 0, null, null, this._cancellation);

        // Act
        GamePageDto page = await this._client.List(
            new GameListQuery(ZaturnavaServer.Chaturaji2027, Open: false, Over: false, PageSize: GameQueryMax),
            this._cancellation
        );

        // Assert
        Assert.Equal(
            (false, true, 1, GameQueryMax),
            (
                page.Games.Any(game => game.Id == open),
                page.Games.Any(game => game.Id == claimed),
                page.Page,
                page.PageSize
            )
        );
    }

    [Theory]
    [InlineData("bob, Rand*", false, true)]
    [InlineData("open,random", true, false)]
    [InlineData("person, Bob (guest)", false, true)]
    [InlineData("Bob, nobody", false, false)]
    [InlineData(" , ", true, true)]
    public async Task List_WhenAskedByPlayers_ListsOnlyTheGamesTheyAreAllAt(string players, bool lists, bool listsBobs)
    {
        // Arrange
        string open = await this.CreateGame();
        string bobs = await this.CreateGame();
        _ = await this._client.Claim(bobs, 0, null, new ClaimSeatRequest("Bob"), this._cancellation);

        // Act
        GamePageDto page = await this._client.List(
            new GameListQuery(PageSize: GameQueryMax, Players: players),
            this._cancellation
        );

        // Assert
        Assert.Equal(
            (lists, listsBobs),
            (page.Games.Any(game => game.Id == open), page.Games.Any(game => game.Id == bobs))
        );
    }

    [Fact]
    public async Task List_WhenAskedByPlayersForALaterPage_CountsOnlyTheGamesTheyAreAt()
    {
        // Arrange - a name no other game on the shared server has
        string name = $"Pat{Guid.NewGuid():N}"[..12];
        _ = await this.CreateGame();
        string pats = await this.CreateGame();
        _ = await this._client.Claim(pats, 0, null, new ClaimSeatRequest(name), this._cancellation);

        // Act
        GamePageDto page = await this._client.List(
            new GameListQuery(Page: 2, PageSize: 1, Players: name),
            this._cancellation
        );

        // Assert
        Assert.Equal((0, 1), (page.Games.Count, page.Total));
    }

    [Fact]
    public async Task List_WhenAskedForTooBigAPage_ListsAtMostTheMost()
    {
        // Arrange - nothing beyond the server

        // Act
        GamePageDto page = await this._client.List(new GameListQuery(PageSize: 1000), this._cancellation);

        // Assert
        Assert.Equal(GameQueryMax, page.PageSize);
    }

    [Fact]
    public async Task List_WhenASeatWasClaimed_SaysAPersonPlaysIt()
    {
        // Arrange
        string gameId = await this.CreateGame();
        _ = await this._client.Claim(gameId, 0, null, null, this._cancellation);

        // Act
        GamePageDto page = await this._client.List(null, this._cancellation);

        // Assert
        GameSummaryDto game = page.Games.Single(listed => listed.Id == gameId);
        Assert.Equal((GameSummaryDto.Guest, 0), (game.Players[0], game.OpenSeats.Count));
    }

    [Fact]
    public async Task Get_WhenTheGameIsNew_ShowsTheOpeningPositionAndTheRollOffWithNothingToPlayUntilTheSeatIsTaken()
    {
        // Arrange
        string gameId = await this.CreateGame();

        // Act
        GameViewDto view = await this._client.Get(gameId, this._cancellation);

        // Assert
        Assert.Equal(
            (64, 4, 32, 0, 0, "open", "bot", "Random", "6"),
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
    public async Task Get_WhenTheLastSeatWasTaken_OffersItsPlayerTheOpeningDie()
    {
        // Arrange - nothing beyond a claimed game

        // Act
        (_, _, GameViewDto view) = await this.ClaimedGame();

        // Assert
        Assert.Equal((0, 7, GameSummaryDto.Guest), (view.DecidingSeat, view.LegalActions.Count, view.Seats[0].Player));
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
        _ = await this._client.Claim(gameId, 0, null, null, this._cancellation);

        // Act
        Task<SeatClaimedDto> claiming = this._client.Claim(gameId, 0, null, null, this._cancellation);

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

    /// <summary>Each piece of <paramref name="view"/>, by its cell, owner and kind, in order of cell.</summary>
    private static string Armies(GameViewDto view) =>
        string.Join(
            ',',
            view.Pieces.OrderBy(piece => piece.Cell).Select(piece => $"{piece.Cell}:{piece.Owner}:{piece.Kind}")
        );

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

    /// <summary>A small Grand Legions game whose armies its players draft, its first seat claimed.</summary>
    private async Task<(string GameId, string Token)> DraftedGame()
    {
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest("grand-legions-small", [null, "random", "random", "random"]) { IsDrafted = true },
            this._cancellation
        );
        SeatClaimedDto claimed = await this._client.Claim(created.GameId, 0, null, null, this._cancellation);
        return (created.GameId, claimed.Token);
    }

    private async Task<(string GameId, string Token, GameViewDto View)> ClaimedGame()
    {
        string gameId = await this.CreateGame();
        SeatClaimedDto claimed = await this._client.Claim(gameId, 0, null, null, this._cancellation);
        GameViewDto view = await this._client.Get(gameId, this._cancellation);
        return (gameId, claimed.Token, view);
    }
}
