using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class PlayerBarTest : IDisposable
{
    private const string PlayerKey = "zaturnava.player";

    private static readonly PlayerDto Ada = new("p", "Ada");

    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenTheServerOffersNoServiceToSignInWith_ShowsNothing()
    {
        // Arrange - nothing beyond a server offering none

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Empty(bar.Markup.Trim());
    }

    [Fact]
    public void Render_WhenTheServerCannotBeAsked_ShowsNothing()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.SignInFailure = new HttpRequestException("asleep");

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Empty(bar.Markup.Trim());
    }

    [Fact]
    public void Render_WhenNoOneIsSignedIn_OffersEachService()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub", "Discord"];

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal(
            ["Sign in with GitHub", "Sign in with Discord"],
            bar.FindAll("button.sign-in").Select(button => button.TextContent)
        );
    }

    [Theory]
    [InlineData("known", "Ada")]
    [InlineData("forgotten", null)]
    public void Render_WhenTheBrowserRemembersAToken_ShowsWhoItStandsFor(string token, string? name)
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.Players = this._page.Server.Players.Add("known", Ada);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", PlayerKey).SetResult(token);

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal(name, bar.FindAll("span.player strong").SingleOrDefault()?.TextContent);
    }

    [Fact]
    public void SignIn_WhenClicked_OpensTheServersWindowForTheServiceToPostBackToThisPage()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Act
        bar.Find("button.sign-in").Click();
        bar.Find("button.sign-in").Click();

        // Assert
        Assert.Equal(
            Enumerable.Repeat("https://games.example/signin/GitHub?origin=http%3A%2F%2Flocalhost", 2),
            this._page.JSInterop.Invocations["zaturnava.signIn"].Select(call => call.Arguments[0])
        );
    }

    [Fact]
    public async Task SignedIn_WhenTheWindowPostsAToken_RemembersItAndShowsThePlayer()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.Players = this._page.Server.Players.Add("new", Ada);
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Act
        await bar.InvokeAsync(() => bar.Instance.SignedIn("new"));

        // Assert
        Assert.Equal(
            ($"{PlayerKey} new", "Ada"),
            (string.Join(' ', this.Saved()), bar.Find("strong").TextContent)
        );
    }

    [Fact]
    public void SignOut_WhenClicked_ForgetsThePlayerAndOffersTheServicesAgain()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.Players = this._page.Server.Players.Add("known", Ada);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", PlayerKey).SetResult("known");
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Act
        bar.Find("button.sign-out").Click();

        // Assert
        Assert.Equal(
            ($"{PlayerKey} ", 1),
            (string.Join(' ', this.Saved()), bar.FindAll("button.sign-in").Count)
        );
    }

    /// <summary>What the page last asked the browser to remember.</summary>
    private IReadOnlyList<object?> Saved() => this._page.JSInterop.Invocations["zaturnava.save"].Single().Arguments;
}
