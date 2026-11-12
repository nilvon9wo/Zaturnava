using System.Collections.Immutable;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class PlayerBarTest : IDisposable
{
    private const string PlayerKey = "zaturnava.player";
    private const string NicknameKey = "zaturnava.nickname";
    private const string AliasKey = "zaturnava.alias";

    private static readonly PlayerDto Ada = new("p", "Ada");

    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenNoOneIsSignedIn_OffersOneButtonToSignInAndNoDialogYet()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub", "Discord"];

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal(
            ("Sign in, or choose a name", 0, 0),
            (
                string.Join('|', bar.FindAll("button").Select(button => button.TextContent.Trim())),
                bar.FindAll("[role=dialog]").Count,
                bar.FindAll(".guest-name").Count
            )
        );
    }

    [Fact]
    public void SignInDialog_WhenOpened_OffersANicknameAndEachService()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub", "Discord"];
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Act
        bar.Find("button.open-sign-in").Click();

        // Assert
        Assert.Equal(
            (1, "Sign in with GitHub|Sign in with Discord"),
            (
                bar.FindAll("[role=dialog] input.nickname").Count,
                string.Join('|', bar.FindAll("[role=dialog] button.sign-in").Select(button => button.TextContent))
            )
        );
    }

    [Fact]
    public void SignInDialog_WhenTheServerCannotBeAsked_OffersOnlyANickname()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.SignInFailure = new HttpRequestException("asleep");
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Act
        bar.Find("button.open-sign-in").Click();

        // Assert
        Assert.Equal((1, 0), (bar.FindAll("input.nickname").Count, bar.FindAll("button.sign-in").Count));
    }

    [Theory]
    [InlineData("button.as-guest")]
    [InlineData("button.cancel")]
    public void SignInDialog_WhenLeft_Closes(string button)
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();
        bar.Find("button.open-sign-in").Click();

        // Act
        bar.Find(button).Click();

        // Assert
        Assert.Empty(bar.FindAll("[role=dialog]"));
    }

    [Fact]
    public void Render_WhenTheBrowserRemembersANickname_ShowsItToPlayByHereAndInTheDialog()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", NicknameKey).SetResult("Bea");
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Act
        bar.Find("button.open-sign-in").Click();

        // Assert
        Assert.Equal(
            ("Bea (guest) ▾", "Bea"),
            (bar.Find(".guest-name").TextContent, bar.Find("input.nickname").GetAttribute("value"))
        );
    }

    [Fact]
    public void Render_WhenTheBrowserHoldsAnAliasStill_RenewsItAndPlaysByIt()
    {
        // Arrange - the alias held is Cy, though the browser kept Bea
        this._page.Server.Aliases = this._page.Server.Aliases.Add("held", "Cy");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", NicknameKey).SetResult("Bea");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", AliasKey).SetResult("held");

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal(
            ("Cy", $"{NicknameKey} Cy"),
            (bar.Find(".guest-name strong").TextContent, string.Join('|', this.SavedEach().Where(IsNickname)))
        );
    }

    [Fact]
    public void Render_WhenTheBrowserHoldsNoAliasNow_TakesTheNicknameAfresh()
    {
        // Arrange - the alias the browser held has lapsed
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", NicknameKey).SetResult("Bea");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", AliasKey).SetResult("lapsed");

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal(
            ("Bea", "[alias-1, Bea]"),
            (bar.Find(".guest-name strong").TextContent, string.Join('|', this._page.Server.Aliases))
        );
    }

    [Fact]
    public void Nickname_WhenTakenByAnother_PlaysByTheNextFreeAndSaysSo()
    {
        // Arrange
        this._page.Server.TakenNames = ["Ada"];
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();
        bar.Find("button.open-sign-in").Click();

        // Act
        bar.Find("input.nickname").Change("Ada");

        // Assert
        Assert.Equal(
            ("Ada 2", "Ada is taken, so you are Ada 2."),
            (bar.Find(".guest-name strong").TextContent, bar.Find("p.alias-note").TextContent)
        );
    }

    [Fact]
    public void Nickname_WhenChangedAgain_KeepsTheTokenTheAliasIsHeldBy()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();
        bar.Find("button.open-sign-in").Click();
        bar.Find("input.nickname").Change("Bea");
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", AliasKey).SetResult("alias-1");

        // Act
        bar.Find("input.nickname").Change("Cy");

        // Assert
        Assert.Equal("[alias-1, Cy]", string.Join('|', this._page.Server.Aliases));
    }

    [Fact]
    public void Nickname_WhenTheServerCannotHoldIt_SaysWhyAndKeepsNone()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();
        bar.Find("button.open-sign-in").Click();
        this._page.Server.SignInFailure = new HttpRequestException("asleep");

        // Act
        bar.Find("input.nickname").Change("Bea");

        // Assert
        Assert.Equal(("asleep", 0), (bar.Find("span.notice").TextContent, bar.FindAll(".guest-name").Count));
    }

    [Fact]
    public void Nickname_WhenCleared_IsNoLongerShownToPlayBy()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", NicknameKey).SetResult("Bea");
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();
        bar.Find("button.open-sign-in").Click();

        // Act
        bar.Find("input.nickname").Change("   ");

        // Assert
        Assert.Equal(
            (0, $"{NicknameKey} |{AliasKey} "),
            (bar.FindAll(".guest-name").Count, string.Join('|', this.SavedEach().TakeLast(2)))
        );
    }

    [Fact]
    public void Nickname_WhenChanged_IsHeldByTheServerAndRememberedWithoutTheSpacesAround()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();
        bar.Find("button.open-sign-in").Click();

        // Act
        bar.Find("input.nickname").Change("  Bea  ");

        // Assert
        Assert.Equal(
            ($"{AliasKey} alias-1|{NicknameKey} Bea", "Bea", 0),
            (
                string.Join('|', this.SavedEach()),
                bar.Find("input.nickname").GetAttribute("value"),
                bar.FindAll("p.alias-note").Count
            )
        );
    }

    [Theory]
    [InlineData("known", "Ada", 0)]
    [InlineData("forgotten", null, 1)]
    public void Render_WhenTheBrowserRemembersAToken_ShowsWhoItStandsForOrForgetsIt(
        string token,
        string? name,
        int forgets
    )
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.Players = this._page.Server.Players.Add("known", Ada);
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", PlayerKey).SetResult(token);

        // Act
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();

        // Assert
        Assert.Equal(
            (name, forgets),
            (
                bar.FindAll("span.player strong").SingleOrDefault()?.TextContent,
                this._page.JSInterop.Invocations["zaturnava.save"]
                    .Count(call => $"{call.Arguments[0]} {call.Arguments[1]}" == PlayerKey + " ")
            )
        );
    }

    [Fact]
    public void SignIn_WhenClicked_OpensTheServersWindowForTheServiceToPostBackToThisPage()
    {
        // Arrange
        this._page.Server.Providers = ["GitHub"];
        IRenderedComponent<PlayerBar> bar = this._page.Render<PlayerBar>();
        bar.Find("button.open-sign-in").Click();

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
        await bar.Find("button.open-sign-in").ClickAsync(new MouseEventArgs());

        // Act
        await bar.InvokeAsync(() => bar.Instance.SignedIn("new"));

        // Assert
        Assert.Equal(
            ($"{PlayerKey} new", "Ada", 0),
            (string.Join(' ', this.Saved()), bar.Find("strong").TextContent, bar.FindAll("[role=dialog]").Count)
        );
    }

    [Fact]
    public async Task SignedInOrOut_WhenAPlayerSignsInThenOut_TellsThePageEachTime()
    {
        // Arrange
        int told = 0;
        this._page.Server.Providers = ["GitHub"];
        this._page.Server.Players = this._page.Server.Players.Add("new", Ada);
        IRenderedComponent<PlayerBar> bar =
            this._page.Render<PlayerBar>(parameters => parameters.Add(x => x.OnSignedInOrOut, () => told++));

        // Act
        await bar.InvokeAsync(() => bar.Instance.SignedIn("new"));
        await bar.Find("button.account-name").ClickAsync(new MouseEventArgs());
        await bar.Find("button.sign-out").ClickAsync(new MouseEventArgs());

        // Assert
        Assert.Equal(2, told);
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
        Choose(bar, "button.sign-out");

        // Assert
        Assert.Equal(
            ($"{PlayerKey} ", 1, isServerDown ? string.Empty : "out known"),
            (
                string.Join(' ', this.Saved()),
                bar.FindAll("button.open-sign-in").Count,
                string.Join('|', this._page.Server.LetGo)
            )
        );
    }

    [Fact]
    public void AccountMenu_WhenTheNameIsClicked_OpensThenClosesAgain()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();
        int before = bar.FindAll("[role=menu]").Count;

        // Act
        bar.Find("button.account-name").Click();
        string opened = string.Join('|', bar.FindAll("[role=menuitem]").Select(item => item.TextContent.Trim()));
        string expanded = bar.Find("button.account-name").GetAttribute("aria-expanded")!;
        bar.Find("button.account-name").Click();

        // Assert
        Assert.Equal(
            (0, "Profile and sign-in services|Sign out|Delete account", "true", 0, "false"),
            (
                before,
                opened,
                expanded,
                bar.FindAll("[role=menu]").Count,
                bar.Find("button.account-name").GetAttribute("aria-expanded")
            )
        );
    }

    [Fact]
    public void DeleteAccount_WhenClicked_AsksFirstAndDeletesNothingYet()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();

        // Act
        Choose(bar, "button.delete-account");

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
        Choose(bar, "button.delete-account");

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
        Choose(bar, "button.delete-account");

        // Act
        bar.Find("button.delete-confirmed").Click();

        // Assert
        Assert.Equal(
            ("delete known", $"{PlayerKey} ", PlayerBar.Deleted, 1),
            (
                string.Join('|', this._page.Server.LetGo),
                string.Join(' ', this.Saved()),
                bar.Find("[role=status]").TextContent,
                bar.FindAll("button.open-sign-in").Count
            )
        );
    }

    [Fact]
    public void DeleteAccount_WhenTheServerCannotBeReached_SaysWhyAndStaysSignedIn()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();
        Choose(bar, "button.delete-account");
        this._page.Server.SignInFailure = new HttpRequestException("asleep");

        // Act
        bar.Find("button.delete-confirmed").Click();

        // Assert
        Assert.Equal(("asleep", "Ada"), (bar.Find("[role=status]").TextContent, bar.Find("strong").TextContent));
    }

    [Fact]
    public void Profile_WhenOpened_ShowsTheNameAndServicesAndOffersToLinkTheOthers()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"]);

        // Act
        Choose(bar, "button.open-profile");

        // Assert
        Assert.Equal(
            ("Ada", "GitHub", 0, "Link Discord"),
            (
                bar.Find("[role=dialog] input.name").GetAttribute("value"),
                bar.Find("ul.services li").TextContent.Trim(),
                bar.FindAll("button.unlink").Count,
                string.Join('|', bar.FindAll("button.link").Select(button => button.TextContent))
            )
        );
    }

    [Fact]
    public void Profile_WhenNoCodeCanBeHad_SaysWhyAndOffersNoLink()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"]);
        this._page.Server.SignInFailure = new HttpRequestException("asleep");

        // Act
        Choose(bar, "button.open-profile");

        // Assert
        Assert.Equal(("asleep", 0), (bar.Find("[role=status]").TextContent, bar.FindAll("button.link").Count));
    }

    [Fact]
    public void Link_WhenClicked_OpensTheWindowWithTheCodeAndAsksAnotherSoTheLinksStayOffered()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"]);
        Choose(bar, "button.open-profile");

        // Act
        bar.Find("button.link").Click();

        // Assert
        Assert.Equal(
            ("https://games.example/signin/Discord?origin=http%3A%2F%2Flocalhost&link=code-known", 1),
            (
                this._page.JSInterop.Invocations["zaturnava.signIn"].Single().Arguments[0],
                bar.FindAll("button.link").Count
            )
        );
    }

    [Fact]
    public async Task SignedIn_WhenLinkedWithTheProfileOpen_ShowsTheServiceAndAsksANewCode()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"]);
        await bar.Find("button.account-name").ClickAsync(new MouseEventArgs());
        await bar.Find("button.open-profile").ClickAsync(new MouseEventArgs());
        await bar.Find("button.link").ClickAsync(new MouseEventArgs());
        PlayerDto linked = Ada with { Services = ["GitHub", "Discord"] };
        this._page.Server.Players = this._page.Server.Players.Add("linked", linked);

        // Act
        await bar.InvokeAsync(() => bar.Instance.SignedIn("linked"));

        // Assert
        Assert.Equal(
            ("GitHub|Discord", 2, 0),
            (
                string.Join('|', bar.FindAll("ul.services li").Select(item => item.FirstChild?.TextContent.Trim())),
                bar.FindAll("button.unlink").Count,
                bar.FindAll("button.link").Count
            )
        );
    }

    [Fact]
    public void Rename_WhenTheNameIsChanged_ShowsTheNewOne()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"]);
        Choose(bar, "button.open-profile");

        // Act
        bar.Find("input.name").Change("Countess");

        // Assert
        Assert.Equal(
            ("Countess", "Username saved."),
            (bar.Find("span.player strong").TextContent, bar.Find("p.saved").TextContent)
        );
    }

    [Fact]
    public void Rename_WhenTheTokenNoLongerStandsForAnyone_ShowsNoOneSignedIn()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"]);
        Choose(bar, "button.open-profile");
        this._page.Server.Players = this._page.Server.Players.Remove("known");

        // Act
        bar.Find("input.name").Change("Countess");

        // Assert
        _ = Assert.Single(bar.FindAll("button.open-sign-in"));
    }

    [Fact]
    public void Describe_WhenTheDescriptionIsChanged_SavesItWithTheServicesShown()
    {
        // Arrange
        this._page.Server.Players = this._page.Server.Players.Add("shown", Ada with { Shown = ["GitHub"] });
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"], "shown");
        Choose(bar, "button.open-profile");

        // Act
        bar.Find("textarea.description").Change("Counts.");

        // Assert
        PlayerDto kept = this._page.Server.Players["shown"];
        Assert.Equal(
            ("Counts.", "GitHub", "Profile saved.", "Counts."),
            (
                kept.Description,
                string.Join(",", kept.Shown),
                bar.Find("p.saved").TextContent,
                bar.Find("textarea.description").GetAttribute("value")
            )
        );
    }

    [Theory]
    [InlineData(true, "GitHub")]
    [InlineData(false, "")]
    public void Show_WhenAServiceIsTickedOrNot_ShowsItOnTheProfileOrNot(bool isTicked, string shown)
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"]);
        Choose(bar, "button.open-profile");
        bar.Find("input.shown").Change(true);

        // Act
        bar.Find("input.shown").Change(isTicked);

        // Assert
        Assert.Equal(
            (shown, isTicked),
            (
                string.Join(",", this._page.Server.Players["known"].Shown),
                bar.Find("input.shown").HasAttribute("checked")
            )
        );
    }

    [Fact]
    public void Unlink_WhenClicked_ShowsTheServicesLeft()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub", "Discord"]);
        Choose(bar, "button.open-profile");

        // Act
        bar.Find("button.unlink").Click();

        // Assert
        Assert.Equal(
            ("Discord", "Link GitHub", "GitHub unlinked."),
            (
                bar.Find("ul.services li").TextContent.Trim(),
                string.Join('|', bar.FindAll("button.link").Select(button => button.TextContent)),
                bar.Find("p.saved").TextContent
            )
        );
    }

    [Fact]
    public void Profile_WhenClosed_ClosesAndForgetsWhatItSaved()
    {
        // Arrange - a player told of no services
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda();
        Choose(bar, "button.open-profile");
        bar.Find("input.name").Change("Countess");

        // Act
        bar.Find("button.close-profile").Click();

        // Assert
        Assert.Empty(bar.FindAll("[role=dialog]"));
        Choose(bar, "button.open-profile");
        Assert.Empty(bar.FindAll("p.saved"));
    }

    [Fact]
    public void AiToken_WhenAsked_ShowsItUntilTheProfileCloses()
    {
        // Arrange
        IRenderedComponent<PlayerBar> bar = this.SignedInAsAda(["GitHub"]);
        Choose(bar, "button.open-profile");

        // Act
        bar.Find("button.ai-token").Click();

        // Assert
        string shown = bar.Find("p.ai-token code").TextContent;
        bar.Find("button.close-profile").Click();
        Choose(bar, "button.open-profile");
        Assert.Equal(("ai-known", 0), (shown, bar.FindAll("p.ai-token").Count));
    }

    /// <summary>
    /// The bar, with Ada signed in on this browser by the token "known", signing in with
    /// <paramref name="services"/>; GitHub and Discord are offered.
    /// </summary>
    private IRenderedComponent<PlayerBar> SignedInAsAda(ImmutableList<string>? services = null)
    {
        this._page.Server.Players = this._page.Server.Players.Add("known", Ada);
        return this.SignedInAsAda(services, "known");
    }

    /// <summary>
    /// The bar, with the player the server knows by <paramref name="token"/> signed in on this browser by it, signing
    /// in with <paramref name="services"/>; GitHub and Discord are offered.
    /// </summary>
    private IRenderedComponent<PlayerBar> SignedInAsAda(ImmutableList<string>? services, string token)
    {
        this._page.Server.Providers = ["GitHub", "Discord"];
        this._page.Server.Players =
            this._page.Server.Players.SetItem(token, this._page.Server.Players[token] with { Services = services });
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", PlayerKey).SetResult(token);
        return this._page.Render<PlayerBar>();
    }

    /// <summary>What the page last asked the browser to remember.</summary>
    private IReadOnlyList<object?> Saved() => this._page.JSInterop.Invocations["zaturnava.save"].Single().Arguments;

    /// <summary>Everything the page asked the browser to remember, in order, each as "key value".</summary>
    private IEnumerable<string> SavedEach() =>
        this._page.JSInterop.Invocations["zaturnava.save"].Select(call => $"{call.Arguments[0]} {call.Arguments[1]}");

    private static bool IsNickname(string saved) => saved.StartsWith(NicknameKey, StringComparison.Ordinal);

    /// <summary>Opens the signed-in player's menu, and chooses <paramref name="item"/> from it.</summary>
    private static void Choose(IRenderedComponent<PlayerBar> bar, string item)
    {
        bar.Find("button.account-name").Click();
        bar.Find(item).Click();
    }
}
