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
    public void Render_WhenLoaded_OffersANewGameAndTheGamesToJoin()
    {
        // Arrange - nothing beyond the server's catalog and games; Claude, paid for, is not seated unasked

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

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
        IRenderedComponent<Lobby> lobby = this.Render();

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

    [Fact]
    public void Play_WhenAScenarioIsChosen_StartsFromItAndDescribesIt()
    {
        // Arrange
        this._page.Server.Scenarios =
        [
            new ScenarioDto("back-to-back", "chaturaji-2027", "Back to back", "Two Pawns face each other."),
            new ScenarioDto("escape", "chaturaji-2027", "Escape", "Doubles."),
        ];
        IRenderedComponent<Lobby> lobby = this.Render();
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
        IRenderedComponent<Lobby> lobby = this.Render();

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
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Empty(lobby.FindAll("select.scenario"));
    }

    [Fact]
    public void Play_WhenAPaidBotIsChosen_SeatsIt()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();
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
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.FindAll("select.plan")[0].Change("solomon");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(Seating.Watching(FakeGameServer.GameId), this._seated);
    }

    [Fact]
    public void Render_WhenABotPlaysOneSeat_CannotBeChosenForAnother()
    {
        // Arrange - nothing beyond the server's catalog

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

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
            ("Black: person, Yellow: open, Green: random, Red: open", expected),
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

    private IRenderedComponent<Lobby> Render() =>
        this._page.Render<Lobby>(parameters => parameters.Add(x => x.OnSeated, seating => this._seated = seating));
}
