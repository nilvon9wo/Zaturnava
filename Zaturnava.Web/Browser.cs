using System.Globalization;
using Microsoft.JSInterop;
using Net.NowhereAtAll.Zaturnava.Presentation;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// What the page asks of the browser itself (wwwroot/js/zaturnava.js): sounds, a memory that outlives
/// the page (seats held, and settings: sound and pieces), and the colour theme.
/// </summary>
internal sealed class Browser(IJSRuntime js)
{
    public const string Dark = "dark";
    public const string Light = "light";

    private const string SeatKey = "zaturnava.seat.";
    private const string ThemeKey = "zaturnava.theme";
    private const string SoundKey = "zaturnava.sound";
    private const string PiecesKey = "zaturnava.pieces";
    private const string LogKey = "zaturnava.log";
    private const string Everything = "everything";
    private const string ChessPieces = "chess";
    private const string Off = "off";

    private readonly IJSRuntime _js = js;

    public ValueTask Play(string sound) => this._js.InvokeVoidAsync("zaturnava.play", sound);

    /// <summary>Remembers the seat held at a game, so that the player can come back to it.</summary>
    public ValueTask Remember(string gameId, SeatClaim claim) =>
        this.Save(SeatKey + gameId, string.Create(CultureInfo.InvariantCulture, $"{claim.Seat} {claim.Token}"));

    /// <summary>The seat this browser held at a game, if it remembers one.</summary>
    public async ValueTask<SeatClaim?> Recall(string gameId) =>
        await this.Load(SeatKey + gameId) is { } text
        && text.Split(' ') is [{ } seat, { } token]
        && int.TryParse(seat, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
            ? new SeatClaim(number, token)
            : null;

    public async ValueTask<string> Theme() => await this.Load(ThemeKey) == Light ? Light : Dark;

    public async ValueTask ShowTheme(string theme)
    {
        await this._js.InvokeVoidAsync("zaturnava.theme", theme);
        await this.Save(ThemeKey, theme);
    }

    public async ValueTask<bool> IsSoundOn() => await this.Load(SoundKey) != Off;

    public ValueTask SetSound(bool isOn) => this.Save(SoundKey, isOn ? "on" : Off);

    /// <summary>The pieces as the player last chose to see them: traditional, unless they chose chess.</summary>
    public async ValueTask<PieceSet> Pieces() =>
        await this.Load(PiecesKey) == ChessPieces ? PieceSet.Chess : PieceSet.Traditional;

    public ValueTask SetPieces(PieceSet pieces) =>
        this.Save(PiecesKey, pieces == PieceSet.Chess ? ChessPieces : "traditional");

    /// <summary>Whether the log shows only its highlights: it does unless the player chose to see everything.</summary>
    public async ValueTask<bool> IsLogHighlightsOnly() => await this.Load(LogKey) != Everything;

    public ValueTask SetLogHighlightsOnly(bool isHighlightsOnly) =>
        this.Save(LogKey, isHighlightsOnly ? "highlights" : Everything);

    private ValueTask<string?> Load(string key) => this._js.InvokeAsync<string?>("zaturnava.load", key);

    private ValueTask Save(string key, string value) => this._js.InvokeVoidAsync("zaturnava.save", key, value);
}
