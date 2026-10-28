using Bunit;
using Net.NowhereAtAll.Zaturnava.Presentation;

namespace Net.NowhereAtAll.Zaturnava.Web.Test;

public sealed class BrowserTest : IDisposable
{
    private const string SeatKey = "zaturnava.seat.g";

    private readonly BunitContext _context = new();
    private readonly Browser _browser;

    public BrowserTest()
    {
        this._context.JSInterop.Mode = JSRuntimeMode.Loose;
        this._browser = new Browser(this._context.JSInterop.JSRuntime);
    }

    public void Dispose() => this._context.Dispose();

    [Fact]
    public async Task Remember_WhenGivenASeat_SavesItForTheGame()
    {
        // Arrange - nothing beyond the browser

        // Act
        await this._browser.Remember("g", new SeatClaim(3, "secret"));

        // Assert
        Assert.Equal([SeatKey, "3 secret"], this._context.JSInterop.Invocations["zaturnava.save"].Single().Arguments);
    }

    [Theory]
    [InlineData("3 secret", 3)]
    [InlineData("three secret", null)]
    [InlineData("secret", null)]
    [InlineData(null, null)]
    public async Task Recall_WhenSomethingIsRemembered_ReadsTheSeatIfItCan(string? remembered, int? expected)
    {
        // Arrange
        _ = this._context.JSInterop.Setup<string?>("zaturnava.load", SeatKey).SetResult(remembered);

        // Act
        SeatClaim? claim = await this._browser.Recall("g");

        // Assert
        Assert.Equal(expected, claim?.Seat);
    }

    [Theory]
    [InlineData("light", Browser.Light)]
    [InlineData("dark", Browser.Dark)]
    [InlineData(null, Browser.Dark)]
    public async Task Theme_WhenRead_IsTheOneChosenElseDark(string? remembered, string expected)
    {
        // Arrange
        _ = this._context.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.theme").SetResult(remembered);

        // Act
        string theme = await this._browser.Theme();

        // Assert
        Assert.Equal(expected, theme);
    }

    [Theory]
    [InlineData("off", false)]
    [InlineData(null, true)]
    public async Task IsSoundOn_WhenRead_IsOnUnlessTurnedOff(string? remembered, bool expected)
    {
        // Arrange
        _ = this._context.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.sound").SetResult(remembered);

        // Act
        bool isOn = await this._browser.IsSoundOn();

        // Assert
        Assert.Equal(expected, isOn);
    }

    [Theory]
    [InlineData("chess", PieceSet.Chess)]
    [InlineData(null, PieceSet.Traditional)]
    public async Task Pieces_WhenRead_AreTraditionalUnlessChessWasChosen(string? remembered, PieceSet expected)
    {
        // Arrange
        _ = this._context.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.pieces").SetResult(remembered);

        // Act
        PieceSet pieces = await this._browser.Pieces();

        // Assert
        Assert.Equal(expected, pieces);
    }

    [Theory]
    [InlineData(PieceSet.Chess, "chess")]
    [InlineData(PieceSet.Traditional, "traditional")]
    public async Task SetPieces_WhenSet_RemembersThem(PieceSet pieces, string expected)
    {
        // Arrange - nothing beyond the browser

        // Act
        await this._browser.SetPieces(pieces);

        // Assert
        Assert.Equal(
            ["zaturnava.pieces", expected],
            this._context.JSInterop.Invocations["zaturnava.save"].Single().Arguments
        );
    }

    [Theory]
    [InlineData("everything", false)]
    [InlineData(null, true)]
    public async Task IsLogHighlightsOnly_WhenRead_IsUnlessEverythingWasChosen(string? remembered, bool expected)
    {
        // Arrange
        _ = this._context.JSInterop.Setup<string?>("zaturnava.load", "zaturnava.log").SetResult(remembered);

        // Act
        bool isHighlightsOnly = await this._browser.IsLogHighlightsOnly();

        // Assert
        Assert.Equal(expected, isHighlightsOnly);
    }

    [Theory]
    [InlineData(true, "highlights")]
    [InlineData(false, "everything")]
    public async Task SetLogHighlightsOnly_WhenSet_RemembersIt(bool isHighlightsOnly, string expected)
    {
        // Arrange - nothing beyond the browser

        // Act
        await this._browser.SetLogHighlightsOnly(isHighlightsOnly);

        // Assert
        Assert.Equal(
            ["zaturnava.log", expected],
            this._context.JSInterop.Invocations["zaturnava.save"].Single().Arguments
        );
    }

    [Theory]
    [InlineData(true, "on")]
    [InlineData(false, "off")]
    public async Task SetSound_WhenSet_RemembersIt(bool isOn, string expected)
    {
        // Arrange - nothing beyond the browser

        // Act
        await this._browser.SetSound(isOn);

        // Assert
        Assert.Equal(
            ["zaturnava.sound", expected],
            this._context.JSInterop.Invocations["zaturnava.save"].Single().Arguments
        );
    }
}
