using System.Collections.Immutable;
using System.Globalization;
using Microsoft.JSInterop;
using Net.NowhereAtAll.Zaturnava.Presentation;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// What the page asks of the browser itself (wwwroot/js/zaturnava.js): sounds, a memory that outlives
/// the page (seats held, and settings: sound and pieces), the colour theme, and word of the page shown again.
/// </summary>
internal sealed class Browser(IJSRuntime js)
{
    public const string Dark = "dark";
    public const string Light = "light";

    private const string SeatKey = "zaturnava.seat.";
    private const string ThemeKey = "zaturnava.theme";
    private const string SoundKey = "zaturnava.sound";
    private const string SafetyKey = "zaturnava.safety";
    private const string PiecesKey = "zaturnava.pieces";
    private const string LogKey = "zaturnava.log";
    private const string ChatOrderKey = "zaturnava.chat-order";
    private const string Everything = "everything";
    private const string OldestFirst = "oldest";
    private const string ChessPieces = "chess";
    private const string Off = "off";

    private readonly IJSRuntime _js = js;

    public ValueTask Play(ImmutableList<string> sounds) => this._js.InvokeVoidAsync("zaturnava.play", [.. sounds]);

    /// <summary>Has the table told (its <c>Shown</c>) whenever the page is shown again, after being hidden.</summary>
    public ValueTask Watch<TTable>(DotNetObjectReference<TTable> table)
        where TTable : class =>
        this._js.InvokeVoidAsync("zaturnava.watch", table);

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

    /// <summary>Whether the safety keeps the player from taking their own team's pieces: unless turned off.</summary>
    public async ValueTask<bool> IsSafetyOn() => await this.Load(SafetyKey) != Off;

    public ValueTask SetSafety(bool isOn) => this.Save(SafetyKey, isOn ? "on" : Off);

    /// <summary>The pieces as the player last chose to see them: chaturaji, unless they chose chess.</summary>
    public async ValueTask<PieceSet> Pieces() =>
        await this.Load(PiecesKey) == ChessPieces ? PieceSet.Chess : PieceSet.Chaturaji;

    public ValueTask SetPieces(PieceSet pieces) =>
        this.Save(PiecesKey, pieces == PieceSet.Chess ? ChessPieces : "chaturaji");

    /// <summary>Whether the log shows only its highlights: it does unless the player chose to see everything.</summary>
    public async ValueTask<bool> IsLogHighlightsOnly() => await this.Load(LogKey) != Everything;

    public ValueTask SetLogHighlightsOnly(bool isHighlightsOnly) =>
        this.Save(LogKey, isHighlightsOnly ? "highlights" : Everything);

    /// <summary>Whether the talk at the table shows its newest message first: unless the player chose oldest.</summary>
    public async ValueTask<bool> IsChatNewestFirst() => await this.Load(ChatOrderKey) != OldestFirst;

    public ValueTask SetChatNewestFirst(bool isNewestFirst) =>
        this.Save(ChatOrderKey, isNewestFirst ? "newest" : OldestFirst);

    private ValueTask<string?> Load(string key) => this._js.InvokeAsync<string?>("zaturnava.load", key);

    private ValueTask Save(string key, string value) => this._js.InvokeVoidAsync("zaturnava.save", key, value);
}
