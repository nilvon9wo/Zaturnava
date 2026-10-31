using System.Collections.Immutable;
using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class LobbyTest : IDisposable
{
    private readonly PageContext _page = new();
    private Seating? _seated;

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void SeatBySeat_WhenChosen_OffersANewGameAndTheGamesToJoin()
    {
        // Arrange - nothing beyond the server's catalog and games; Claude, paid for, is not seated unasked
        IRenderedComponent<Lobby> lobby = this.Render();

        // Act
        lobby.Find("button.seat-by-seat").Click();

        // Assert
        Assert.Equal(
            ("Chaturaji 2027", "Black|Yellow|Green|Red", "|random|oracle|midas", "Join as Yellow", "game-000"),
            (
                lobby.Find("p.rules").TextContent,
                string.Join('|', lobby.FindAll("span.seat-name").Select(name => name.TextContent)),
                string.Join('|', lobby.FindAll("select.plan").Select(plan => plan.GetAttribute("value"))),
                lobby.Find("button.join").TextContent,
                lobby.Find("span.game").TextContent
            )
        );
    }

    [Fact]
    public void Play_WhenTheFirstSeatIsForAPerson_CreatesTheGameAndSitsThere()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equivalent(
            (
                new[] { null, "random", "oracle", "midas" },
                new Seating(FakeGameServer.GameId, new SeatClaim(0, "secret"))
            ),
            (this._page.Server.Created?.Bots, this._seated),
            strict: true
        );
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
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();

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
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();
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
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();

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
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();

        // Assert
        Assert.Empty(lobby.FindAll("select.scenario"));
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

    [Theory]
    [InlineData("strength", "button.by-strength", "select.strength")]
    [InlineData("seats", "button.seat-by-seat", "select.plan")]
    public void Render_WhenSetUpBefore_SetsUpAsLastChosen(string remembered, string mode, string plan)
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.setup").SetResult(remembered);

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("true", true, 0),
            (
                lobby.Find(mode).GetAttribute("aria-pressed"),
                lobby.FindAll(plan).Count > 0,
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
        lobby.Find("button.seat-by-seat").Click();
        lobby.Find("button.quick-game").Click();

        // Assert
        Assert.Equal(
            "seats|quick",
            string.Join(
                '|',
                this._page.JSInterop.Invocations["zaturnava.save"]
                    .Where(save => save.Arguments[0] is "zaturnava.setup")
                    .Select(save => save.Arguments[1])
            )
        );
    }

    [Fact]
    public void Play_WhenAPaidBotIsChosen_SeatsIt()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();
        lobby.FindAll("select.plan")[3].Change("claude");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal("claude", this._page.Server.Created!.Bots[3]);
    }

    [Fact]
    public void Play_WhenEverySeatIsABot_WatchesTheGame()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();
        lobby.FindAll("select.plan")[0].Change("solomon");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(Seating.Watching(FakeGameServer.GameId), this._seated);
    }

    [Fact]
    public void SeatBySeat_WhenABotPlaysOneSeat_CannotBeChosenForAnother()
    {
        // Arrange - nothing beyond the server's catalog

        // Act
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();

        // Assert - Random plays seat 2, so seat 1 cannot have it; seat 2 can
        Assert.Equal(
            (true, false),
            (
                lobby.FindAll("select.plan")[0].QuerySelector("option[value=random]")!.HasAttribute("disabled"),
                lobby.FindAll("select.plan")[1].QuerySelector("option[value=random]")!.HasAttribute("disabled")
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
    public void SeatBySeat_WhenChosen_NamesEachBotWithItsRating()
    {
        // Arrange - nothing beyond the server's catalog

        // Act
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();

        // Assert
        Assert.Equal(
            ("Bot: Random (Elo 1000)", "Bot: Claude (AI, costs tokens)"),
            (
                lobby.Find("select.plan option[value=random]").TextContent.Trim(),
                lobby.Find("select.plan option[value=claude]").TextContent.Trim()
            )
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
                "Person|Easy bot (Elo 1000)|Medium bot (Elo 1100)|Hard bot (Elo 1250–1400)",
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
    public void ByStrength_WhenChosenAgain_OffersEachSeatsStrength()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.SeatBySeat();

        // Act
        lobby.Find("button.by-strength").Click();

        // Assert
        Assert.Equal((0, 3), (lobby.FindAll("select.plan").Count, lobby.FindAll("select.strength").Count));
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
        lobby.Find("button.by-strength").Click();
        return lobby;
    }

    private IRenderedComponent<Lobby> SeatBySeat()
    {
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.Find("button.seat-by-seat").Click();
        return lobby;
    }

    private IRenderedComponent<Lobby> Render() =>
        this._page.Render<Lobby>(parameters => parameters.Add(x => x.OnSeated, seating => this._seated = seating));
}
