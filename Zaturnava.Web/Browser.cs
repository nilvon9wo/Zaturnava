using System.Globalization;
using Microsoft.JSInterop;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// What the page asks of the browser itself (wwwroot/js/zaturnava.js): sounds, a memory that outlives
/// the page, and the colour theme.
/// </summary>
internal sealed class Browser(IJSRuntime js)
{
    public const string Dark = "dark";
    public const string Light = "light";

    private const string SeatKey = "zaturnava.seat.";
    private const string ThemeKey = "zaturnava.theme";
    private const string SoundKey = "zaturnava.sound";
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

    private ValueTask<string?> Load(string key) => this._js.InvokeAsync<string?>("zaturnava.load", key);

    private ValueTask Save(string key, string value) => this._js.InvokeVoidAsync("zaturnava.save", key, value);
}
