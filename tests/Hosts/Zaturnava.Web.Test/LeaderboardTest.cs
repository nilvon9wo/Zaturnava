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
            ("1 Ada 1516 14 9 1 0 0 2|2 Bea 1484 0 3 0 1 0 0", 0),
            (
                string.Join(
                    "|",
                    board.FindAll(".leaders tbody tr").Select(row =>
                        string.Join(" ", row.QuerySelectorAll("td").Select(cell => cell.TextContent))
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
                board.Find(".leaders td.name").TextContent,
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
            ("1 Sage 9 3 1 0 2–1–0", "bots chaturaji-2027"),
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
            string.Join(" ", board.FindAll(".against-bots tbody td").Select(cell => cell.TextContent))
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
}
