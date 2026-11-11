using System.Collections.Immutable;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Net.NowhereAtAll.Zaturnava.Contracts;
using Net.NowhereAtAll.Zaturnava.Presentation;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// What the page asks of the browser itself (wwwroot/js/zaturnava.js): sounds, a memory that outlives
/// the page (seats held, the player signed in or a guest's nickname, and settings: sound, pieces, the piece cards
/// and where they open, how a game is set up), the colour theme, word of the page shown again, a piece card to
/// drag, and a window to sign in. A player signed in keeps the page's settings (<see cref="Kept"/>) with the server
/// too, as they choose them, and every browser they sign in on starts from them (<see cref="Adopt"/>).
/// </summary>
internal sealed class Browser(IJSRuntime js, IGameServing games)
{
    public const string Dark = "dark";
    public const string Light = "light";

    private const string SeatKey = "zaturnava.seat.";
    private const string ThemeKey = "zaturnava.theme";
    private const string SoundKey = "zaturnava.sound";
    private const string SafetyKey = "zaturnava.safety";
    private const string PiecesKey = "zaturnava.pieces";
    private const string CardsKey = "zaturnava.cards";
    private const string CardPlaceKey = "zaturnava.card-place";
    private const string LogKey = "zaturnava.log";
    private const string ChatOrderKey = "zaturnava.chat-order";
    private const string AiTalkKey = "zaturnava.ai-talk";
    private const string Hidden = "hidden";
    private const string Shown = "shown";
    private const string PreviewKey = "zaturnava.previews";
    private const string PlayerKey = "zaturnava.player";
    private const string NicknameKey = "zaturnava.nickname";
    private const string AliasKey = "zaturnava.alias";
    private const string SetupKey = "zaturnava.setup";
    private const string LastGameKey = "zaturnava.last-game";
    private const string SetupOfKey = "zaturnava.setup-of.";
    private const string Custom = "strength";
    private const string SeatBySeat = "seats";
    private const string Everything = "everything";
    private const string OldestFirst = "oldest";
    private const string ChessPieces = "chess";
    private const string Off = "off";

    /// <summary>
    /// The settings a player signed in keeps with the server, by the names the browser keeps them by: how the page
    /// looks and sounds, and how it plays and sets games up; not seats, sign-ins or a game's own setup.
    /// </summary>
    private static readonly ImmutableHashSet<string> Kept =
    [
        ThemeKey, SoundKey, SafetyKey, PiecesKey, CardsKey, LogKey, ChatOrderKey, AiTalkKey, PreviewKey, SetupKey,
    ];

    private readonly IJSRuntime _js = js;
    private readonly IGameServing _games = games;

    /// <summary>
    /// Takes up the settings the player signed in keeps with the server, in place of this browser's; none, if no one
    /// is signed in, or the server cannot say. A setting the server keeps nothing of stays as it was.
    /// </summary>
    public async Task Adopt()
    {
        if (await this.PlayerToken() is not { } token)
        {
            return;
        }

        PlayerDto? player = null;
        _ = await Requests.Attempt(async () => player = await this._games.Me(token, CancellationToken.None));
        foreach (KeyValuePair<string, string> kept in player?.Preferences.Where(IsKept) ?? [])
        {
            await this.Store(kept.Key, kept.Value);
        }
    }

    private static bool IsKept(KeyValuePair<string, string> setting) => Kept.Contains(setting.Key);

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

    /// <summary>Shows the page in the theme, as it is kept in this browser.</summary>
    public ValueTask ShowTheme(string theme) => this._js.InvokeVoidAsync("zaturnava.theme", theme);

    /// <summary>Shows the page in the theme the player chose, and keeps it.</summary>
    public async ValueTask SetTheme(string theme)
    {
        await this.ShowTheme(theme);
        await this.Save(ThemeKey, theme);
    }

    public async ValueTask<bool> IsSoundOn() => await this.Load(SoundKey) != Off;

    public ValueTask SetSound(bool isOn) => this.Save(SoundKey, isOn ? "on" : Off);

    /// <summary>Whether the safety keeps the player from taking their own team's pieces: unless turned off.</summary>
    public async ValueTask<bool> IsSafetyOn() => await this.Load(SafetyKey) != Off;

    public ValueTask SetSafety(bool isOn) => this.Save(SafetyKey, isOn ? "on" : Off);

    /// <summary>Whether a piece clicked on is told of on a card: unless the player turned the cards off.</summary>
    public async ValueTask<bool> AreCardsShown() => await this.Load(CardsKey) != Off;

    public ValueTask SetCards(bool isShown) => this.Save(CardsKey, isShown ? "on" : Off);

