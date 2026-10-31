using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class PlayerBarTest : IDisposable
{
    private const string PlayerKey = "zaturnava.player";
    private const string NicknameKey = "zaturnava.nickname";

    private static readonly PlayerDto Ada = new("p", "Ada");

    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenTheServerOffersNoServiceToSignInWith_OffersOnlyANickname()
    {
        // Arrange - nothing beyond a server offering none

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal((1, 0), (bar.FindAll("input.nickname").Count, bar.FindAll("button").Count));
    }

    [Fact]
    public void Render_WhenTheServerCannotBeAsked_OffersOnlyANickname()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.SignInFailure = new HttpRequestException("asleep");

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal((1, 0), (bar.FindAll("input.nickname").Count, bar.FindAll("button").Count));
    }

    [Fact]
    public void Render_WhenTheBrowserRemembersANickname_ShowsItToPlayBy()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", NicknameKey).SetResult("Bea");

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal("Bea", bar.Find("input.nickname").GetAttribute("value"));
    }

    [Fact]
    public void Nickname_WhenChanged_IsRememberedWithoutTheSpacesAround()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Act
        bar.Find("input.nickname").Change("  Bea  ");

        // Assert
        Assert.Equal(
            ($"{NicknameKey} Bea", "Bea"),
            (string.Join(' ', this.Saved()), bar.Find("input.nickname").GetAttribute("value"))
        );
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SignOut_WhenClicked_HasTheServerLetTheTokenGoAndForgetsItEvenIfTheServerCannot(bool isServerDown)
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();
        this._page.Server.SignInFailure = isServerDown ? new HttpRequestException("asleep") : null;

        // Act
        bar.Find("button.sign-out").Click();

        // Assert
        Assert.Equal(
            ($"{PlayerKey} ", 1, isServerDown ? string.Empty : "out known"),
            (
                string.Join(' ', this.Saved()),
                bar.FindAll("button.sign-in").Count,
                string.Join('|', this._page.Server.LetGo)
            )
        );
    }

    [Fact]
    public void DeleteAccount_WhenClicked_AsksFirstAndDeletesNothingYet()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();

        // Act
        bar.Find("button.delete-account").Click();

        // Assert
        Assert.Equal(
            (1, 0),
            (bar.FindAll("[role=alertdialog] button.delete-confirmed").Count, this._page.Server.LetGo.Count)
        );
    }

    [Fact]
    public void DeleteAccount_WhenKept_AsksNoMoreAndStaysSignedIn()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();
        bar.Find("button.delete-account").Click();

        // Act
        bar.Find("button.delete-cancelled").Click();

        // Assert
        Assert.Equal(
            (0, "Ada", 0),
            (bar.FindAll("[role=alertdialog]").Count, bar.Find("strong").TextContent, this._page.Server.LetGo.Count)
        );
    }

    [Fact]
    public void DeleteAccount_WhenConfirmed_HasTheServerForgetThePlayerAndSaysSo()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();
        bar.Find("button.delete-account").Click();

        // Act
        bar.Find("button.delete-confirmed").Click();

        // Assert
        Assert.Equal(
            ("delete known", $"{PlayerKey} ", PlayerBar.Deleted, 1),
            (
                string.Join('|', this._page.Server.LetGo),
                string.Join(' ', this.Saved()),
                bar.Find("[role=status]").TextContent,
                bar.FindAll("button.sign-in").Count
            )
        );
    }

    [Fact]
    public void DeleteAccount_WhenTheServerCannotBeReached_SaysWhyAndStaysSignedIn()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();
        bar.Find("button.delete-account").Click();
        this._page.Server.SignInFailure = new HttpRequestException("asleep");

        // Act
        bar.Find("button.delete-confirmed").Click();

        // Assert
        Assert.Equal(("asleep", "Ada"), (bar.Find("[role=status]").TextContent, bar.Find("strong").TextContent));
    }

    /// <summary>The bar, with Ada signed in on this browser by the token "known".</summary>
    private IRenderedComponent<PlayerBar> SignedInAsAda()
    {
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.Players = this._page.Server.Players.Add("known", Ada);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", PlayerKey).SetResult("known");
        return this._page.Render<PlayerBar>();
    }

    /// <summary>What the page last asked the browser to remember.</summary>
    private IReadOnlyList<object?> Saved() => this._page.JSInterop.Invocations["zaturnava.save"].Single().Arguments;
}
