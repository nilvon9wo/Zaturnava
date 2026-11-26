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
            "chaturaji-2027,grand-legions-small,grand-legions-medium,grand-legions-large,grand-legions-custom",
            string.Join(",", catalog.Rulesets.Select(offered => offered.Id))
        );
        Assert.Equal(
            (
                ZaturnavaServer.Chaturaji2027,
                4,
                "Black,Yellow,Green,Red",
                "0,1,0,1",
                "argus,athena,croesus,desdemona,echo,epimetheus,icarus,midas,morpheus,musashi,narcissus,naresuan,"
                    + "oberon,palamedes,phaethon,philidor,pythia,ragnar,random,sage,solomon,sun-tzu,tactician,"
                    + "tantalus,yi-sun-sin",
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
            "chaturaji-2027/Chaturaji 2027//False,grand-legions/Grand Legions/small/False/6x6x3x2,"
            + "grand-legions/Grand Legions/medium/False/8x8x4x2,grand-legions/Grand Legions/large/False/12x12x6x3,"
            + "grand-legions/Grand Legions/custom/False/10x10x4x3/10x10x4x3",
            string.Join(
                ",",
                catalog.Rulesets.Select(offered =>
                    $"{offered.Game}/{offered.GameName}/{offered.Size}/{offered.IsPreview}"
                        + SizeOf(offered.Board) + SizeOf(offered.SizableBoard)
                )
            )
        );
    }

    private static string SizeOf(BoardSizeDto? board) =>
        board is null ? string.Empty : $"/{board.Width}x{board.Height}x{board.KingdomWidth}x{board.KingdomDepth}";

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

    [Fact]
    public async Task List_WhenGamesWereDealtOnBoardsSetOrNot_TellsEachItsBoardAndBudget()
    {
        // Arrange - a board set by hand, a size's own, and a game not dealt (the user, 2026-11-20)
        GameCreatedDto custom = await this._client.Create(
            new CreateGameRequest("grand-legions-custom", [null, "random", "random", "random"])
            {
                Board = new BoardSizeDto(12, 9, 4, 4),
            },
            this._cancellation
        );
        GameCreatedDto small = await this._client.Create(
            new CreateGameRequest("grand-legions-small", [null, "random", "random", "random"]),
            this._cancellation
        );
        string chaturaji = await this.CreateGame();

        // Act
        ImmutableList<GameSummaryDto> listed =
        [
            .. await Task.WhenAll(
                new[] { custom.GameId, small.GameId, chaturaji }.Select(async id =>
                    Assert.Single(
                        (await this._client.List(new GameListQuery(Code: id[..8]), this._cancellation)).Games
                    )
                )
            ),
        ];

        // Assert
        Assert.Equal(
            "12x9x4x4/True,6x6x3x2/True,/False",
            string.Join(
                ",",
                listed.Select(game =>
                    (game.Board is { } board
                        ? $"{board.Width}x{board.Height}x{board.KingdomWidth}x{board.KingdomDepth}"
                        : string.Empty)
                    + $"/{game.Budget is not null}"
                )
            )
        );
    }

    [Fact]
    public async Task List_WhenAskedForAGameByItsCode_ListsItAloneWhateverTheCaseAndSpacesAround()
    {
        // Arrange
        string game = await this.CreateGame();
        _ = await this.CreateGame();

        // Act
        GamePageDto page = await this._client.List(
            new GameListQuery(Code: $" {game[..8].ToUpperInvariant()} "),
            this._cancellation
        );

        // Assert
        Assert.Equal(game, Assert.Single(page.Games).Id);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(null, true, true)]
    public async Task List_WhenAskedForPreviewsOrNot_ListsOnlyThoseOrOnlyOthersOrBoth(
        bool? preview,
        bool listsChaturaji,
        bool listsGrand
    )
    {
        // Arrange - neither game a preview, Grand Legions no longer (the user's word of 2026-11-20)
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
            "False,True,True,True,True",
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
    public async Task Get_WhenTheKingdomHasRoomForSeveralPalaces_OffersEachForOneBeforeAnyThrone()
    {
        // Arrange - a large game's kingdom, 6 × 3, has room for a palace about any of 4 squares of its middle row
        (string gameId, _) = await this.DraftedGame(rulesetId: "grand-legions-large");

        // Act
        GameViewDto view = await this._client.Get(gameId, this._cancellation);

        // Assert
        Assert.Equal(
            ("Your palace,Your palace,Your palace,Your palace", false),
            (
                string.Join(
                    ",",
                    view.LegalActions.Where(action => action.Kind == "palace").Select(action => action.Choice)
                ),
                view.LegalActions.Any(action => action.Kind == "throne")
            )
        );
    }

    [Fact]
    public async Task Get_WhenTheCreatorSetTheBoardsSize_PlaysItThereAndSaysSo()
    {
        // Arrange
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest("grand-legions-custom", [null, "random", "random", "random"])
            {
                Board = new BoardSizeDto(12, 9, 4, 4),
            },
            this._cancellation
        );

        // Act
        GameViewDto view = await this._client.Get(created.GameId, this._cancellation);

        // Assert
        Assert.Equal(
            ("grand-legions-custom", "Grand Legions (12 × 9, kingdoms 4 × 4)", 108, new BoardSizeDto(12, 9, 4, 4)),
            (view.RulesetId, view.RulesetName, view.Cells.Count, view.Board)
        );
    }

    [Fact]
    public async Task Get_WhenTheCreatorChoseTheRivers_PlaysOnThemAndSaysSo()
    {
        // Arrange - every axis of the terrain the catalog offers, each set to its first option: no rivers, through
        // the kingdoms (asked 2026-11-19)
        CatalogDto catalog = await this._client.Catalog(this._cancellation);
        ImmutableList<TerrainAxisDto> axes = catalog.Rulesets.Single(ruleset => ruleset.Id == "grand-legions-custom")
            .TerrainAxes;
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest("grand-legions-custom", [null, "random", "random", "random"])
            {
                TerrainMask = axes.Aggregate(0, (all, axis) => all | axis.Mask),
                TerrainValue = axes.Aggregate(0, (all, axis) => all | axis.Options[0].Value),
            },
            this._cancellation
        );

        // Act
        GameViewDto view = await this._client.Get(created.GameId, this._cancellation);

        // Assert
        Assert.Equal(
            (2, "No rivers.", 0),
            (axes.Count, view.Terrain, view.Lines.Count(line => line.Kind == "river"))
        );
    }

    [Fact]
    public async Task Create_WhenTheBoardIsSetTooSmall_RefusesIt()
    {
        // Arrange - kingdoms of 4 x 4 overlap on a board 6 cells across
        CreateGameRequest request = new("grand-legions-custom", [null, "random", "random", "random"])
        {
            Board = new BoardSizeDto(6, 6, 4, 4),
        };

        // Act
        Task<GameCreatedDto> creating = this._client.Create(request, this._cancellation);

        // Assert
        _ = await Assert.ThrowsAsync<GameRequestRefusedException>(() => creating);
    }

    [Fact]
    public async Task Get_WhenTheDraftsBudgetAndDrafterAreSet_SaysSoAndHasTheDrafterSpendIt()
    {
        // Arrange - asked in secret too, which one drafting for all has no need of
        (string gameId, _) = await this.DraftedGame(40, 0, isSecret: true);

        // Act
        GameViewDto view = await this._client.Get(gameId, this._cancellation);

        // Assert
        Assert.Equal(
            (true, (int?)40, (int?)0, (int?)40, (int?)0, false),
            (
                view.IsDrafted,
                view.DraftBudget,
                view.Drafter,
                view.DraftLeft![0],
                view.DecidingSeat,
                view.IsDraftSecret
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

    [Fact]
    public async Task Get_WhenTheArmiesAreDraftedInSecret_ShowsASeatsHolderOnlyWhatItsTeamDrafted()
    {
        // Arrange - Black and Yellow, people, each set a throne and buy a piece; Green, Black's ally, and Red, bots,
        // draft theirs all at once
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest("grand-legions-small", [null, null, "random", "random"])
            {
                IsDrafted = true,
                IsDraftSecret = true,
            },
            this._cancellation
        );
        string gameId = created.GameId;
        string black = (await this._client.Claim(gameId, 0, null, null, this._cancellation)).Token;
        string yellow = (await this._client.Claim(gameId, 1, null, null, this._cancellation)).Token;
        _ = await this.Decided(gameId, black, action => action.Kind == "throne");
        _ = await this.Decided(gameId, yellow, action => action.Kind == "throne");
        _ = await this.Decided(gameId, black, action => action.Kind == "draft");
        _ = await this.Decided(gameId, yellow, action => action.Kind == "draft");

        // Act
        GameViewDto seen = await this._client.Get(gameId, black, this._cancellation);
        GameViewDto other = await this._client.Get(gameId, yellow, this._cancellation);

        // Assert
        Assert.Equal(
            (true, "0,2", "1,3", "False,True,False,True", "0,2", "0,2"),
            (
                seen.IsDraftSecret,
                string.Join(",", seen.Pieces.Select(piece => piece.Owner).Distinct().Order()),
                string.Join(",", other.Pieces.Select(piece => piece.Owner).Distinct().Order()),
                string.Join(",", seen.DraftLeft!.Select(left => left is null)),
                string.Join(",", seen.Cells.Select(cell => cell.Throne).OfType<int>().Order()),
                string.Join(
                    ",",
                    seen.Log.Where(told => told.Kind == EventDto.Drafted)
                        .Select(told => told.Seat)
                        .Distinct()
                        .Order()
                )
            )
        );
    }

    [Fact]
    public async Task Get_WhileTheArmiesAreDraftedAllAtOnce_ShowsEachHolderTheirOwnStepsAndHowToChangeTheirMind()
    {
        // Arrange - Black decides; Yellow places its palace, sets its throne in its middle and buys a piece, out of
        // turn
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest("grand-legions-large", [null, null, "random", "random"])
            {
                IsDrafted = true,
                IsDraftSecret = true,
            },
            this._cancellation
        );
        string gameId = created.GameId;
        _ = await this._client.Claim(gameId, 0, null, null, this._cancellation);
        string yellow = (await this._client.Claim(gameId, 1, null, null, this._cancellation)).Token;
        GameViewDto first = await this._client.Get(gameId, yellow, this._cancellation);
        ActionDto palace = first.LegalActions.First(action => action.Kind == "palace");
        _ = await this._client.Submit(gameId, yellow, palace, this._cancellation);
        _ = await this.Decided(
            gameId,
            yellow,
            action => action.Kind == "throne" && action.Arguments["home"] == palace.Arguments["home"]
        );
        _ = await this.Decided(gameId, yellow, action => action.Kind == "draft");

        // Act
        GameViewDto buying = await this._client.Get(gameId, yellow, this._cancellation);
        GameViewDto done = await this.Decided(gameId, yellow, action => action.Kind == "pass");

        // Assert
        Assert.Equal(
            (1, "Move your palace,Move your throne,Take back a piece", true, 0),
            (
                first.DecidingSeat,
                string.Join(
                    ",",
                    buying.LegalActions.Where(action => action.Kind != "draft")
                        .Select(action => action.Choice)
                        .OfType<string>()
                        .Distinct()
                        .Order(StringComparer.Ordinal)
                ),
                buying.Seats.All(seat => seat.Chances.IsEmpty),
                done.DecidingSeat
            )
        );
    }

    [Fact]
    public async Task Get_WhenTheArmiesAreDraftedInSecretFromAlliesToo_ShowsASeatsHolderOnlyItsOwn()
    {
        // Arrange - Black and Yellow, people, each set a throne; so do Green, Black's ally, and Red
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest("grand-legions-small", [null, null, "random", "random"])
            {
                IsDrafted = true,
                IsDraftSecret = true,
                IsDraftHiddenFromAllies = true,
            },
            this._cancellation
        );
        string gameId = created.GameId;
        string black = (await this._client.Claim(gameId, 0, null, null, this._cancellation)).Token;
        string yellow = (await this._client.Claim(gameId, 1, null, null, this._cancellation)).Token;
        _ = await this.Decided(gameId, black, action => action.Kind == "throne");
        GameViewDto thrones = await this.Decided(gameId, yellow, action => action.Kind == "throne");
        GameViewDto green = await this._client.Advance(gameId, thrones.Version, black, this._cancellation);
        _ = await this._client.Advance(gameId, green.Version, black, this._cancellation);

        // Act
        GameViewDto seen = await this._client.Get(gameId, black, this._cancellation);

        // Assert
        Assert.Equal(
            (true, "False,True,True,True", "0"),
            (
                seen.IsDraftHiddenFromAllies,
                string.Join(",", seen.DraftLeft!.Select(left => left is null)),
                string.Join(",", seen.Cells.Select(cell => cell.Throne).OfType<int>().Order())
            )
        );
    }

    [Fact]
    public async Task Get_WhenTheArmiesAreDraftedInSecret_ShowsOneHoldingNoSeatNoArmyNorChoicesNorHistory()
    {
        // Arrange - Black's throne set
        (string gameId, string token) = await this.DraftedGame(isSecret: true);
        GameViewDto view = await this._client.Get(gameId, token, this._cancellation);
        ActionDto throne = view.LegalActions.First(action => action.Kind == "throne");
        _ = await this._client.Submit(gameId, token, throne, this._cancellation);

        // Act
        GameViewDto seen = await this._client.Get(gameId, this._cancellation);
        GameHistoryDto history = await this._client.History(gameId, this._cancellation);

        // Assert
        Assert.Equal(
            (0, true, 0, 0, 0, 1),
            (
                seen.Pieces.Count,
                seen.DraftLeft!.All(left => left is null),
                seen.Cells.Count(cell => cell.Throne is not null),
                seen.LegalActions.Count,
                seen.Log.Count(told => told.Kind == EventDto.Drafted),
                history.Actions.Count
            )
        );
    }

    [Fact]
    public async Task Get_WhenTheArmiesAreDraftedInSecretWithPalaces_ShowsOnlyTheTeamsPalaces()
    {
        // Arrange - a large game: Black's palace built
        (string gameId, string token) = await this.DraftedGame(rulesetId: "grand-legions-large", isSecret: true);
        GameViewDto view = await this._client.Get(gameId, token, this._cancellation);
        ActionDto palace = view.LegalActions.First(action => action.Kind == "palace");
        _ = await this._client.Submit(gameId, token, palace, this._cancellation);

        // Act
        GameViewDto seen = await this._client.Get(gameId, token, this._cancellation);
        GameViewDto unseen = await this._client.Get(gameId, this._cancellation);

        // Assert
        Assert.Equal(
            (true, false),
            (
                seen.Lines.Count > unseen.Lines.Count,
                unseen.Lines.Count > view.Lines.Count
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
        Assert.Equal(
            ("Black,Yellow,Green,Red", false, 0),
            (string.Join(",", game.SeatNames), game.IsOver, game.DecidingSeat)
        );
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
    public async Task Create_WhenFromTheGrandLegionsBackToBack_OffersBothSwaps()
    {
        // Arrange - Black's Makruk Pawn and Lance, each back to back with a Green Pawn (the user's report of
        // 2026-11-19: only the leader could move)
        CreateGameRequest request = new("grand-legions-medium", ZaturnavaServer.OneOpenSeat)
        {
            Scenario = "forward-only-swaps",
        };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        string black = (await this._client.Claim(created.GameId, 0, null, null, this._cancellation)).Token;

        // Act
        GameViewDto view = await this._client.Get(created.GameId, black, this._cancellation);

        // Assert
        string CellOf(string name) => $"{view.Cells.Single(cell => cell.Name == name).Id}";
        bool IsSwap(string from, string to) =>
            view.LegalActions.Any(action =>
                action.Kind == "move"
                && action.Arguments["from"] == CellOf(from)
                && action.Arguments["to"] == CellOf(to)
                && action.Label is not null
            );
        Assert.Equal((true, true), (IsSwap("d3", "d4"), IsSwap("f3", "f4")));
    }

    [Theory]
    [InlineData("chaturaji-2027", "back-to-back", "d4", "d5")]
    [InlineData("grand-legions-medium", "pawns", "f3", "f4")]
    [InlineData("grand-legions-medium", "forward-only-swaps", "d3", "d4")]
    public async Task Create_WhenFromAScenarioOfPiecesBackToBack_OffersTheirSwap(
        string ruleset,
        string scenario,
        string from,
        string to
    )
    {
        // Arrange - the user's report of 2026-11-19: back to back still not working in the scenarios
        CreateGameRequest request = new(ruleset, ZaturnavaServer.OneOpenSeat) { Scenario = scenario };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        string black = (await this._client.Claim(created.GameId, 0, null, null, this._cancellation)).Token;

        // Act
        GameViewDto view = await this._client.Get(created.GameId, black, this._cancellation);

        // Assert - labelled, as the page keeps no other move onto its own team's piece while the safety is on
        string CellOf(string name) => $"{view.Cells.Single(cell => cell.Name == name).Id}";
        Assert.Contains(
            view.LegalActions,
            action => action.Kind == "move"
                && action.Arguments["from"] == CellOf(from)
                && action.Arguments["to"] == CellOf(to)
                && action.Label is not null
        );
    }

    [Fact]
    public async Task Get_WhenTheKingMayCastle_SaysWhereItsRookGoesWithIt()
    {
        // Arrange - Black's King on d1 may castle to b1, its Rook from a1 to c1 (the user's report of 2026-11-19:
        // hovering the King's landing, the Rook should show where it lands too)
        CreateGameRequest request = new("grand-legions-medium", ZaturnavaServer.OneOpenSeat) { Scenario = "castling" };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        string black = (await this._client.Claim(created.GameId, 0, null, null, this._cancellation)).Token;

        // Act
        GameViewDto view = await this._client.Get(created.GameId, black, this._cancellation);

        // Assert
        int CellOf(string name) => view.Cells.Single(cell => cell.Name == name).Id;
        ActionDto castle = view.LegalActions.Single(action =>
            action.Kind == "move"
            && action.Arguments["from"] == $"{CellOf("d1")}"
            && action.Arguments["to"] == $"{CellOf("b1")}"
        );
        Assert.Equal(
            (new PartnerMoveDto(CellOf("a1"), CellOf("c1")), 1),
            (castle.Partner, view.LegalActions.Count(action => action.Partner is not null))
        );
    }

    [Fact]
    public async Task Submit_WhenAMercenaryIsHiredInTheScenario_OffersItsPlacing()
    {
        // Arrange - Black holds Red's Dragon Horse, with the points to hire it (the user's report of 2026-11-19: hiring
        // it answered 500)
        CreateGameRequest request = new("grand-legions-medium", ZaturnavaServer.OneOpenSeat)
        {
            Scenario = "mercenaries",
        };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        string black = (await this._client.Claim(created.GameId, 0, null, null, this._cancellation)).Token;
        GameViewDto view = await this._client.Get(created.GameId, black, this._cancellation);

        // Act
        GameViewDto hired = await this._client.Submit(
            created.GameId,
            black,
            view.LegalActions.First(action => action.Kind == "hire"),
            this._cancellation
        );

        // Assert - on the free squares of Black's palace, a1 to c3, its Rook on a1 and Gold General on c1
        Assert.Equal(
            "a2,a3,b1,b2,b3,c2,c3",
            string.Join(
                ",",
                hired.LegalActions.Where(action => action.Kind == "deploy")
                    .Select(action => hired.Cells.Single(cell => $"{cell.Id}" == action.Arguments["home"]).Name)
                    .Order(StringComparer.Ordinal)
            )
        );
    }

    [Fact]
    public async Task Submit_WhenTheTaflKingComesToItsFarCornerInItsScenario_OffersItsEscapeAtOnceAmongTheTurnsChoices()
    {
        // Arrange - Black's Tafl King on h4, its 5 taking it to h8 (the user's reports of 2026-11-19: it should escape
        // as soon as it gets there; staying is the rule, the turn not stopped to ask)
        CreateGameRequest request = new("grand-legions-medium", ZaturnavaServer.OneOpenSeat)
        {
            Scenario = "tafl-escape",
        };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        string black = (await this._client.Claim(created.GameId, 0, null, null, this._cancellation)).Token;
        GameViewDto view = await this._client.Get(created.GameId, black, this._cancellation);
        string CellOf(string name) => $"{view.Cells.Single(cell => cell.Name == name).Id}";

        // Act
        GameViewDto arrived = await this._client.Submit(
            created.GameId,
            black,
            view.LegalActions.First(action =>
                action.Kind == "move"
                && action.Arguments["from"] == CellOf("h4")
                && action.Arguments["to"] == CellOf("h8")
            ),
            this._cancellation
        );

        // Assert - the 3 still to move the Rook; and the scenario it came from, to play again
        string flights = string.Join(
            "|",
            arrived.LegalActions.Where(action => action.Kind == "flee").Select(action => action.Label)
        );
        Assert.Equal(
            (
                "Escape the board with Black's King (Tafl), from h8: no one can take it until it comes back",
                true,
                null,
                "tafl-escape"
            ),
            (flights, arrived.LegalActions.Any(action => action.Kind == "move"), arrived.Fleeing, arrived.Scenario)
        );
    }

    [Fact]
    public async Task Submit_WhenAShogiPawnIsPromotedIntoACapturedWesternRook_ShowsItStillAMercenary()
    {
        // Arrange - the promotions scenario: Black's Shogi Pawn on f7, Yellow's Rook held (the user's report of
        // 2026-11-20: whether it stays a mercenary could not be seen)
        CreateGameRequest request = new("grand-legions-medium", ZaturnavaServer.OneOpenSeat)
        {
            Scenario = "promotions",
        };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        string black = (await this._client.Claim(created.GameId, 0, null, null, this._cancellation)).Token;
        GameViewDto view = await this._client.Get(created.GameId, black, this._cancellation);
        int CellOf(string name) => view.Cells.Single(cell => cell.Name == name).Id;
        GameViewDto arrived = await this._client.Submit(
            created.GameId,
            black,
            view.LegalActions.First(action => action.Kind == "move" && action.Arguments["from"] == $"{CellOf("f7")}"),
            this._cancellation
        );

        // Act
        GameViewDto promoted = await this._client.Submit(
            created.GameId,
            black,
            arrived.LegalActions.First(action => action.Kind == "promote"),
            this._cancellation
        );

        // Assert - Black's Western Pawn, never a mercenary, stays none
        Assert.Equal(
            ("Rook (Western)", true, false),
            (
                promoted.Pieces.Single(piece => piece.Cell == CellOf("f8")).Name,
                promoted.Pieces.Single(piece => piece.Cell == CellOf("f8")).IsMercenary,
                promoted.Pieces.Single(piece => piece.Cell == CellOf("b7")).IsMercenary
            )
        );
    }

    [Fact]
    public async Task Submit_WhenTheTaflKingComesToItsFarCornerOnTheLastDie_WaitsOnItsEscapeOrStaying()
    {
        // Arrange - the Rook moved first, with the 3, so the 5 taking the King to h8 would end the turn
        CreateGameRequest request = new("grand-legions-medium", ZaturnavaServer.OneOpenSeat)
        {
            Scenario = "tafl-escape",
        };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        string black = (await this._client.Claim(created.GameId, 0, null, null, this._cancellation)).Token;
        GameViewDto view = await this._client.Get(created.GameId, black, this._cancellation);
        string CellOf(string name) => $"{view.Cells.Single(cell => cell.Name == name).Id}";
        GameViewDto rookMoved = await this._client.Submit(
            created.GameId,
            black,
            view.LegalActions.First(action => action.Kind == "move" && action.Arguments["from"] == CellOf("c1")),
            this._cancellation
        );

        // Act
        GameViewDto arrived = await this._client.Submit(
            created.GameId,
            black,
            rookMoved.LegalActions.First(action =>
                action.Kind == "move"
                && action.Arguments["from"] == CellOf("h4")
                && action.Arguments["to"] == CellOf("h8")
            ),
            this._cancellation
        );

        // Assert
        string choices = string.Join(
            "|",
            arrived.LegalActions.Select(action => action.Kind == "pass" ? $"pass: {action.Label}" : action.Kind)
        );
        Assert.Equal((CellOf("h8"), "pass: Stay on the board|flee"), ($"{arrived.Fleeing}", choices));
    }

    [Fact]
    public async Task Submit_WhenTheEvangelistStepsInItsScenario_ConvertsTheLinesItClosesButNotTheLeader()
    {
        // Arrange - Black's Evangelist on c4 (the user's report of 2026-11-19: no scenario tried it)
        CreateGameRequest request = new("grand-legions-medium", ZaturnavaServer.OneOpenSeat)
        {
            Scenario = "evangelist",
        };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        string black = (await this._client.Claim(created.GameId, 0, null, null, this._cancellation)).Token;
        GameViewDto view = await this._client.Get(created.GameId, black, this._cancellation);
        string CellOf(string name) => $"{view.Cells.Single(cell => cell.Name == name).Id}";

        // Act
        GameViewDto stepped = await this._client.Submit(
            created.GameId,
            black,
            view.LegalActions.First(action =>
                action.Kind == "move"
                && action.Arguments["from"] == CellOf("c4")
                && action.Arguments["to"] == CellOf("d4")
            ),
            this._cancellation
        );

        // Assert - Yellow's Pawns on e4 and f4 and Red's on e5 Black's, Yellow's King on d5 still Yellow's
        int OwnerAt(string name) => stepped.Pieces.Single(piece => $"{piece.Cell}" == CellOf(name)).Owner;
        Assert.Equal((0, 0, 0, 1), (OwnerAt("e4"), OwnerAt("f4"), OwnerAt("e5"), OwnerAt("d5")));
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
    public async Task RemoveScenarioGames_OnADevelopmentServer_LetsGoOfThemAndListsThemNoMore()
    {
        // Arrange - the user's request of 2026-11-19: to hide scenario games, and be rid of them
        CreateGameRequest request = new("grand-legions-medium", ZaturnavaServer.OneOpenSeat)
        {
            Scenario = "tafl-escape",
        };
        GameCreatedDto created = await this._client.Create(request, this._cancellation);
        GamePageDto before = await this._client.List(
            new GameListQuery(Code: created.GameId, Scenario: true),
            this._cancellation
        );

        // Act
        ScenarioGamesRemovedDto removed = await this._client.RemoveScenarioGames(this._cancellation);

        // Assert
        GamePageDto after = await this._client.List(new GameListQuery(Scenario: true), this._cancellation);
        Assert.Equal(
            (true, true, 0),
            (Assert.Single(before.Games).IsScenario, removed.Removed >= 1, after.Total)
        );
    }

    [Fact]
    public async Task Submit_WhenDiceAreFakedInAGameNotStartedFromAScenario_IsRefusedAsInvalid()
    {
        // Arrange - the user's request of 2026-11-19: faked dice, taken in scenarios only, sent with another game's
        (string gameId, string token, GameViewDto view) = await this.ClaimedGame();
        ActionDto faked = view.LegalActions[0] with { Faked = [6, 6] };

        // Act
        Task<GameViewDto> submitting = this._client.Submit(gameId, token, faked, this._cancellation);

        // Assert
        await AssertRefused(Refusal.Invalid, submitting);
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
    private async Task<(string GameId, string Token)> DraftedGame(
        int? budget = null,
        int? drafter = null,
        string rulesetId = "grand-legions-small",
        bool isSecret = false
    )
    {
        GameCreatedDto created = await this._client.Create(
            new CreateGameRequest(rulesetId, [null, "random", "random", "random"])
            {
                IsDrafted = true,
                DraftBudget = budget,
                Drafter = drafter,
                IsDraftSecret = isSecret,
            },
            this._cancellation
        );
        SeatClaimedDto claimed = await this._client.Claim(created.GameId, 0, null, null, this._cancellation);
        return (created.GameId, claimed.Token);
    }

    /// <summary>
    /// The game after the seat <paramref name="token"/> holds takes the first of its choices that is wanted.
    /// </summary>
    private async Task<GameViewDto> Decided(string gameId, string token, Func<ActionDto, bool> isWanted)
    {
        GameViewDto view = await this._client.Get(gameId, token, this._cancellation);
        return await this._client.Submit(gameId, token, view.LegalActions.First(isWanted), this._cancellation);
    }

    private async Task<(string GameId, string Token, GameViewDto View)> ClaimedGame()
    {
        string gameId = await this.CreateGame();
        SeatClaimedDto claimed = await this._client.Claim(gameId, 0, null, null, this._cancellation);
        GameViewDto view = await this._client.Get(gameId, this._cancellation);
        return (gameId, claimed.Token, view);
    }
}