    /// <summary>
    /// Lets the player drag the piece card by its header wherever they like; it opens where they last put it.
    /// </summary>
    public ValueTask MakeDraggable(ElementReference card) =>
        this._js.InvokeVoidAsync("zaturnava.draggable", card, CardPlaceKey);

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

    /// <summary>Whether the talk at the table leaves out what AIs say: only if the player chose so.</summary>
    public async ValueTask<bool> IsAiTalkHidden() => await this.Load(AiTalkKey) == Hidden;

    public ValueTask SetAiTalkHidden(bool isHidden) => this.Save(AiTalkKey, isHidden ? Hidden : Shown);

    /// <summary>How the lobby last set up a new game: a quick game, unless the player chose otherwise.</summary>
    /// <remarks>Seat by seat, no longer offered, is set up as the player chooses too.</remarks>
    public async ValueTask<SetupMode> Setup() =>
        await this.Load(SetupKey) is Custom or SeatBySeat ? SetupMode.Custom : SetupMode.Quick;

    public ValueTask SetSetup(SetupMode mode) => this.Save(SetupKey, mode == SetupMode.Custom ? Custom : "quick");

    /// <summary>Whether the lobby shows preview games: only if the player chose so.</summary>
    public async ValueTask<bool> ArePreviewsShown() => await this.Load(PreviewKey) == Shown;

    public ValueTask SetPreviewsShown(bool isShown) => this.Save(PreviewKey, isShown ? Shown : Hidden);

    /// <summary>The game the player last started, if they have.</summary>
    public ValueTask<string?> LastGame() => this.Load(LastGameKey);

    /// <summary>How the player last set up <paramref name="game"/> to start it, if they have.</summary>
    public async ValueTask<GameSetup?> SetupOf(string game) => GameSetup.Parse(await this.Load(SetupOfKey + game));

    /// <summary>Remembers the game the player starts, and how they set it up.</summary>
    public async ValueTask RememberSetup(string game, GameSetup setup)
    {
        await this.Save(LastGameKey, game);
        await this.Save(SetupOfKey + game, setup.Text);
    }

    /// <summary>The token of the player signed in on this browser, if one is.</summary>
    public async ValueTask<string?> PlayerToken() => await this.Load(PlayerKey) is { Length: > 0 } token ? token : null;

    public ValueTask RememberPlayer(string token) => this.Save(PlayerKey, token);

    public ValueTask ForgetPlayer() => this.Save(PlayerKey, string.Empty);

    /// <summary>The nickname this browser plays by as a guest, at every table, until cleared; none, if none.</summary>
    public async ValueTask<string?> Nickname() =>
        await this.Load(NicknameKey) is { Length: > 0 } nickname ? nickname : null;

    /// <summary>
    /// Who takes a seat from this browser: the player signed in, if one is, and a guest's nickname and alias token.
    /// </summary>
    public async ValueTask<Claimant> Claimant() =>
        new(await this.PlayerToken(), await this.Nickname(), await this.AliasToken());

    /// <summary>Keeps the nickname to play by as a guest; an empty one clears it.</summary>
    public ValueTask SetNickname(string nickname) => this.Save(NicknameKey, nickname.Trim());

    /// <summary>The token this browser holds its guest's alias by (<see cref="GuestAliasDto"/>), if any.</summary>
    public async ValueTask<string?> AliasToken() => await this.Load(AliasKey) is { Length: > 0 } token ? token : null;

    /// <summary>Keeps the token the guest's alias is held by; an empty one forgets it.</summary>
    public ValueTask RememberAlias(string token) => this.Save(AliasKey, token);

    /// <summary>
    /// Opens a window of its own at <paramref name="url"/>, to sign in, and has <paramref name="listener"/> told
    /// (its <c>SignedIn</c>) the token that window posts back, if it comes from the server's own origin.
    /// </summary>
    public ValueTask SignIn<TListener>(Uri url, DotNetObjectReference<TListener> listener)
        where TListener : class =>
        this._js.InvokeVoidAsync("zaturnava.signIn", url.AbsoluteUri, listener);

    private ValueTask<string?> Load(string key) => this._js.InvokeAsync<string?>("zaturnava.load", key);

    /// <summary>Keeps the setting in this browser; and with the server, if kept so and a player is signed in.</summary>
    private async ValueTask Save(string key, string value)
    {
        await this.Store(key, value);
        PreferRequest preferring = new(key, value);
        _ = Kept.Contains(key) && await this.PlayerToken() is { } token
            ? await Requests.Attempt(() => this._games.Prefer(token, preferring, CancellationToken.None))
            : null;
    }

    private ValueTask Store(string key, string value) => this._js.InvokeVoidAsync("zaturnava.save", key, value);
}
