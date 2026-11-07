using System.Collections.Immutable;
using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class LeaderboardTest : IDisposable
{
    private static readonly ImmutableList<RulesetDto> GrandSizes =
    [
        new("grand-legions-small", "Grand Legions (small)", 4) { Game = "grand-legions", GameName = "Grand Legions" },
        new("grand-legions-large", "Grand Legions (large)", 4) { Game = "grand-legions", GameName = "Grand Legions" },
    ];

    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenPlayersStandAtTheFirstGame_ListsThemBestFirstWithoutAChoiceOfGame()
    {
        // Arrange
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [
                new StandingDto(1, "Ada", 1516, 14, 1, 0, 0, 2) { Points = 9 },
                new StandingDto(2, "Bea", 1484, 0, 0, 1, 0) { Points = 3 },
            ]
        );

        // Act
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();

        // Assert
        Assert.Equal(
            ("1 Ada 1516 14 1 0 0 2|2 Bea 1484 0 0 1 0 0", 0),
            (
                string.Join(
                    "|",
                    board.FindAll(".leaders tbody tr").Select(row =>
                        string.Join(" ", row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim()))
                    )
                ),
                board.FindAll("select.leaderboard-game").Count
            )
        );
    }

    [Fact]
    public void Choose_WhenAnotherGameIsChosen_ShowsWhoStandsAtItOnceForEachOfItsSizes()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "grand-legions",
            [new StandingDto(1, "Cy", 1530, 40, 2, 0, 0)]
        );
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();
        string before = board.Find(".no-leaders").TextContent;

        // Act
        board.Find("select.leaderboard-game").Change("grand-legions");

        // Assert
        Assert.Equal(
            (
                "No one signed in has finished a game of it yet.",
                "Chaturaji 2027,Grand Legions",
                "Cy",
                "leaderboard chaturaji-2027,leaderboard grand-legions"
            ),
            (
                before,
                string.Join(",", board.FindAll("select.leaderboard-game option").Select(option => option.TextContent)),
                board.Find(".leaders td.name").TextContent.Trim(),
                string.Join(
                    ",",
                    this._page.Server.Calls.Where(call => call.StartsWith("leaderboard", StringComparison.Ordinal))
                )
            )
        );
    }

    [Theory]
    [InlineData("grand-legions-small", null, "grand-legions")]
    [InlineData(null, "grand-legions", "grand-legions")]
    [InlineData("no-such-ruleset", "no-such-game", "chaturaji-2027")]
    public void Render_WhenOpened_BeginsAtTheTablesGameElseTheLastStartedElseTheFirst(
        string? rulesetId,
        string? last,
        string expected
    )
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        _ = this._page.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.last-game").SetResult(last);

        // Act
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>(
            parameters => parameters.Add(leaderboard => leaderboard.RulesetId, rulesetId)
        );

        // Assert
        Assert.Equal(
            (expected, $"leaderboard {expected}"),
            (
                board.Find("select.leaderboard-game").GetAttribute("value"),
                string.Join(
                    ",",
                    this._page.Server.Calls.Where(call => call.StartsWith("leaderboard", StringComparison.Ordinal))
                )
            )
        );
    }

    [Fact]
    public void ChooseBoard_WhenTheBotsAreChosen_ListsThemWithHowTheyDidAgainstPeople()
    {
        // Arrange
        this._page.Server.BotLeaderboards = this._page.Server.BotLeaderboards.Add(
            "chaturaji-2027",
            [
                new BotStandingDto(1, "Sage", 9, 3, 1, 0)
                {
                    WonAgainstPeople = 2,
                    LostAgainstPeople = 1,
                    DrawnAgainstPeople = 0,
                },
            ]
        );
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();

        // Act
        board.Find("select.leaderboard-board").Change("bots");

        // Assert
        Assert.Equal(
            ("1 Sage 3 1 0 2 1 0", "bots chaturaji-2027"),
            (
                string.Join(
                    " ",
                    board.FindAll(".bot-leaders tbody td").Select(cell => cell.TextContent.Trim())
                ),
                Assert.Single(this._page.Server.Calls, call => call.StartsWith("bots", StringComparison.Ordinal))
            )
        );
    }

    [Fact]
    public void ChooseBoard_WhenPeopleAgainstBotsAreChosen_ListsTheirGamesAgainstBotsAlone()
    {
        // Arrange
        this._page.Server.AgainstBotsLeaderboards = this._page.Server.AgainstBotsLeaderboards.Add(
            "chaturaji-2027",
            [
                new StandingDto(1, "Ada", 1516, 14, 5, 2, 1)
                {
                    WonAgainstBots = 3,
                    LostAgainstBots = 1,
                    DrawnAgainstBots = 1,
                },
            ]
        );
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();

        // Act
        board.Find("select.leaderboard-board").Change("against-bots");

        // Assert
        Assert.Equal(
            "1 Ada 3 1 1",
            string.Join(" ", board.FindAll(".against-bots tbody td").Select(cell => cell.TextContent.Trim()))
        );
    }

    [Fact]
    public void SortBy_WhenAHeaderIsClickedOnceThenTwice_SortsByItMostFirstThenFewestFirst()
    {
        // Arrange - by Elo, Ada first; Bea has lost the most, Cy the fewest
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [
                new StandingDto(1, "Ada", 1600, 0, 0, 2, 0),
                new StandingDto(2, "Bea", 1500, 0, 0, 5, 0),
                new StandingDto(3, "Cy", 1400, 0, 0, 1, 0),
            ]
        );
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();
        string before = Names(board);

        // Act
        board.Find("button.sort.lost").Click();
        string once = $"{Names(board)} {board.Find("th[aria-sort=descending]").TextContent.Trim()}";
        board.Find("button.sort.lost").Click();
        string twice = $"{Names(board)} {board.Find("th[aria-sort=ascending]").TextContent.Trim()}";

        // Assert
        Assert.Equal(
            ("Ada,Bea,Cy", "Bea,Ada,Cy Lost ▼", "Cy,Ada,Bea Lost ▲", "1,2,3"),
            (before, once, twice, string.Join(",", board.FindAll("td.place").Select(cell => cell.TextContent)))
        );
    }

    [Fact]
    public void Name_WhenAPlayersIsClicked_OpensTheirProfile()
    {
        // Arrange
        this._page.Server.Profiles = this._page.Server.Profiles.Add("ada", new ProfileDto("ada", "Ada Lovelace"));
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [new StandingDto(1, "Ada", 1516, 0, 1, 0, 0) { PlayerId = "ada" }]
        );
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();

        // Act
        board.Find(".leaders td.name button.profile").Click();

        // Assert
        Assert.Equal("Ada Lovelace", board.Find(".public-profile h2").TextContent);
    }

    [Fact]
    public void Name_WhenABotsIsClickedOverATable_OpensItsProfileWithItsRatingThere()
    {
        // Arrange
        this._page.Server.BotLeaderboards = this._page.Server.BotLeaderboards.Add(
            "chaturaji-2027",
            [new BotStandingDto(1, "Random", 0, 3, 1, 0) { BotId = "random" }]
        );
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>(
            parameters => parameters.Add(leaderboard => leaderboard.RulesetId, "chaturaji-2027")
        );
        board.Find("select.leaderboard-board").Change("bots");

        // Act
        board.Find(".leaders td.name button.bot-profile").Click();

        // Assert
        Assert.Equal(
            ("Random", "Rated 1000 at this game, from games between the bots."),
            (board.Find(".public-profile h2").TextContent, board.Find(".public-profile .bot-rating").TextContent)
        );
    }

    [Fact]
    public void Paging_WhenMoreThanTenStand_ShowsTenAPageAndTurnsThePagesBothWays()
    {
        // Arrange - fifteen players, by Elo
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [.. Enumerable.Range(1, 15).Select(place => new StandingDto(place, $"P{place}", 2000 - place, 0, 0, 0, 0))]
        );
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();
        string first = $"{board.FindAll(".leaders tbody tr").Count} {board.Find(".pager .page").TextContent}";

        // Act
        board.Find(".pager button.next").Click();
        bool isLast = board.Find(".pager .next").HasAttribute("disabled");
        string second = $"{Names(board)} {board.Find(".pager .page").TextContent} {isLast}";
        board.Find(".pager button.previous").Click();

        // Assert
        Assert.Equal(
            ("10 Page 1 of 2", "P11,P12,P13,P14,P15 Page 2 of 2 True", "P1", true),
            (
                first,
                second,
                board.Find(".leaders td.name").TextContent.Trim(),
                board.Find(".pager .previous").HasAttribute("disabled")
            )
        );
    }

    [Fact]
    public void SortBy_WhenOnALaterPage_ShowsTheFirstPageAgain()
    {
        // Arrange - eleven players, on the second page
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [.. Enumerable.Range(1, 11).Select(at => new StandingDto(at, $"P{at}", 2000 - at, at, 0, 0, 0))]
        );
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();
        board.Find(".pager button.next").Click();

        // Act
        board.Find("button.sort.experience").Click();

        // Assert
        Assert.Equal(
            ("P11", "Page 1 of 2"),
            (board.Find(".leaders td.name").TextContent.Trim(), board.Find(".pager .page").TextContent)
        );
    }

    [Theory]
    [InlineData("bots", "No bot has finished a game of it yet.")]
    [InlineData("against-bots", "No one signed in has finished a game of it against bots alone yet.")]
    [InlineData("players", "No one signed in has finished a game of it yet.")]
    public void ChooseBoard_WhenTheBoardIsEmpty_SaysNoOneIsOnIt(string chosen, string expected)
    {
        // Arrange
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();

        // Act
        board.Find("select.leaderboard-board").Change(chosen);

        // Assert
        Assert.Equal(expected, board.Find(".no-leaders").TextContent);
    }

    [Theory]
    [InlineData("bots")]
    [InlineData("against-bots")]
    public void ChooseBoard_WhenTheServerCannotBeReached_SaysWhyAndListsNoOne(string chosen)
    {
        // Arrange
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();
        this._page.Server.Failure = new HttpRequestException("unreachable");

        // Act
        board.Find("select.leaderboard-board").Change(chosen);

        // Assert
        Assert.Equal(
            (1, 0, 0),
            (board.FindAll("p.problem").Count, board.FindAll(".leaders").Count, board.FindAll(".no-leaders").Count)
        );
    }

    [Fact]
    public void Choose_WhenTheServerCannotBeReached_SaysWhyAndListsNoOne()
    {
        // Arrange
        this._page.Server.MoreRulesets = GrandSizes;
        IRenderedComponent<Leaderboard> board = this._page.Render<Leaderboard>();
        this._page.Server.Failure = new HttpRequestException("unreachable");

        // Act
        board.Find("select.leaderboard-game").Change("grand-legions");

        // Assert
        Assert.Equal(
            (1, 0, 0),
            (board.FindAll("p.problem").Count, board.FindAll(".leaders").Count, board.FindAll(".no-leaders").Count)
        );
    }

    /// <summary>The names on the page shown, in order.</summary>
    private static string Names(IRenderedComponent<Leaderboard> board) =>
        string.Join(",", board.FindAll(".leaders td.name").Select(cell => cell.TextContent.Trim()));
}
