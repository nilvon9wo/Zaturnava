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
    public void Game_WhenChosenBackAndForth_KeepsTheSizeChosen()
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

        // Assert
        Assert.Equal("other-large", lobby.Find("select.size").GetAttribute("value"));
    }

    [Theory]
    [InlineData("player-token", "Bea", "player-token")]
    [InlineData(null, "Bea", null)]
    [InlineData(null, null, null)]
    public void Play_WhenSignedInOrNot_ClaimsTheSeatAsThePlayerIfAnyAndByTheNicknameIfAny(
        string? player,
        string? nickname,
        string? expected
    )
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.player").SetResult(player);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.nickname").SetResult(nickname);
        IRenderedComponent<Lobby> lobby = this.ByStrength();

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal((expected, nickname), (this._page.Server.ClaimedAs, this._page.Server.ClaimedBy));
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
    public void Render_WhenLoaded_ListsTheFirstPageOfTheGamesNotOver()
    {
        // Arrange - nothing beyond the server

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            (new GameListQuery(Over: false), "not-over", 0),
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
            (new GameListQuery(Open: open, Over: false), state),
            (this._page.Server.Listed, lobby.Find("select.filter-state").GetAttribute("value"))
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
    public void FilterGame_WhenRendered_OffersEachGameOnceWhateverItsSizes()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Any|Chaturaji 2027|Grand Legions", 0),
            (
                string.Join('|', lobby.FindAll("select.filter-game option").Select(option => option.TextContent)),
                lobby.FindAll("select.filter-size").Count
            )
        );
    }

    [Fact]
    public void FilterGame_WhenAGamePlayedAtSeveralSizesIsChosen_ListsItAtAnySizeAndOffersItsSizes()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("select.filter-game").Change("grand-legions");

        // Assert
        Assert.Equal(
            (new GameListQuery(Over: false, Game: "grand-legions"), "Any|small|large"),
            (
                this._page.Server.Listed,
                string.Join('|', lobby.FindAll("select.filter-size option").Select(option => option.TextContent))
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
        lobby.Find("select.filter-game").Change("grand-legions");

        // Act
        lobby.Find("select.filter-size").Change(chosen);

        // Assert
        Assert.Equal(new GameListQuery(expected, Over: false, Game: "grand-legions"), this._page.Server.Listed);
    }

    [Fact]
    public void FilterGame_WhenAGameOfOneSizeIsChosen_ListsItAndOffersNoSize()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.filter-game").Change("grand-legions");
        lobby.Find("select.filter-size").Change("grand-legions-large");

        // Act
        lobby.Find("select.filter-game").Change("chaturaji-2027");

        // Assert
        Assert.Equal(
            (new GameListQuery(Over: false, Game: "chaturaji-2027"), 0),
            (this._page.Server.Listed, lobby.FindAll("select.filter-size").Count)
        );
    }

    [Fact]
    public void FilterGame_WhenAnyIsChosen_ListsEveryGame()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("select.filter-game").Change("chaturaji-2027");

        // Act
        lobby.Find("select.filter-game").Change(string.Empty);

        // Assert
        Assert.Equal(new GameListQuery(Over: false), this._page.Server.Listed);
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
    [InlineData(true, true)]
    [InlineData(false, null)]
    public void FilterPeople_WhenTicked_ListsOnlyGamesWithNoBotsOrAny(bool ticked, bool? expected)
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("input.filter-people").Change(ticked);

        // Assert
        Assert.Equal(new GameListQuery(PeopleOnly: expected, Over: false), this._page.Server.Listed);
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
            (new GameListQuery(Over: false, Page: 2), "Page 2 of 3 (45 games)"),
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
}
