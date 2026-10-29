using Bunit;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class BotGuideTest : IDisposable
{
    private readonly PageContext _page = new();

    public void Dispose() => this._page.Dispose();

    [Fact]
    public void Render_WhenLoaded_ListsTheBotsEasiestFirstWithTheirRatings()
    {
        // Arrange - Claude is not rated, so comes last

        // Act
        IRenderedComponent<BotGuide> guide = this._page.Render<BotGuide>();

        // Assert
        Assert.Equal(
            "Random Elo 1000|Midas Elo 1100|Solomon Elo 1250|Oracle Elo 1400|Claude Not rated yet",
            string.Join(
                "|",
                guide.FindAll(".bots h3").Select(heading =>
                    $"{heading.QuerySelector(".bot-name")!.TextContent} {heading.QuerySelector(".rating")!.TextContent}"
                )
            )
        );
    }

    [Fact]
    public void Render_WhenABotUsesAiOrIsPaidFor_SaysSo()
    {
        // Arrange - Oracle uses AI; Claude does too, and costs tokens

        // Act
        IRenderedComponent<BotGuide> guide = this._page.Render<BotGuide>();

        // Assert
        Assert.Equal(
            (1, 1, "Asks Claude."),
            (
                guide.FindAll(".bots .ai").Count,
                guide.FindAll(".bots .costs").Count,
                guide.FindAll(".bots .description")[^1].TextContent
            )
        );
    }

    [Fact]
    public void Render_WhenTheServerCannotBeReached_SaysWhy()
    {
        // Arrange
        this._page.Server.Failure = new HttpRequestException("unreachable");

        // Act
        IRenderedComponent<BotGuide> guide = this._page.Render<BotGuide>();

        // Assert
        Assert.Equal((1, 0), (guide.FindAll("p.problem").Count, guide.FindAll(".bots").Count));
    }
}
