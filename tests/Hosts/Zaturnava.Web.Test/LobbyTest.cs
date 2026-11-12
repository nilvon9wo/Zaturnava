using System.Collections.Immutable;
using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class LobbyTest : IDisposable
{
    private static readonly ImmutableList<RulesetDto> GrandSizes =
    [
        Sized("grand-legions", "Grand Legions", "small"),
        Sized("grand-legions", "Grand Legions", "large"),
    ];

    private static readonly ImmutableList<RulesetDto> PreviewSizes =
        [.. GrandSizes.Select(ruleset => ruleset with { IsPreview = true })];

    private readonly PageContext _page = new();
    private Seating? _seated;

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Custom_WhenChosen_OffersANewGameOfOneSizeAndTheGamesToJoin()
    {
        // Arrange - nothing beyond the server's catalog and games
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.custom").Click();

        // Assert
        Assert.Equal(
            ("Chaturaji 2027", 0, 3, "Join as Yellow", "game-000"),
            (
                lobby.Find("p.rules").TextContent,
                lobby.FindAll("select.size, button.seat-by-seat").Count,
                lobby.FindAll("select.strength").Count,
                lobby.Find("button.join").TextContent,
                lobby.Find("span.game").TextContent
            )
        );
    }

    [Fact]
    public void Render_WhenShown_TellsThatCustomIsTheWayToPlayOtherPeople()
    {
        // Arrange - nothing beyond the server's catalog and games

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("Click Custom to play other people.", lobby.Find("p.setup-hint").TextContent.Trim());
    }

    [Fact]
    public void Render_WhenAGameIsAPreview_NeitherOffersNorListsIt()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Chaturaji 2027", false, "chaturaji-2027", false),
            (
                lobby.Find("p.rules").TextContent,
                lobby.Find("input.show-previews").HasAttribute("checked"),
                this._page.Server.Listed?.Game,
                this._page.Server.Listed?.Preview
            )
        );
    }

    [Fact]
    public void Render_WhenNoGameIsAPreview_OffersNoChoiceToShowThem()
    {
        // Arrange - nothing beyond the server's catalog

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll("input.show-previews"));
    }

    [Fact]
    public void ShowPreviews_WhenTicked_OffersAndListsThemNamedSoAndRemembersIt()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.show-previews").Change(true);

        // Assert
        Assert.Equal(
            ("Chaturaji 2027|Grand Legions (preview)", "chaturaji-2027", null, "shown"),
            (
                string.Join('|', lobby.FindAll("select.game option").Select(option => option.TextContent)),
                lobby.Find("select.game").GetAttribute("value"),
                this._page.Server.Listed?.Preview,
                $"{this._page.JSInterop.Invocations["zaturnava.save"][^1].Arguments[1]}"
            )
        );
    }

    [Fact]
    public void ShowPreviews_WhenAPreviewOfSeveralSizesIsChosen_KeepsTheChoiceWithTheGameForTheWholeLobby()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("input.show-previews").Change(true);

        // Act
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal(
            ("game,show-previews", "size", "grand-legions"),
            (
                ClassesOf(lobby, ".lobby-games select, .lobby-games input"),
                ClassesOf(lobby, ".choices select, .choices input"),
                this._page.Server.Listed?.Game
            )
        );
    }

    [Fact]
    public void ShowPreviews_WhenUntickedWithOneChosenAndListed_GoesBackToTheFirstGameShownAndAnyInTheList()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.previews").SetResult("shown");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.last-game").SetResult("grand-legions");
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.filter-size").Change("grand-legions-large");
        string chosen = $"{lobby.Find("select.game").GetAttribute("value")}";

        // Act
        lobby.Find("input.show-previews").Change(false);

        // Assert
        Assert.Equal(
            ("grand-legions", "Chaturaji 2027", Waiting()),
            (chosen, lobby.Find("p.rules").TextContent, this._page.Server.Listed)
        );
    }

    [Fact]
    public void ShowPreviews_WhenUntickedWithAnotherChosenAndListed_KeepsBoth()
    {
        // Arrange
        this._page.Server.MoreRulesets = PreviewSizes;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.previews").SetResult("shown");
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.show-previews").Change(false);

        // Assert
        Assert.Equal(
            ("Chaturaji 2027", Waiting()),
            (lobby.Find("p.rules").TextContent, this._page.Server.Listed)
        );
    }

    [Fact]
    public void Render_WhenEveryGameIsAPreview_OffersThemAllNamedSo()
    {
        // Arrange
        this._page.Server.ArePreviewsOnly = true;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("Chaturaji 2027 (preview)", lobby.Find("p.rules").TextContent);
    }

    [Fact]
    public void Game_WhenOnePlayedAtSeveralSizesIsChosen_OffersItsSizesApart()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal(
            ("Chaturaji 2027|Grand Legions", "small|large", "grand-legions-small"),
            (
                string.Join('|', lobby.FindAll("select.game option").Select(option => option.TextContent)),
                string.Join('|', lobby.FindAll("select.size option").Select(option => option.TextContent)),
                lobby.Find("select.size").GetAttribute("value")
            )
        );
    }

    [Fact]
    public void Game_WhenChosenBackAndForth_KeepsEachGamesSizeChosen()
    {
        // Arrange
        this._page.Server.MoreRulesets =
        [
            .. GrandSizes,
            Sized("other", "Other", "small"),
            Sized("other", "Other", "large"),
        ];
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.game").Change("grand-legions");
        lobby.Find("select.size").Change("grand-legions-large");

        // Act
        lobby.Find("select.game").Change("other");
        string? other = lobby.Find("select.size").GetAttribute("value");
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal(("other-small", "grand-legions-large"), (other, lobby.Find("select.size").GetAttribute("value")));
    }

    [Fact]
    public void Game_WhenPlayedAtThreeSizes_IsSetUpAtTheMiddleOne()
    {
        // Arrange
        this._page.Server.MoreRulesets =
        [
            Sized("grand-legions", "Grand Legions", "small"),
            Sized("grand-legions", "Grand Legions", "medium"),
            Sized("grand-legions", "Grand Legions", "large"),
        ];
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal("grand-legions-medium", lobby.Find("select.size").GetAttribute("value"));
    }

    [Theory]
    [InlineData("grand-legions", "grand-legions")]
    [InlineData("gone", "chaturaji-2027")]
    [InlineData(null, "chaturaji-2027")]
    public void Render_WhenAGameWasStartedBefore_SetsThatGameUpIfStillOffered(string? last, string expected)
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.last-game").SetResult(last);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(expected, lobby.Find("select.game").GetAttribute("value"));
    }

    [Fact]
    public void Render_WhenTheGameWasSetUpBefore_SetsItUpAsLastStarted()
    {
        // Arrange - Green played; Black medium (Midas), Yellow the player's own AI, Red hard, Oracle chosen
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult("chaturaji-2027;2;1,-2,-1,2;,,,oracle");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("2", "Your ally, Black|Enemy, Red|Enemy, Yellow", "1|2|-2", "oracle"),
            (
                lobby.Find("select.colour").GetAttribute("value"),
                string.Join('|', SeatNames(lobby).Split('|').Skip(1)),
                string.Join('|', lobby.FindAll("select.strength").Select(strength => strength.GetAttribute("value"))),
                lobby.Find("select.pick").GetAttribute("value")
            )
        );
    }

    [Theory]
    [InlineData("chaturaji-2027;4;1,0,-1,2;,,,")]
    [InlineData("chaturaji-2027;-1;1,0,-1,2;,,,")]
    [InlineData("chaturaji-2027;0;1,0,2;,,,")]
    [InlineData("chaturaji-2027;0;-1,0,1,2;,,")]
    [InlineData("chaturaji-2027;0;-1,3,0,2;,,,")]
    [InlineData("chaturaji-2027;0;-1,-3,0,2;,,,")]
    [InlineData("chaturaji-2027;0;-1,1,1,2;,,,")]
    [InlineData("gone;0;-1,0,1,2;,,,")]
    [InlineData("chaturaji-2027;0")]
    public void Render_WhenTheSetupRememberedNoLongerFits_SetsTheGameUpAfresh(string remembered)
    {
        // Arrange - a colour, a strength or a bot count the game no longer has; medium (Midas alone) twice
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult(remembered);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("0", "1|0|2"),
            (
                lobby.Find("select.colour").GetAttribute("value"),
                string.Join('|', lobby.FindAll("select.strength").Select(strength => strength.GetAttribute("value")))
            )
        );
    }

    [Fact]
    public void Render_WhenTheBotRememberedIsNoLongerOffered_ChoosesAnyOfItsStrength()
    {
        // Arrange - Red hard, with a bot no longer offered
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup-of.chaturaji-2027")
            .SetResult("chaturaji-2027;0;-1,0,1,2;,,,gone");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(string.Empty, lobby.Find("select.pick").GetAttribute("value"));
    }

    [Theory]
    [InlineData("button.create")]
    [InlineData("button.quick.medium")]
    public void Start_WhenClicked_RemembersTheGameAndHowItWasSetUp(string start)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("select.colour").Change("2");
        lobby.Find(start == "button.create" ? "button.custom" : "button.quick-game").Click();

        // Act
        lobby.Find(start).Click();

        // Assert
        ImmutableDictionary<string, string> saved = this._page.JSInterop.Invocations["zaturnava.save"]
            .GroupBy(save => $"{save.Arguments[0]}", StringComparer.Ordinal)
            .ToImmutableDictionary(group => group.Key, group => $"{group.Last().Arguments[1]}", StringComparer.Ordinal);
        Assert.Equal(
            ("chaturaji-2027", true),
            (
                saved["zaturnava.last-game"],
                saved["zaturnava.setup-of.chaturaji-2027"].StartsWith("chaturaji-2027;2;", StringComparison.Ordinal)
            )
        );
    }

    [Theory]
    [InlineData("player-token", "Bea", null, "player-token")]
    [InlineData(null, "Bea", "alias-1", null)]
    [InlineData(null, null, null, null)]
    public void Play_WhenSignedInOrNot_ClaimsTheSeatAsThePlayerIfAnyAndByTheNicknameAndAliasIfAny(
        string? player,
        string? nickname,
        string? alias,
        string? expected
    )
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult(player);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.nickname").SetResult(nickname);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.alias").SetResult(alias);
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (expected, nickname, alias),
            (this._page.Server.ClaimedAs, this._page.Server.ClaimedBy, this._page.Server.ClaimedWith)
        );
    }

    [Fact]
    public void Play_WhenAScenarioIsChosen_StartsFromItAndDescribesIt()
    {
        // Arrange
        this._page.Server.Scenarios =
        [
            new ScenarioDto("back-to-back", "chaturaji-2027", "Back to back", "Two Pawns face each other."),
            new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles."),
        ];
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("select.scenario").Change("back-to-back");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            ("back-to-back", "Two Pawns face each other."),
            (this._page.Server.Created?.Scenario, lobby.Find("p.scenario-description").TextContent)
        );
    }

    [Fact]
    public void Play_WhenNoScenarioIsChosen_StartsFromTheBeginning()
    {
        // Arrange
        this._page.Server.Scenarios = [new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles.")];
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal((null, 0), (this._page.Server.Created?.Scenario, lobby.FindAll("p.scenario-description").Count));
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    public void Play_WhenAPlayerIsSignedIn_OffersToBarGuests(bool isSignedIn, bool barsGuests, int offered)
    {
        // Arrange
        if (isSignedIn)
        {
            this.SignInAsAdaAtYellow();
        }

        IRenderedComponent<Lobby> lobby = this.ByStrength();
        if (barsGuests)
        {
            lobby.Find("input.players-only").Change(true);
        }

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(
            (offered, barsGuests),
            (lobby.FindAll("input.players-only").Count, this._page.Server.Created!.IsForPlayers)
        );
    }

    [Theory]
    [InlineData(false, true, "Join as Yellow")]
    [InlineData(true, false, "Join as Yellow")]
    [InlineData(true, true, "Sign in to join")]
    public void Render_WhenAGameIsForPlayersOnly_AsksAGuestToSignInToJoin(bool isForPlayers, bool isGuest, string shown)
    {
        // Arrange
        if (!isGuest)
        {
            this.SignInAsAdaAtYellow();
        }

        this._page.Server.Games =
        [
            this._page.Server.Games[0] with { OpenSeats = [1], IsForPlayers = isForPlayers, PlayerIds = [] },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            shown,
            lobby.Find(".panel:not(.awaited) ul.games li :is(button.join, span.for-players)").TextContent.Trim()
        );
    }

    [Theory]
    [InlineData(false, false, null, null, "0 0 0 False False  ")]
    [InlineData(true, false, null, null, "1 2 0 False False  ")]
    [InlineData(true, false, "random-one", null, "1 2 0 True False  ")]
    [InlineData(true, true, null, null, "1 4 1 False True 75 ")]
    [InlineData(true, true, "draft-own", "30", "1 4 1 False True 30 ")]
    [InlineData(true, true, "draft-one", "30", "1 4 1 False True 30 0")]
    [InlineData(true, true, "draft-own", "", "1 4 1 False True  ")]
    [InlineData(true, true, "random-each", null, "1 4 0 False False  ")]
    public void Play_WhenTheArmiesAreDealt_AsksHowTheyComeToBe(
        bool isDealt,
        bool isDraftable,
        string? armies,
        string? budget,
        string expected
    )
    {
        // Arrange - expected: selects, options, budget boxes, alike, drafted, budget, drafter
        this._page.Server.IsDealt = isDealt;
        this._page.Server.IsDraftable = isDraftable;
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        if (armies is not null)
        {
            lobby.Find("select.armies").Change(armies);
        }

        if (budget is not null)
        {
            lobby.Find("input.draft-budget").Change(budget);
        }

        int boxes = lobby.FindAll("input.draft-budget").Count;

        // Act
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest created = this._page.Server.Created!;
        Assert.Equal(
            (expected, (string?)null),
            (
                $"{lobby.FindAll("select.armies").Count} {lobby.FindAll("select.armies option").Count} {boxes} "
                    + $"{created.AreArmiesAlike} {created.IsDrafted} {created.DraftBudget} {created.Drafter}",
                created.ArmiesOf
            )
        );
    }

    [Fact]
    public void Play_WhenTheBoardAndKingdomsAreSet_AsksForThemAndGrowsTheBudgetWithTheKingdoms()
    {
        // Arrange - 75 points for kingdoms of 4 x 3, so 112 for 6 x 3
        this._page.Server.IsDealt = true;
        this._page.Server.IsDraftable = true;
        this._page.Server.SizableBoard = new BoardSizeDto(10, 4, 3);
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        string before = lobby.Find("input.board-side").GetAttribute("value") ?? string.Empty;
        lobby.Find("input.board-side").Change("12");
        lobby.Find("input.kingdom-width").Change("6");
        lobby.Find("input.kingdom-depth").Change("3");
        string budget = lobby.Find("input.draft-budget").GetAttribute("value") ?? string.Empty;

        // Act
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest created = this._page.Server.Created!;
        Assert.Equal(
            ("10", "112", new BoardSizeDto(12, 6, 3), (int?)112),
            (before, budget, created.Board, created.DraftBudget)
        );
    }

    [Fact]
    public void Play_WhenTheKingdomsAreSetInAGameNotDrafted_AsksForNoBudget()
    {
        // Arrange - armies dealt at random, never drafted
        this._page.Server.IsDealt = true;
        this._page.Server.SizableBoard = new BoardSizeDto(10, 4, 3);
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("input.kingdom-width").Change("5");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        CreateGameRequest created = this._page.Server.Created!;
        Assert.Equal((new BoardSizeDto(10, 5, 3), (int?)null), (created.Board, created.DraftBudget));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Quick_WhenTheBoardMayBeSet_StartsOneOnTheBoardAsSet(bool isSizable)
    {
        // Arrange
        this._page.Server.SizableBoard = isSizable ? new BoardSizeDto(10, 4, 3) : null;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.quick.medium").Click();

        // Assert
        Assert.Equal(
            (isSizable ? new BoardSizeDto(10, 4, 3) : null, isSizable ? 1 : 0),
            (this._page.Server.Created!.Board, lobby.FindAll("input.board-side").Count)
        );
    }

    [Fact]
    public void Render_WhenTheArmiesAreDrafted_ShowsTheGamesOwnBudgetToSpend()
    {
        // Arrange - the fake server's drafted game gives each seat 75 points
        this._page.Server.IsDealt = true;
        this._page.Server.IsDraftable = true;

        // Act
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Assert
        Assert.Equal("75", lobby.Find("input.draft-budget").GetAttribute("value"));
    }

    [Fact]
    public void Render_WhenTheServerOffersNoScenarios_OffersNoChoiceOfStart()
    {
        // Arrange - the fake server offers none

        // Act
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Assert
        Assert.Empty(lobby.FindAll("select.scenario"));
    }

    [Fact]
    public void Quick_WhenAnotherGameAndSizeAreChosen_StartsOneOfThatGameAtThatSize()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.game").Change("grand-legions");
        lobby.Find("select.size").Change("grand-legions-large");
        lobby.Find("button.quick.medium").Click();

        // Assert
        Assert.Equal("grand-legions-large", this._page.Server.Created?.RulesetId);
    }

    [Fact]
    public void Render_WhenNeverSetUpBefore_OffersAQuickGameAlone()
    {
        // Arrange
        this._page.Server.Scenarios = [new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles.")];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert - no Play, no plans and no scenario: each difficulty starts a game at once
        Assert.Equal(
            ("true", "Easy|Medium|Hard", 0),
            (
                lobby.Find("button.quick-game").GetAttribute("aria-pressed"),
                string.Join('|', lobby.FindAll("button.quick").Select(quick => quick.TextContent)),
                lobby.FindAll("button.create, select.strength, select.plan, select.scenario").Count
            )
        );
    }

    [Fact]
    public void Render_WhenSetUpBefore_SetsUpAsLastChosen()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult("strength");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("true", true, 0),
            (
                lobby.Find("button.custom").GetAttribute("aria-pressed"),
                lobby.FindAll("select.strength").Count > 0,
                lobby.FindAll("button.quick").Count
            )
        );
    }

    [Fact]
    public void SetUp_WhenAModeIsChosen_RemembersIt()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.custom").Click();
        lobby.Find("button.quick-game").Click();

        // Assert
        Assert.Equal(
            "strength|quick",
            string.Join(
                '|',
                this._page.JSInterop.Invocations["zaturnava.save"]
                    .Where(save => save.Arguments[0] is "zaturnava.setup")
                    .Select(save => save.Arguments[1])
            )
        );
    }

    [Fact]
    public void Quick_WhenClicked_SitsAtSomeSeatWithEachBotOnceAmongTheOthers()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.quick.medium").Click();

        // Assert - never Claude, which is paid for
        ImmutableList<string?> bots = this._page.Server.Created!.Bots;
        Assert.Equal(
            (3, 3, true, false),
            (
                bots.OfType<string>().Count(),
                bots.OfType<string>().Distinct(StringComparer.Ordinal).Count(),
                bots[this._seated!.Claim!.Seat] is null,
                bots.Contains("claude")
            )
        );
    }

    [Theory]
    [InlineData("easy", "midas,random,solomon")]
    [InlineData("hard", "midas,oracle,solomon")]
    public void Quick_WhenEasyOrHard_SeatsTheEasiestOrHardestBots(string difficulty, string expected)
    {
        // Arrange - Random is rated easiest, then Midas, Solomon and Oracle; Claude, paid for, is never seated
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find($"button.quick.{difficulty}").Click();

        // Assert
        Assert.Equal(
            expected,
            string.Join(",", this._page.Server.Created!.Bots.OfType<string>().Order(StringComparer.Ordinal))
        );
    }

    [Fact]
    public void Render_WhenLoaded_OffersByStrengthTheColourThenTheAllyAndEnemiesNearestMediumWithABotLeft()
    {
        // Arrange - Easy is Random alone, Medium Midas alone, Hard Solomon and Oracle; the ally gets Medium, an
        // enemy the easier of the two strengths as near, the other the one left

        // Act
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Assert
        Assert.Equal(
            (
                "You play|Your ally, Green|Enemy, Yellow|Enemy, Red",
                "1|0|2",
                "Person|Your own AI|Easy bot (Elo 1000)|Medium bot (Elo 1100)|Hard bot (Elo 1250–1400)",
                0
            ),
            (
                SeatNames(lobby),
                string.Join('|', lobby.FindAll("select.strength").Select(strength => strength.GetAttribute("value"))),
                string.Join('|', lobby.Find("select.strength").Children.Select(option => option.TextContent.Trim())),
                lobby.FindAll("select.plan").Count
            )
        );
    }

    [Fact]
    public void ByStrength_WhenAStrengthsBotsAllHaveSeats_OffersItToNoOtherSeat()
    {
        // Arrange - Green is Medium, Midas alone; Yellow Easy, Random alone; Red Hard, of two
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.FindAll("select.strength")[1].Change("2");

        // Assert - Hard's two bots seat Yellow and Red, Medium's one Green; Easy's is free again
        Assert.Equal(
            ("hard", "medium", "medium"),
            (
                DisabledLevels(lobby, 0),
                DisabledLevels(lobby, 1),
                DisabledLevels(lobby, 2)
            )
        );
    }

    [Fact]
    public void ByStrength_WhenTheColourLeftHasNoBotLeftAtItsStrength_PlaysItAtTheNearestWithOne()
    {
        // Arrange - Black, the player's, would be Hard; Yellow and Red take both Hard bots
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[1].Change("2");

        // Act - Green, Medium, becomes the player's, and Black is left
        lobby.Find("select.colour").Change("2");

        // Assert - seats in turn after Green: Black, its ally, then Yellow and Red
        Assert.Equal(
            "1|2|2",
            string.Join('|', lobby.FindAll("select.strength").Select(strength => strength.GetAttribute("value")))
        );
    }

    [Fact]
    public void ByStrength_WhenTheColourLeftWasForAPerson_KeepsItForAPerson()
    {
        // Arrange - Yellow is left to a person, then played, then left again
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[1].Change("-1");
        lobby.Find("select.colour").Change("1");

        // Act
        lobby.Find("select.colour").Change("0");

        // Assert
        Assert.Equal("-1", lobby.FindAll("select.strength")[1].GetAttribute("value"));
    }

    [Fact]
    public void Play_WhenASeatIsForTheirOwnAi_LeavesItOpenHavingSaidHowToConnectIt()
    {
        // Arrange - Yellow is left for the player's own AI
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[1].Change("-2");
        string hint = lobby.Find("p.own-ai a").GetAttribute("href")!;

        // Act
        lobby.Find("button.create").Click();

        // Assert - the player's seat and Yellow open; bots in the other two
        Assert.Equal(
            (DocsLinks.Bots, (string?)null, 2),
            (hint, this._page.Server.Created!.Bots[1], this._page.Server.Created.Bots.OfType<string>().Count())
        );
    }

    [Fact]
    public void ByStrength_WhenNoSeatIsForTheirOwnAi_SaysNothingOfConnectingOne()
    {
        // Arrange - nothing beyond the seats set up by strength

        // Act
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Assert
        Assert.Empty(lobby.FindAll("p.own-ai"));
    }

    [Fact]
    public void Play_WhenABotWasChosenForTheColourTakenSinceAndForAnotherSeat_SeatsItOnce()
    {
        // Arrange - Oracle chosen for Red; Red taken, so Black, left, may choose it; then Green taken instead
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.pick")[0].Change("oracle");
        lobby.Find("select.colour").Change("3");
        lobby.FindAll("select.pick")[0].Change("oracle");
        lobby.Find("select.colour").Change("2");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        ImmutableList<string> bots = [.. this._page.Server.Created!.Bots.OfType<string>()];
        Assert.Equal(bots.Count, bots.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ByStrength_WhenAnotherColourIsChosen_PutsItsAllyFirstThenItsEnemiesInTurn()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("select.colour").Change("1");

        // Assert
        Assert.Equal("You play|Your ally, Red|Enemy, Green|Enemy, Black", SeatNames(lobby));
    }

    [Fact]
    public void Play_WhenSetUpByStrength_SitsAtTheColourWithBotsAsStrongAsChosen()
    {
        // Arrange - Green's ally is Black, a person; Yellow is easy (Random), Red hard (Solomon or Oracle)
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.Find("select.colour").Change("2");
        lobby.FindAll("select.strength")[0].Change("-1");
        lobby.FindAll("select.strength")[1].Change("2");
        lobby.FindAll("select.strength")[2].Change("0");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        ImmutableList<string?> bots = this._page.Server.Created!.Bots;
        Assert.Equal(
            ("random", true, 2),
            (bots[1], (bots[0], bots[2], bots[3]) is (null, null, "solomon" or "oracle"), this._seated!.Claim!.Seat)
        );
    }

    [Fact]
    public void ByStrength_WhenAStrengthHasSeveralBots_OffersAChoiceOfThemOrAny()
    {
        // Arrange - Black plays; the seats go Green, Yellow, Red; medium is Midas alone, hard Solomon and Oracle
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.FindAll("select.strength")[2].Change("2");

        // Assert
        Assert.Equal(
            (1, "Any of them|Solomon (Elo 1250)|Oracle (Elo 1400)"),
            (
                lobby.FindAll("select.pick").Count,
                string.Join('|', lobby.Find("select.pick").Children.Select(option => option.TextContent.Trim()))
            )
        );
    }

    [Fact]
    public void Play_WhenABotOfTheStrengthIsChosen_SeatsThatOne()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[2].Change("2");
        lobby.Find("select.pick").Change("oracle");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal("oracle", this._page.Server.Created!.Bots[3]);
    }

    [Fact]
    public void ByStrength_WhenABotIsChosenForOneSeat_CannotBeChosenForAnother()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[1].Change("2");
        lobby.FindAll("select.strength")[2].Change("2");

        // Act
        lobby.FindAll("select.pick")[1].Change("oracle");

        // Assert
        Assert.Equal(
            (true, false),
            (
                lobby.FindAll("select.pick")[0].QuerySelector("option[value=oracle]")!.HasAttribute("disabled"),
                lobby.FindAll("select.pick")[1].QuerySelector("option[value=oracle]")!.HasAttribute("disabled")
            )
        );
    }

    [Fact]
    public void ByStrength_WhenTheStrengthChanges_ForgetsTheBotChosen()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.ByStrength();
        lobby.FindAll("select.strength")[2].Change("2");
        lobby.Find("select.pick").Change("oracle");

        // Act
        lobby.FindAll("select.strength")[2].Change("-1");
        lobby.FindAll("select.strength")[2].Change("2");

        // Assert
        Assert.Equal(string.Empty, lobby.Find("select.pick").GetAttribute("value"));
    }

    [Fact]
    public void Custom_WhenChosenAfterQuick_OffersEachSeatsStrengthInsteadOfQuickGames()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.custom").Click();

        // Assert
        Assert.Equal((0, 3), (lobby.FindAll("button.quick").Count, lobby.FindAll("select.strength").Count));
    }

    [Fact]
    public void Join_WhenClicked_ClaimsThatSeat()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.join").Click();

        // Assert
        Assert.Equal(new Seating(FakeGameServer.GameId, new SeatClaim(1, "secret")), this._seated);
    }

    [Fact]
    public void Watch_WhenClicked_WatchesTheGame()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.watch").Click();

        // Assert
        Assert.Equal(Seating.Watching(FakeGameServer.GameId), this._seated);
    }

    [Theory]
    [InlineData(12, "12 moves")]
    [InlineData(1, "1 move")]
    public void Render_WhenGamesAreListed_SaysWhoPlaysAndHowFarAlongEachIs(int moves, string expected)
    {
        // Arrange
        this._page.Server.Games = [this._page.Server.Games[0] with { Moves = moves }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Black: Ada, Yellow: open, Green: random, Red: open", expected),
            (lobby.Find("span.players").TextContent, lobby.Find("span.moves").TextContent)
        );
    }

    [Fact]
    public void Resume_WhenThisBrowserHeldASeat_GoesBackToIt()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.resume").Click();

        // Assert
        Assert.Equal(new Seating(FakeGameServer.GameId, new SeatClaim(2, "kept")), this._seated);
    }

    [Fact]
    public void TakeBack_WhenThePlayerSignedInClaimedASeatThisBrowserDoesNotHold_TakesItBackForANewToken()
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.take-back").Click();

        // Assert
        Assert.Equal(
            ("Back to Yellow", new Seating(FakeGameServer.GameId, new SeatClaim(1, FakeGameServer.Token))),
            (lobby.Find("button.take-back").TextContent.Trim(), this._seated)
        );
        Assert.Contains($"take back {FakeGameServer.GameId} 1 player", this._page.Server.Calls);
    }

    [Theory]
    [InlineData(null, "take back", FakeGameServer.Token)]
    [InlineData("1 kept", "resume", "kept")]
    [InlineData("2 kept", "take back", FakeGameServer.Token)]
    public void BackTo_WhenAGameWaitsForThePlayer_GoesBackToTheSeatHeldHereElseTakesItBack(
        string? held,
        string how,
        string token
    )
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        this._page.Server.Awaited = this._page.Server.Awaited.SetItem(
            "player",
            [new AwaitedGameDto(this._page.Server.Games[0], 1)]
        );
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult(held);
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.awaited-resume").Click();

        // Assert
        Assert.Equal(
            ("Back to Yellow", new Seating(FakeGameServer.GameId, new SeatClaim(1, token)), how == "take back"),
            (
                lobby.Find("button.awaited-resume").TextContent.Trim(),
                this._seated,
                this._page.Server.Calls.Contains($"take back {FakeGameServer.GameId} 1 player")
            )
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_WhenNoGameWaitsForThePlayer_ShowsNoneWaiting(bool isKnown)
    {
        // Arrange - the player's games told, none; or not told, the token standing for no one by then
        this.SignInAsAdaAtYellow();
        this._page.Server.Awaited = isKnown ? [] : this._page.Server.Awaited.SetItem("player", null);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll(".awaited"));
    }

    [Fact]
    public void Render_WhenThePlayerSignedInPlaysNoSeat_OffersNoneToTakeBack()
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        this._page.Server.Players = this._page.Server.Players.SetItem("player", new PlayerDto("bob", "Bob"));

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll("button.take-back"));
    }

    [Fact]
    public void TakeBack_WhenRefused_SaysWhyAndStaysInTheLobby()
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        IRenderedComponent<Lobby> lobby = this.Render();
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Forbidden, "Not your seat.");

        // Act
        lobby.Find("button.take-back").Click();

        // Assert
        Assert.Equal(
            ((Seating?)null, "Not your seat."),
            (this._seated, lobby.Find("p.problem").TextContent)
        );
    }

    [Fact]
    public void Resume_WhenThePlayerSignedInOrChoseANicknameSince_SaysSoAtTheSeat()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult("player");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.nickname").SetResult("Ada");
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.resume").Click();

        // Assert
        Assert.Equal([$"{FakeGameServer.GameId} 2 kept player Ada"], this._page.Server.Identified);
    }

    [Fact]
    public void Resume_WhenTheSeatCannotBeSaidToBeHeldAnew_GoesBackToItAsItWas()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");
        IRenderedComponent<Lobby> lobby = this.Render();
        this._page.Server.Failure = new GameRequestRefusedException(Refusal.Forbidden, "Signed out.");

        // Act
        lobby.Find("button.resume").Click();

        // Assert
        Assert.Equal(
            (new Seating(FakeGameServer.GameId, new SeatClaim(2, "kept")), (string?)null),
            (this._seated, lobby.FindAll("p.problem").Count == 0 ? null : "a problem shown")
        );
    }

    [Fact]
    public void Render_WhenThisBrowserHeldASeat_NamesIt()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("Back to Green", lobby.Find("button.resume").TextContent);
    }

    [Fact]
    public void Render_WhenAGameIsOver_OffersNothingToDoInIt()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.seat." + FakeGameServer.GameId)
            .SetResult("2 kept");
        this._page.Server.Games = [this._page.Server.Games[0] with { IsOver = true }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Over", 0),
            (
                lobby.Find("span.over").TextContent,
                lobby.FindAll("button.watch, button.join, button.resume").Count
            )
        );
    }

    [Fact]
    public void Render_WhenTheRulesetNamesNoSeats_NumbersThem()
    {
        // Arrange
        this._page.Server.Games = [this._page.Server.Games[0] with { SeatNames = [] }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("Join as seat 2", lobby.Find("button.join").TextContent);
    }

    [Fact]
    public void Render_WhenThisBrowserHeldNoSeat_OffersNoReturn()
    {
        // Arrange - nothing remembered

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll("button.resume"));
    }

    [Fact]
    public void Render_WhenTheServerCannotBeReached_SaysWhy()
    {
        // Arrange
        this._page.Server.Failure = new HttpRequestException("The server is asleep.");

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("The server is asleep.", 0),
            (lobby.Find("p.problem").TextContent, lobby.FindAll("button").Count)
        );
    }

    [Fact]
    public void Render_WhenLoaded_ListsTheFirstPageOfTheGamesWaitingForPlayers()
    {
        // Arrange - nothing beyond the server

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            (Waiting(), "waiting", 0),
            (this._page.Server.Listed, lobby.Find("select.filter-state").GetAttribute("value"), Pages(lobby))
        );
    }

    [Theory]
    [InlineData("waiting", true)]
    [InlineData("in-progress", false)]
    [InlineData("not-over", null)]
    public void FilterState_WhenChosen_ListsTheGamesWaitingForPlayersOrUnderWayOrEither(string state, bool? open)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.filter-state").Change(state);

        // Assert
        Assert.Equal(
            (Waiting() with { Open = open }, state),
            (this._page.Server.Listed, lobby.Find("select.filter-state").GetAttribute("value"))
        );
    }

    [Fact]
    public void FilterState_WhenAPlayerSignedInChoosesMine_ListsTheirGamesUntilAnotherIsChosen()
    {
        // Arrange
        this.SignInAsAdaAtYellow();
        IRenderedComponent<Lobby> lobby = this.Render();
        string offered =
            string.Join('|', lobby.FindAll("select.filter-state option").Select(option => option.TextContent));

        // Act
        lobby.Find("select.filter-state").Change("mine");
        (GameListQuery?, string?) mine =
            (this._page.Server.Listed, lobby.Find("select.filter-state").GetAttribute("value"));
        lobby.Find("select.filter-state").Change("waiting");

        // Assert
        Assert.Equal(
            (
                "Any not over|Waiting for players|In progress|Mine",
                (Waiting() with { Open = null, Player = "ada" }, "mine"),
                Waiting()
            ),
            (offered, mine, this._page.Server.Listed)
        );
    }

    [Fact]
    public void FilterState_WhenRendered_OffersNoGamesOver()
    {
        // Arrange - nothing beyond the server

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            "Any not over|Waiting for players|In progress",
            string.Join('|', lobby.FindAll("select.filter-state option").Select(option => option.TextContent))
        );
    }

    [Fact]
    public void Game_WhenRendered_OffersEachGameOnceWhateverItsSizesAndListsTheOneChosenWithoutAChoiceOfSize()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Chaturaji 2027|Grand Legions", 0, 1, Waiting()),
            (
                string.Join('|', lobby.FindAll("select.game option").Select(option => option.TextContent)),
                lobby.FindAll("select.filter-size").Count,
                lobby.FindAll("select.game").Count,
                this._page.Server.Listed
            )
        );
    }

    [Fact]
    public void Game_WhenOnePlayedAtSeveralSizesIsChosen_ListsItAtAnySizeAndShowsItsLeaderboard()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.game").Change("grand-legions");

        // Assert
        Assert.Equal(
            (Waiting() with { Game = "grand-legions" }, "Any|small|large", "leaderboard grand-legions"),
            (
                this._page.Server.Listed,
                string.Join('|', lobby.FindAll("select.filter-size option").Select(option => option.TextContent)),
                this._page.Server.Calls.Last(call => call.StartsWith("leaderboard", StringComparison.Ordinal))
            )
        );
    }

    [Theory]
    [InlineData("grand-legions-large", "grand-legions-large")]
    [InlineData("", null)]
    public void FilterSize_WhenChosen_ListsTheGameAtThatSizeOrAny(string chosen, string? expected)
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.game").Change("grand-legions");

        // Act
        lobby.Find("select.filter-size").Change(chosen);

        // Assert
        Assert.Equal(Waiting() with { Ruleset = expected, Game = "grand-legions" }, this._page.Server.Listed);
    }

    [Fact]
    public void Game_WhenOneOfOneSizeIsChosenAfterASize_ListsItAndOffersNoSize()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.game").Change("grand-legions");
        lobby.Find("select.filter-size").Change("grand-legions-large");

        // Act
        lobby.Find("select.game").Change("chaturaji-2027");

        // Assert
        Assert.Equal((Waiting(), 0), (this._page.Server.Listed, lobby.FindAll("select.filter-size").Count));
    }

    [Theory]
    [InlineData("chaturaji-2027", "Chaturaji 2027")]
    [InlineData("retired", "retired")]
    public void Render_WhenGamesAreListed_NamesEachOnesGameAndSize(string rulesetId, string expected)
    {
        // Arrange
        this._page.Server.Games = [this._page.Server.Games[0] with { RulesetId = rulesetId }];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(expected, lobby.Find("span.ruleset").TextContent);
    }

    [Theory]
    [InlineData("1", 12, false, "Waiting for a player")]
    [InlineData("1,3", 12, false, "Waiting for players")]
    [InlineData("", 0, false, "Not started")]
    [InlineData("", 12, false, "")]
    [InlineData("1", 0, true, "")]
    public void Render_WhenGamesAreListed_SaysWhetherEachWaitsOrHasNotStarted(
        string open,
        int moves,
        bool isOver,
        string expected
    )
    {
        // Arrange
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with
            {
                OpenSeats = [.. open.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)],
                Moves = moves,
                IsOver = isOver,
            },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(expected, lobby.Find("span.state").TextContent);
    }

    [Theory]
    [InlineData("1", false, "Yellow away")]
    [InlineData("1,3", false, "Yellow and Red away")]
    [InlineData("1", true, "")]
    public void Render_WhenSeatsAtAGameAreAway_SaysWhoUnlessItIsOver(string away, bool isOver, string expected)
    {
        // Arrange
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with
            {
                OpenSeats = [],
                Moves = 12,
                IsOver = isOver,
                AwaySeats = [.. away.Split(',').Select(int.Parse)],
            },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(expected, lobby.Find("span.state").TextContent);
    }

    [Fact]
    public void Render_WhenAGamesTimesWereKept_SaysWhenItWasCreatedAndLastChanged()
    {
        // Arrange - well inside the hour and the minute they round down to, however slow the test
        DateTimeOffset now = DateTimeOffset.UtcNow;
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with
            {
                OpenSeats = [],
                Created = now.AddMinutes(-210),
                LastChanged = now.AddSeconds(-330),
            },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("created 3 h ago, last change 5 min ago", lobby.Find("span.state").TextContent);
    }

    [Fact]
    public void Render_WhenAWaitingGameWasCreatedJustNow_SaysBoth()
    {
        // Arrange
        this._page.Server.Games =
        [
            this._page.Server.Games[0] with { Created = DateTimeOffset.UtcNow },
        ];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("Waiting for players; created just now", lobby.Find("span.state").TextContent);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, null)]
    public void FilterPeople_WhenTicked_ListsOnlyGamesWithNoBotsOrAny(bool ticked, bool? expected)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.filter-people").Change(ticked);

        // Assert
        Assert.Equal(Waiting() with { PeopleOnly = expected }, this._page.Server.Listed);
    }

    [Theory]
    [InlineData("Bob, Jo*", "Bob, Jo*")]
    [InlineData("", null)]
    public void FilterPlayers_WhenSomeoneIsNamed_ListsOnlyGamesTheyAreAllAtOrAny(string named, string? expected)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.filter-players").Change(named);

        // Assert
        Assert.Equal(Waiting() with { Players = expected }, this._page.Server.Listed);
    }

    [Fact]
    public void Render_WhenNoGameMatches_SaysSo()
    {
        // Arrange
        this._page.Server.Games = [];

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal("No games like that.", lobby.Find("p.no-games").TextContent);
    }

    [Fact]
    public void Render_WhenMoreGamesMatchThanAPageHolds_OffersOnlyTheNextPage()
    {
        // Arrange
        this._page.Server.Total = 45;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Page 1 of 3 (45 games)", true, false),
            (
                lobby.Find("span.page-number").TextContent,
                lobby.Find("button.previous").HasAttribute("disabled"),
                lobby.Find("button.next").HasAttribute("disabled")
            )
        );
    }

    [Fact]
    public void Next_WhenClicked_ListsTheNextPage()
    {
        // Arrange
        this._page.Server.Total = 45;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.next").Click();

        // Assert
        Assert.Equal(
            (Waiting() with { Page = 2 }, "Page 2 of 3 (45 games)"),
            (this._page.Server.Listed, lobby.Find("span.page-number").TextContent)
        );
    }

    [Fact]
    public void Previous_WhenClickedOnTheLastPage_ListsThePageBeforeAndAFilterListsTheFirstAgain()
    {
        // Arrange
        this._page.Server.Total = 45;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.next").Click();
        lobby.Find("button.next").Click();
        bool isLast = lobby.Find("button.next").HasAttribute("disabled");

        // Act
        lobby.Find("button.previous").Click();
        GameListQuery? turned = this._page.Server.Listed;
        lobby.Find("select.filter-state").Change("in-progress");

        // Assert
        Assert.Equal(
            (true, 2, null),
            (isLast, turned?.Page, this._page.Server.Listed?.Page)
        );
    }

    [Fact]
    public void Filter_WhenTheServerCannotBeReached_SaysWhy()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();
        this._page.Server.Failure = new HttpRequestException("The server is asleep.");

        // Act
        lobby.Find("select.filter-state").Change("in-progress");

        // Assert
        Assert.Equal("The server is asleep.", lobby.Find("p.problem").TextContent);
    }

    private static int Pages(IRenderedComponent<Lobby> lobby) => lobby.FindAll("div.pages").Count;

    /// <summary>
    /// What the list asks for until the player filters it otherwise: the games waiting for players, of the game
    /// chosen, Chaturaji 2027 unless another.
    /// </summary>
    private static GameListQuery Waiting() => new(Open: true, Over: false, Game: "chaturaji-2027", Preview: false);

    /// <summary>The classes of the fields <paramref name="selector"/> finds, in order, by commas.</summary>
    private static string ClassesOf(IRenderedComponent<Lobby> lobby, string selector) =>
        string.Join(",", lobby.FindAll(selector).Select(field => field.ClassName));

    /// <summary>A ruleset that is one size of a game played at several: "Grand Legions (small)".</summary>
    private static RulesetDto Sized(string game, string gameName, string size) =>
        new($"{game}-{size}", $"{gameName} ({size})", 4) { Game = game, GameName = gameName, Size = size };

    private static string SeatNames(IRenderedComponent<Lobby> lobby) =>
        string.Join('|', lobby.FindAll("span.seat-name").Select(name => name.TextContent.Trim()));

    /// <summary>"easy,medium": the strengths the seat's choice offers but may not be chosen.</summary>
    private static string DisabledLevels(IRenderedComponent<Lobby> lobby, int seat) =>
        string.Join(
            ',',
            lobby.FindAll("select.strength")[seat].QuerySelectorAll("option[disabled]")
                .Select(option => option.TextContent.Trim().Split(' ')[0].ToLowerInvariant())
        );

    private IRenderedComponent<Lobby> ByStrength()
    {
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.custom").Click();
        return lobby;
    }

    private IRenderedComponent<Lobby> Render() =>
        this._page.Render<Lobby>(parameters => parameters.Add(x => x.OnSeated, seating => this._seated = seating));

    /// <summary>Ada signed in, by the token "player", playing Yellow themselves at the game listed.</summary>
    private void SignInAsAdaAtYellow()
    {
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult("player");
        this._page.Server.Players = this._page.Server.Players.SetItem("player", new PlayerDto("ada", "Ada"));
        this._page.Server.Games =
        [
            .. this._page.Server.Games.Select(game => game with { PlayerIds = [null, "ada", null, null] }),
        ];
    }
}
