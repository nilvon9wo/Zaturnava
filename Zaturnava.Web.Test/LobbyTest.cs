using Bunit;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class LobbyTest : IDisposable
{
    private readonly PageContext _page = new();
    private Seating? _seated;

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenLoaded_OffersANewGameAndTheGamesToJoin()
    {
        // Arrange - nothing beyond the server's catalog and games

        // Act
        IRenderedComponent<Lobby> lobby = this.Render();

        // Assert
        Assert.Equal(
            ("Zaturnava (classic)", 4, "", "random", "Join seat 2", "game-000"),
            (
                lobby.Find("p.rules").TextContent,
                lobby.FindAll("select.plan").Count,
                lobby.FindAll("select.plan")[0].GetAttribute("value"),
                lobby.FindAll("select.plan")[1].GetAttribute("value"),
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
                new[] { null, "random", "random", "random" },
                new Seating(FakeGameServer.GameId, new SeatClaim(0, "secret"))
            ),
            (this._page.Server.Created?.Bots, this._seated),
            strict: true
        );
    }

    [Fact]
    public void Play_WhenEverySeatIsABot_WatchesTheGame()
    {
        // Arrange
        IRenderedComponent<Lobby> lobby = this.Render();
        lobby.FindAll("select.plan")[0].Change("random");

        // Act
        lobby.Find("button.create").Click();

        // Assert
        Assert.Equal(Seating.Watching(FakeGameServer.GameId), this._seated);
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
