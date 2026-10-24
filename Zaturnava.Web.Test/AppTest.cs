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
}
