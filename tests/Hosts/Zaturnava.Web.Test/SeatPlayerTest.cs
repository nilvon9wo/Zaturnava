using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class SeatPlayerTest : IDisposable
{
    private static readonly ProfileDto Ada = new("p", "Ada");

    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenNoPlayerSignedInHoldsTheSeat_IsJustTheText()
    {
        // Arrange - nothing beyond the page

        // Act
        IRenderedComponent<SeatPlayer> player = this.Rendered(null);

        // Assert
        Assert.Equal(("Ada", 0), (player.Find("span.player").TextContent, player.FindAll("button").Count));
    }

    [Fact]
    public void Open_WhenThePlayerIsKnown_ShowsTheirName()
    {
        // Arrange
        this._page.Server.Profiles = this._page.Server.Profiles.Add("p", Ada);
        IRenderedComponent<SeatPlayer> player = this.Rendered("p");

        // Act
        player.Find("button.player").Click();

        // Assert
        Assert.Equal(
            ("Ada", 0),
            (player.Find("[role=dialog] h2").TextContent, player.FindAll("[role=dialog] [role=alert]").Count)
        );
    }

    [Fact]
    public void Open_WhenThePlayerIsGone_SaysSo()
    {
        // Arrange
        IRenderedComponent<SeatPlayer> player = this.Rendered("p");

        // Act
        player.Find("button.player").Click();

        // Assert
        Assert.Equal(SeatPlayer.Gone, player.Find("[role=dialog] [role=alert]").TextContent);
    }

    [Fact]
    public void Open_WhenTheServerCannotBeAsked_SaysWhy()
    {
        // Arrange
        this._page.Server.SignInFailure = new HttpRequestException("asleep");
        IRenderedComponent<SeatPlayer> player = this.Rendered("p");

        // Act
        player.Find("button.player").Click();

        // Assert
        Assert.Equal("asleep", player.Find("[role=dialog] [role=alert]").TextContent);
    }

    [Fact]
    public void Close_WhenClicked_ClosesTheProfile()
    {
        // Arrange
        this._page.Server.Profiles = this._page.Server.Profiles.Add("p", Ada);
        IRenderedComponent<SeatPlayer> player = this.Rendered("p");
        player.Find("button.player").Click();

        // Act
        player.Find("button.close-public-profile").Click();

        // Assert
        Assert.Empty(player.FindAll("[role=dialog]"));
    }

    private IRenderedComponent<SeatPlayer> Rendered(string? playerId) =>
        this._page.Render<SeatPlayer>(parameters =>
            parameters.Add(x => x.Text, "Ada").Add(x => x.PlayerId, playerId)
        );
}
