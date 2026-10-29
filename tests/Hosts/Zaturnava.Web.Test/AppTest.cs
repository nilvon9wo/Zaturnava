using Bunit;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class AppTest : IDisposable
{
    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Play_WhenClicked_SitsThePlayerAtTheTable()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.create").Click();

        // Assert
        Assert.Equal((1, 0), (app.FindAll("section.table").Count, app.FindAll("section.lobby").Count));
    }

    [Fact]
    public void Lobby_WhenClickedAtTheTable_ReturnsToTheLobby()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();
        app.Find("button.create").Click();

        // Act
        app.Find("button.leave").Click();

        // Assert
        Assert.Equal((0, 1), (app.FindAll("section.table").Count, app.FindAll("section.lobby").Count));
    }

    [Fact]
    public void HowToPlay_WhenClicked_OpensTheGuideUntilClosed()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.how-to-play").Click();
        string title = app.Find(".guide h2").TextContent;
        app.Find(".guide button.close").Click();

        // Assert
        Assert.Equal(("How to play", 0), (title, app.FindAll(".guide").Count));
    }

    [Fact]
    public void HowToPlay_WhenOpen_LinksToTheFullRulesAndToBringingYourOwnAi()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.how-to-play").Click();

        // Assert
        Assert.Equal(
            [DocsLinks.Rules, DocsLinks.Bots],
            app.FindAll(".guide .more a").Select(link => link.GetAttribute("href"))
        );
    }

    [Fact]
    public void TheBots_WhenClicked_OpensTheGuideToTheBotsUntilClosed()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.the-bots").Click();
        string title = app.Find(".bot-guide h2").TextContent;
        app.Find(".bot-guide button.close").Click();

        // Assert
        Assert.Equal(("The bots", 0), (title, app.FindAll(".bot-guide").Count));
    }

    [Fact]
    public void Play_WhenSeated_ShowsHowToPlayBesideTheGamesNameAndTheLooksBeforeTheSound()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.create").Click();

        // Assert
        Assert.Equal(
            ("brand,how-to-play,the-bots,leave", "safety,theme,pieces,sound"),
            (
                string.Join(",", app.FindAll(".table-bar .where > *").Select(part => part.ClassName)),
                string.Join(",", app.FindAll(".table-bar .settings button").Select(button => button.ClassName))
            )
        );
    }

    [Fact]
    public void Play_WhenThePlayerTakesASeat_RemembersIt()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.create").Click();

        // Assert
        Assert.Equal(
            ["zaturnava.seat." + FakeGameServer.GameId, "0 " + FakeGameServer.Token],
            this.SeatsRemembered().Single().Arguments
        );
    }

    [Fact]
    public void Watch_WhenClicked_RemembersNoSeat()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.watch").Click();

        // Assert
        Assert.Empty(this.SeatsRemembered());
    }

    [Fact]
    public void ThemeButton_WhenClicked_SwitchesBetweenDarkAndLight()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.theme").Click();
        string afterOne = app.Find("button.theme").TextContent;
        app.Find("button.theme").Click();

        // Assert
        Assert.Equal(
            ("Dark theme", "Light theme", "dark,light,dark"),
            (
                afterOne,
                app.Find("button.theme").TextContent,
                string.Join(",", this._page.JSInterop.Invocations["zaturnava.theme"].Select(call => call.Arguments[0]))
            )
        );
    }

    [Fact]
    public void SafetyButton_WhenClicked_TurnsTheSafetyOffAndOn()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.safety").Click();
        string afterOne = $"{app.Find("button.safety").TextContent.Trim()} ({app.Find("button.safety").ClassName})";
        app.Find("button.safety").Click();

        // Assert
        Assert.Equal(
            ("Safety: OFF (safety off)", "Safety: on"),
            (afterOne, app.Find("button.safety").TextContent.Trim())
        );
    }

    [Fact]
    public void SoundButton_WhenClicked_TurnsTheSoundOffAndOn()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.sound").Click();
        string afterOne = app.Find("button.sound").TextContent;
        app.Find("button.sound").Click();

        // Assert
        Assert.Equal(("Sound: off", "Sound: on"), (afterOne, app.Find("button.sound").TextContent));
    }

    [Fact]
    public void PiecesButton_WhenClicked_ShowsChessPiecesAndBack()
    {
        // Arrange
        IRenderedComponent<App> app = this._page.Render<App>();

        // Act
        app.Find("button.pieces").Click();
        string afterOne = app.Find("button.pieces").TextContent.Trim();
        app.Find("button.pieces").Click();

        // Assert
        Assert.Equal(
            ("Pieces: chess", "Pieces: chaturaji"),
            (afterOne, app.Find("button.pieces").TextContent.Trim())
        );
    }

    private IEnumerable<JSRuntimeInvocation> SeatsRemembered() =>
        this._page.JSInterop.Invocations["zaturnava.save"]
            .Where(call => ((string)call.Arguments[0]!).StartsWith("zaturnava.seat.", StringComparison.Ordinal));
}
