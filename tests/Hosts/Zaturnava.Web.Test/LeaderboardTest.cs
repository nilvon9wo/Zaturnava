using Bunit;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class LeaderboardTest : IDisposable
{
    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenPlayersStandAtTheGame_ListsThemBestFirstWithTheirPointsWithoutAChoiceOfGame()
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
        IRenderedComponent<Leaderboard> board = this.Render();

        // Assert
        Assert.Equal(
            ("1 Ada 1516 14 9 1 0 0 2 0 0 0|2 Bea 1484 0 3 0 1 0 0 0 0 0", 0),
            (Rows(board), board.FindAll(".leaderboard select:not(.leaderboard-board)").Count)
        );
    }

    [Fact]
    public void Render_WhenAPlayerIsOnAStreak_ShowsEachStreakWithTheMeanEloItBeat()
    {
        // Arrange
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [
                new StandingDto(1, "Ada", 1600, 30, 3, 0, 0)
                {
                    Streak = new StreakDto(3, 1580),
                    RatedStreak = new StreakDto(2, 1610),
                    PeopleStreak = new StreakDto(0),
                },
            ]
        );

        // Act
        IRenderedComponent<Leaderboard> board = this.Render();

        // Assert
        Assert.Equal(
            ("3", "vs 1580", "2", "vs 1610", "0", 2),
            (
                board.Find("td.streak").FirstChild!.TextContent.Trim(),
                board.Find("td.streak .against").TextContent,
                board.Find("td.rated-streak").FirstChild!.TextContent.Trim(),
                board.Find("td.rated-streak .against").TextContent,
                board.Find("td.people-streak").TextContent.Trim(),
                board.FindAll(".leaders .against").Count
            )
        );
    }

    [Fact]
    public void Game_WhenTheLobbyChoosesAnother_ShowsWhoStandsAtItOnceForEachOfItsSizes()
    {
        // Arrange
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "grand-legions",
            [new StandingDto(1, "Cy", 1530, 40, 2, 0, 0)]
        );
        IRenderedComponent<Leaderboard> board = this.Render();
        string before = board.Find(".no-leaders").TextContent;

        // Act
        board.Render(parameters => parameters.Add(leaderboard => leaderboard.Game, "grand-legions"));
        board.Render(parameters => parameters.Add(leaderboard => leaderboard.Game, "grand-legions"));

        // Assert
        Assert.Equal(
            (
                "No one signed in has finished a game of it yet.",
                "Cy",
                "leaderboard chaturaji-2027,leaderboard grand-legions"
            ),
            (
                before,
                board.Find(".leaders td.name").TextContent.Trim(),
                string.Join(
                    ",",
                    this._page.Server.Calls.Where(call => call.StartsWith("leaderboard", StringComparison.Ordinal))
                )
            )
        );
    }

    [Fact]
    public void Game_WhenTheServerCannotBeReached_SaysWhyAndListsNoOne()
    {
        // Arrange
        IRenderedComponent<Leaderboard> board = this.Render();
        this._page.Server.Failure = new HttpRequestException("unreachable");

        // Act
        board.Render(parameters => parameters.Add(leaderboard => leaderboard.Game, "grand-legions"));

        // Assert
        Assert.Equal(
            (1, 0, 0),
            (board.FindAll("p.problem").Count, board.FindAll(".leaders").Count, board.FindAll(".no-leaders").Count)
        );
    }

    [Fact]
    public void ChooseBoard_WhenTheBotsAreChosen_ListsThemByRatingWithPointsAndHowTheyDidAgainstPeople()
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
                    Elo = 2600,
                },
            ]
        );
        IRenderedComponent<Leaderboard> board = this.Render();

        // Act
        board.Find("select.leaderboard-board").Change("bots");

        // Assert
        Assert.Equal(
            ("1 Sage 2600 9 3 1 0 2 1 0", "bots chaturaji-2027"),
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
    public void ChooseBoard_WhenEveryoneIsChosen_ListsPlayersAndBotsTogetherByEloEachMarkedPersonOrBot()
    {
        // Arrange - Sage rated above Ada, Random below her
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [new StandingDto(1, "Ada", 1700, 30, 4, 1, 0) { Points = 25, PlayerId = "ada" }]
        );
        this._page.Server.BotLeaderboards = this._page.Server.BotLeaderboards.Add(
            "chaturaji-2027",
            [
                new BotStandingDto(1, "Random", 2, 0, 6, 1) { BotId = "random", Elo = 1000 },
                new BotStandingDto(2, "Sage", 40, 5, 0, 1) { BotId = "sage", Elo = 2600 },
            ]
        );
        IRenderedComponent<Leaderboard> board = this.Render();

        // Act
        board.Find("select.leaderboard-board").Change("everyone");

        // Assert
        Assert.Equal(
            (
                "1 🤖Sage 2600 40 5 0 1|2 🧑Ada 1700 25 4 1 0|3 🤖Random 1000 2 0 6 1",
                "Bot,Person,Bot"
            ),
            (
                Rows(board),
                string.Join(",", board.FindAll(".everyone .kind").Select(kind => kind.GetAttribute("aria-label")))
            )
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
        IRenderedComponent<Leaderboard> board = this.Render();
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
    public void Render_WhenShown_ExplainsWhatPointsAre()
    {
        // Arrange
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [new StandingDto(1, "Ada", 1516, 0, 1, 0, 0)]
        );

        // Act
        IRenderedComponent<Leaderboard> board = this.Render();

        // Assert
        Assert.Equal(
            ("The points scored at the board, in every game finished, won or lost", true),
            (
                board.Find("th:has(button.sort.points)").GetAttribute("title"),
                board.Find(".about-standings").TextContent.Contains("Points are", StringComparison.Ordinal)
            )
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
        IRenderedComponent<Leaderboard> board = this.Render();

        // Act
        board.Find(".leaders td.name button.profile").Click();

        // Assert
        Assert.Equal(
            ("Ada Lovelace", "Elo 1516, 0 experience; won 1, lost 0, drawn 0 at this game."),
            (board.Find(".public-profile h2").TextContent, board.Find(".public-profile p.player-standing").TextContent)
        );
    }

    [Fact]
    public void Name_WhenABotsIsClicked_OpensItsProfile()
    {
        // Arrange
        this._page.Server.BotLeaderboards = this._page.Server.BotLeaderboards.Add(
            "chaturaji-2027",
            [new BotStandingDto(1, "Random", 0, 3, 1, 0) { BotId = "random" }]
        );
        IRenderedComponent<Leaderboard> board = this.Render();
        board.Find("select.leaderboard-board").Change("bots");

        // Act
        board.Find(".leaders td.name button.bot-profile").Click();

        // Assert
        Assert.Equal("Random", board.Find(".public-profile h2").TextContent);
    }

    [Fact]
    public void Paging_WhenMoreThanTenStand_ShowsTenAPageAndTurnsThePagesBothWays()
    {
        // Arrange - fifteen players, by Elo
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [.. Enumerable.Range(1, 15).Select(place => new StandingDto(place, $"P{place}", 2000 - place, 0, 0, 0, 0))]
        );
        IRenderedComponent<Leaderboard> board = this.Render();
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
        IRenderedComponent<Leaderboard> board = this.Render();
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
    [InlineData("everyone", "No one has finished a game of it yet.")]
    [InlineData("players", "No one signed in has finished a game of it yet.")]
    public void ChooseBoard_WhenTheBoardIsEmpty_SaysNoOneIsOnIt(string chosen, string expected)
    {
        // Arrange
        IRenderedComponent<Leaderboard> board = this.Render();

        // Act
        board.Find("select.leaderboard-board").Change(chosen);

        // Assert
        Assert.Equal(expected, board.Find(".no-leaders").TextContent);
    }

    [Theory]
    [InlineData("bots")]
    [InlineData("everyone")]
    public void ChooseBoard_WhenTheServerCannotBeReached_SaysWhyAndListsNoOne(string chosen)
    {
        // Arrange
        IRenderedComponent<Leaderboard> board = this.Render();
        this._page.Server.Failure = new HttpRequestException("unreachable");

        // Act
        board.Find("select.leaderboard-board").Change(chosen);

        // Assert
        Assert.Equal(
            (1, 0, 0),
            (board.FindAll("p.problem").Count, board.FindAll(".leaders").Count, board.FindAll(".no-leaders").Count)
        );
    }

    /// <summary>The leaderboard of Chaturaji 2027.</summary>
    private IRenderedComponent<Leaderboard> Render() =>
        this._page.Render<Leaderboard>(parameters => parameters.Add(leaderboard => leaderboard.Game, "chaturaji-2027"));

    /// <summary>Each row shown, its cells by spaces, the rows by bars.</summary>
    private static string Rows(IRenderedComponent<Leaderboard> board) =>
        string.Join(
            "|",
            board.FindAll(".leaders tbody tr").Select(row =>
                string.Join(" ", row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim()))
            )
        );

    /// <summary>The names on the page shown, in order.</summary>
    private static string Names(IRenderedComponent<Leaderboard> board) =>
        string.Join(",", board.FindAll(".leaders td.name").Select(cell => cell.TextContent.Trim()));
}
