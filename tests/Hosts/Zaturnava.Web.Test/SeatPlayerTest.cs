using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class SeatPlayerTest : IDisposable
{
    private static readonly ProfileDto Ada = new("p", "Ada", ["https://ada.example"]);

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
    public void Open_WhenThePlayerIsKnown_ShowsTheirNameAndLinksSafely()
    {
        // Arrange
        this._page.Server.Profiles = this._page.Server.Profiles.Add("p", Ada);
        IRenderedComponent<SeatPlayer> player = this.Rendered("p");

        // Act
        player.Find("button.player").Click();

        // Assert
        AngleSharp.Dom.IElement link = player.Find("[role=dialog] ul.links a");
        Assert.Equal(
            ("Ada", "https://ada.example", "noopener noreferrer nofollow", "_blank"),
            (
                player.Find("[role=dialog] h2").TextContent,
                link.GetAttribute("href"),
                link.GetAttribute("rel"),
                link.GetAttribute("target")
            )
        );
    }

    [Fact]
    public void Open_WhenThePlayerHasNoLinks_SaysSo()
    {
        // Arrange
        this._page.Server.Profiles = this._page.Server.Profiles.Add("p", Ada with { Links = [] });
        IRenderedComponent<SeatPlayer> player = this.Rendered("p");

        // Act
        player.Find("button.player").Click();

        // Assert
        _ = Assert.Single(player.FindAll("[role=dialog] p.no-links"));
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
