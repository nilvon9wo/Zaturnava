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
    public void Open_WhenThePlayerDescribesThemselvesAndShowsServices_ShowsBoth()
    {
        // Arrange
        ProfileDto described = Ada with { Description = "Counts.", Services = ["GitHub", "Google"] };
        this._page.Server.Profiles = this._page.Server.Profiles.Add("p", described);
        IRenderedComponent<SeatPlayer> player = this.Rendered("p");

        // Act
        player.Find("button.player").Click();

        // Assert
        Assert.Equal(
            "Ada|Counts.|Signs in with GitHub, Google.|Not yet on this game's leaderboard.",
            string.Join('|', player.FindAll("[role=dialog] h2, [role=dialog] p").Select(line => line.TextContent))
        );
    }

    [Fact]
    public void Open_WhenThePlayerStandsAtTheTablesGame_ShowsTheirEloExperienceAndRecordThere()
    {
        // Arrange - Ada stands on Chaturaji 2027's leaderboard, the game of the table's ruleset
        this._page.Server.Profiles = this._page.Server.Profiles.Add("p", Ada);
        this._page.Server.Leaderboards = this._page.Server.Leaderboards.Add(
            "chaturaji-2027",
            [new StandingDto(1, "Ada", 1516, 450, 3, 2, 1) { PlayerId = "p" }]
        );
        IRenderedComponent<SeatPlayer> player = this._page.Render<SeatPlayer>(parameters =>
            parameters.Add(x => x.Text, "Ada").Add(x => x.PlayerId, "p").Add(x => x.RulesetId, "chaturaji-2027")
        );

        // Act
        player.Find("button.player").Click();

        // Assert - laid out as a table, each figure under its heading
        Assert.Equal(
            ("Elo|Experience|Won|Lost|Drawn", "1516|450|3|2|1"),
            (
                string.Join('|', player.FindAll("table.player-standing th").Select(cell => cell.TextContent)),
                string.Join('|', player.FindAll("table.player-standing td").Select(cell => cell.TextContent))
            )
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
        Assert.Equal(SeatPlayer.GonePlayer, player.Find("[role=dialog] [role=alert]").TextContent);
    }

    [Theory]
    [InlineData("random", "Random|Any move.", "Plain algorithm", "1000")]
    [InlineData("oracle", "Oracle|Asks a model.", "AI", "1400")]
    [InlineData("claude", "Claude|Asks Claude.", "AI, paid per use", null)]
    public void OpenBot_WhenTheBotIsOffered_ShowsWhatItDoesAndItsRatingAtTheGame(
        string botId,
        string about,
        string kind,
        string? rated
    )
    {
        // Arrange
        IRenderedComponent<SeatPlayer> bot = this.RenderedBot(botId);

        // Act
        bot.Find("button.bot-profile").Click();

        // Assert - each fact by its label
        Assert.Equal(
            (about, $"Plays by|{kind}|Elo at this game|{rated ?? "Not yet rated"}"),
            (
                string.Join('|', bot.FindAll("[role=dialog] h2, [role=dialog] p").Select(line => line.TextContent)),
                string.Join('|', bot.FindAll("dl.bot-facts dt, dl.bot-facts dd").Select(line => line.TextContent))
            )
        );
    }

    [Fact]
    public void OpenBot_WhenTheBotIsNoLongerOffered_SaysSo()
    {
        // Arrange
        IRenderedComponent<SeatPlayer> bot = this.RenderedBot("gone");

        // Act
        bot.Find("button.bot-profile").Click();

        // Assert
        Assert.Equal(SeatPlayer.GoneBot, bot.Find("[role=dialog] [role=alert]").TextContent);
    }

    [Fact]
    public void OpenBot_WhenTheServerCannotBeAsked_SaysWhy()
    {
        // Arrange
        this._page.Server.Failure = new HttpRequestException("asleep");
        IRenderedComponent<SeatPlayer> bot = this.RenderedBot("random");

        // Act
        bot.Find("button.bot-profile").Click();

        // Assert
        Assert.Equal("asleep", bot.Find("[role=dialog] [role=alert]").TextContent);
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

    private IRenderedComponent<SeatPlayer> RenderedBot(string botId) =>
        this._page.Render<SeatPlayer>(parameters =>
            parameters.Add(x => x.Text, "Random").Add(x => x.BotId, botId).Add(x => x.RulesetId, "chaturaji-2027")
        );
}
