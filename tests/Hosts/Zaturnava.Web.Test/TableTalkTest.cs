using System.Globalization;
using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class TableTalkTest : IDisposable
{
    private static readonly Seating SeatZero = new(FakeGameServer.GameId, new SeatClaim(0, FakeGameServer.Token));

    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenAPlayerSits_ShowsTheLogWithItsToolsAndTabsForTheTableAndTeam()
    {
        // Arrange - nothing beyond seat 0

        // Act
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);

        // Assert
        Assert.Equal(
            ("Log|Table|Team", "true", 1, 1, "chat 0"),
            (
                Tabs(talk),
                talk.Find("button.log-tab").GetAttribute("aria-selected"),
                talk.FindAll("p.log-body").Count,
                talk.FindAll("button.tool").Count,
                this._page.Server.Talk.Single()
            )
        );
    }

    [Fact]
    public void Open_WhenWatchingTheTable_ShowsItsTalkButNoWayToSpeak()
    {
        // Arrange
        this._page.Server.Said = [Said(1, 1, ChatChannels.Table, "Hello")];
        IRenderedComponent<TableTalk> talk = this.Render(Seating.Watching(FakeGameServer.GameId));

        // Act
        talk.Find("button.table-tab").Click();

        // Assert
        Assert.Equal(
            ("Log|Table", "Only the players may speak here.", 0, 0, "Yellow Hello", "chat 0 watching"),
            (
                Tabs(talk),
                talk.Find("p.watching").TextContent,
                talk.FindAll("form.say").Count,
                talk.FindAll("button.tool").Count,
                Shown(talk),
                this._page.Server.Talk.Single()
            )
        );
    }

    [Fact]
    public void Render_WhenOthersSaidThingsUnread_CountsThemOnTheirTabsButNotThePlayersOwn()
    {
        // Arrange
        this._page.Server.Said =
        [
            Said(1, 1, ChatChannels.Table, "Hello"),
            Said(2, 0, ChatChannels.Table, "Hi"),
            Said(3, 1, ChatChannels.Table, "Ready?"),
        ];

        // Act
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);

        // Assert
        Assert.Equal(("2", 1), (talk.Find("button.table-tab .unread").TextContent, talk.FindAll(".unread").Count));
    }

    [Fact]
    public void Open_WhenATalkIsOpenedAndLeft_ShowsItNewestFirstAndCountsNothingSeen()
    {
        // Arrange
        this._page.Server.Said = [Said(1, 1, ChatChannels.Table, "Hello"), Said(2, 0, ChatChannels.Table, "Hi")];
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);

        // Act
        talk.Find("button.table-tab").Click();
        string shown = Shown(talk);
        talk.Find("button.log-tab").Click();

        // Assert
        Assert.Equal(("You Hi|Yellow Hello", 0), (shown, talk.FindAll(".unread").Count));
    }

    [Fact]
    public void Open_WhenATalkIsOpened_ShowsWhenEachMessageWasSaid()
    {
        // Arrange
        DateTimeOffset at = new(2026, 10, 31, 18, 5, 0, TimeSpan.Zero);
        this._page.Server.Said = [new ChatMessageDto(1, 1, ChatChannels.Table, "Hello", at)];
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);

        // Act
        talk.Find("button.table-tab").Click();

        // Assert
        Assert.Equal(
            ("2026-10-31T18:05:00.0000000+00:00", at.ToLocalTime().ToString("t", CultureInfo.CurrentCulture)),
            (talk.Find("time.at").GetAttribute("datetime"), talk.Find("time.at").TextContent.Trim())
        );
    }

    [Fact]
    public void Open_WhenThePlayerPrefersOldestFirst_ShowsTheTalkSo()
    {
        // Arrange
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.chat-order").SetResult("oldest");
        this._page.Server.Said = [Said(1, 1, ChatChannels.Table, "Hello"), Said(2, 0, ChatChannels.Table, "Hi")];
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);

        // Act
        talk.Find("button.table-tab").Click();

        // Assert
        Assert.Equal(
            ("Yellow Hello|You Hi", "Show newest first"),
            (Shown(talk), talk.Find("button.chat-order").TextContent.Trim())
        );
    }

    [Fact]
    public void ToggleOrder_WhenNewestWasFirst_ShowsOldestFirstAndRemembersIt()
    {
        // Arrange
        this._page.Server.Said = [Said(1, 1, ChatChannels.Table, "Hello"), Said(2, 0, ChatChannels.Table, "Hi")];
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);
        talk.Find("button.table-tab").Click();

        // Act
        talk.Find("button.chat-order").Click();

        // Assert
        Assert.Equal(
            ("Yellow Hello|You Hi", "zaturnava.chat-order oldest"),
            (
                Shown(talk),
                string.Join(" ", this._page.JSInterop.Invocations["zaturnava.save"].Single().Arguments)
            )
        );
    }

    [Fact]
    public void Say_WhenThePlayerSpeaksToTheirTeam_SaysItThereAndShowsIt()
    {
        // Arrange
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);
        talk.Find("button.team-tab").Click();
        talk.Find("input.draft").Change("Hold the left");

        // Act
        talk.Find("form.say").Submit();

        // Assert
        Assert.Equal(
            ("chat 0|say team Hold the left|chat 0", "You Hold the left", string.Empty, true),
            (
                string.Join("|", this._page.Server.Talk),
                Shown(talk),
                talk.Find("input.draft").GetAttribute("value") ?? string.Empty,
                talk.Find("button.send").HasAttribute("disabled")
            )
        );
    }

    [Fact]
    public void Say_WhenRefused_SaysWhyAndKeepsTheDraft()
    {
        // Arrange
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);
        talk.Find("button.table-tab").Click();
        talk.Find("input.draft").Change("Hello");
        this._page.Server.TalkFailure = new HttpRequestException("The server is asleep.");

        // Act
        talk.Find("form.say").Submit();

        // Assert
        Assert.Equal(
            ("The server is asleep.", "Hello"),
            (talk.Find("p.problem").TextContent, talk.Find("input.draft").GetAttribute("value"))
        );
    }

    [Fact]
    public void Render_WhenTheTalkCannotBeHeard_SaysWhyInIt()
    {
        // Arrange
        this._page.Server.TalkFailure = new HttpRequestException("The server is asleep.");
        IRenderedComponent<TableTalk> talk = this.Render(SeatZero);

        // Act
        talk.Find("button.table-tab").Click();

        // Assert
        Assert.Equal("The server is asleep.", talk.Find("p.problem").TextContent);
    }

    [Fact]
    public void Poll_WhenSomethingIsSaidLater_HearsIt()
    {
        // Arrange
        using PageContext page = new(TimeSpan.FromMilliseconds(10));
        IRenderedComponent<TableTalk> talk = page.Render<TableTalk>(parameters => parameters
            .Add(x => x.Seating, SeatZero)
            .Add(x => x.Seats, FakeGameServer.SeatZeroToMove.Seats)
        );

        // Act
        page.Server.Said = [Said(1, 1, ChatChannels.Team, "Hello")];

        // Assert
        talk.WaitForAssertion(() => Assert.Equal("1", talk.Find("button.team-tab .unread").TextContent));
    }

    private static ChatMessageDto Said(int number, int seat, string channel, string text) =>
        new(number, seat, channel, text, DateTimeOffset.UnixEpoch);

    /// <summary>Who said what in the talk shown, in the order shown, without when.</summary>
    private static string Shown(IRenderedComponent<TableTalk> talk) =>
        string.Join(
            "|",
            talk.FindAll("li.said").Select(said =>
                $"{said.QuerySelector(".who")!.TextContent} {said.QuerySelector(".words")!.TextContent}"
            )
        );

    private static string Tabs(IRenderedComponent<TableTalk> talk) =>
        string.Join("|", talk.FindAll("button.tab").Select(tab => tab.TextContent.Trim()));

    private IRenderedComponent<TableTalk> Render(Seating seating) =>
        this._page.Render<TableTalk>(parameters => parameters
            .Add(x => x.Seating, seating)
            .Add(x => x.Seats, FakeGameServer.SeatZeroToMove.Seats)
            .Add(x => x.LogTools, "<button class=\"tool\">Tool</button>")
            .AddChildContent("<p class=\"log-body\">What happened</p>")
        );
}
