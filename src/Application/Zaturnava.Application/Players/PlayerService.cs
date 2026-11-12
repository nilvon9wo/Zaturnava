using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Signing in, which is optional: a provider vouches for a login, and the player whose login it is (made the first
/// time, named as the provider names them) gets a token, as a seat's is made (<see cref="SeatTokens"/>), to show
/// from then on. Only its hash is kept. A player may sign in with several services (<see cref="Link"/>), name
/// themselves as they please (<see cref="Rename"/>), and give an AI a token of its own to play for them
/// (<see cref="AiToken"/>), which may do nothing to their account. When each last signed in is kept.
/// </summary>
public sealed class PlayerService(
    IPlayerPersisting store,
    IGamePersisting games,
    LinkCodes links,
    TimeProvider clock,
    GuestAliases aliases
)
{
    /// <summary>The longest a player's name may be, in characters; a longer one is cut.</summary>
    public const int MaximumNameLength = 40;

    /// <summary>How an AI's token begins: it is part of what is hashed, so it cannot be taken off.</summary>
    public const string AiTokenPrefix = "ai-";

    /// <summary>A new player's name, when the provider gives none.</summary>
    public const string Unnamed = "Player";

    /// <summary>Whoever holds a person's seat without having signed in.</summary>
    public const string Guest = "guest";

    /// <summary>Whoever held a person's seat as a player who has since deleted their account.</summary>
    public const string Deleted = "deleted player";

    /// <summary>The longest a setting's name or value may be, in characters (<see cref="Prefer"/>).</summary>
    public const int MaximumPreferenceLength = 64;

    /// <summary>The most settings a player may keep (<see cref="Prefer"/>).</summary>
    public const int MostPreferences = 32;

    /// <summary>The longest a profile's description may be, in characters; a longer one is cut.</summary>
    public const int MaximumDescriptionLength = 500;

    private readonly IPlayerPersisting _store = store;
    private readonly IGamePersisting _games = games;
    private readonly LinkCodes _links = links;
    private readonly TimeProvider _clock = clock;
    private readonly GuestAliases _aliases = aliases;

    /// <summary>
    /// Signs in the player whose login it is: the one already known, else a new one, named
    /// <paramref name="name"/>, or, if another player goes by it, by the first of "name 2", "name 3" and so on that
    /// no one does (Proposed); either way with a new token.
    /// </summary>
    public async Task<SignedIn> SignIn(Login login, string? name, CancellationToken cancellationToken)
    {
        Player player = await this._store.ByLogin(login, cancellationToken)
            ?? await this.Add(
                new Player(PlayerId.New(), await this.FreeName(NameOf(name) ?? Unnamed, cancellationToken), [login]),
                cancellationToken
            );
        return await this.Session(player, cancellationToken);
    }

    /// <summary>
    /// <paramref name="name"/>, if no player goes by it; else the first of "name 2", "name 3" and so on that no one
    /// does, the name cut so that each fits <see cref="MaximumNameLength"/>.
    /// </summary>
    private async Task<string> FreeName(string name, CancellationToken cancellationToken)
    {
        string free = name;
        for (int number = 2; await this.IsTaken(free, null, cancellationToken); number++)
        {
            free = Numbered(name, number, MaximumNameLength);
        }

        return free;
    }

    /// <summary>"name 2", say, the name cut so that it fits <paramref name="longest"/>.</summary>
    private static string Numbered(string name, int number, int longest)
    {
        string suffix = $" {number.ToString(CultureInfo.InvariantCulture)}";
        return name[..Math.Min(name.Length, longest - suffix.Length)].TrimEnd() + suffix;
    }

    /// <summary>
    /// Whether a player but <paramref name="except"/> goes by <paramref name="name"/>, or a guest holds it as their
    /// alias (<see cref="GuestAliases"/>): either way, no one else may take it.
    /// </summary>
    private async Task<bool> IsTaken(string name, PlayerId? except, CancellationToken cancellationToken) =>
        await this._store.IsNameTaken(name, except, cancellationToken) || this._aliases.IsHeld(name);

    /// <summary>
    /// Has a guest take <paramref name="wanted"/> as their alias for the visit (<see cref="Nicknames.Of"/>; "Guest",
    /// if nothing is left of it), or, if a player goes by it or another guest holds it, the first of "alias 2",
    /// "alias 3" and so on that is free (Stated 2026-11-11); held by <paramref name="token"/>, if the guest gives the
    /// one they hold an alias by already, letting that go, else by a new one.
    /// </summary>
    public async Task<GuestAlias> TakeAlias(string? wanted, string? token, CancellationToken cancellationToken)
    {
        string holder = token ?? SeatTokens.Issue();
        string hash = SeatTokens.Hash(holder);
        string alias = Nicknames.Of(wanted) ?? Nicknames.Unnamed;
        string free = alias;
        for (int number = 2;
            await this._store.IsNameTaken(free, null, cancellationToken) || !this._aliases.Hold(free, hash);
            number++)
        {
            free = Numbered(alias, number, Nicknames.MaximumLength);
        }

        return new GuestAlias(free, holder);
    }

    /// <summary>
    /// Refuses a guest the <paramref name="nickname"/> they would be shown by at a seat (<see cref="Nicknames.Of"/>)
    /// if a player goes by it, or a guest holds it as their alias but by another token than
    /// <paramref name="aliasToken"/> (Proposed): no guest passes for a player, nor for another guest. A name no one
    /// goes by or holds is anyone's, alias or not, so a program playing as a guest needs none (Stated 2026-11-12).
    /// </summary>
    /// <exception cref="NameTakenException">Someone else goes by the nickname.</exception>
    public async Task CheckGuestName(string? nickname, string? aliasToken, CancellationToken cancellationToken)
    {
        if (Nicknames.Of(nickname) is { } named
            && (await this._store.IsNameTaken(named, null, cancellationToken)
                || this._aliases.IsHeld(named, aliasToken is null ? null : SeatTokens.Hash(aliasToken))))
        {
            throw new NameTakenException(named);
        }
    }

    /// <summary>The alias <paramref name="token"/> holds, held anew; none, if it holds none now.</summary>
    public GuestAlias? RenewAlias([Required] string token) =>
        this._aliases.Renew(SeatTokens.Hash(token)) is { } alias ? new GuestAlias(alias, token) : null;

    /// <summary>
    /// A one-time code (<see cref="LinkCodes"/>) for the player the token stands for to link another login with
    /// (<see cref="Link"/>); none, if it stands for no one.
    /// </summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    public async Task<string?> LinkCode([Required] string token, CancellationToken cancellationToken) =>
        await this.Owner(token, cancellationToken) is { } player ? this._links.Issue(player.Id) : null;

    /// <summary>Whether the token is an AI's (<see cref="AiToken"/>), whether or not it stands for anyone.</summary>
    public static bool IsAiToken(string? token) =>
        token?.StartsWith(AiTokenPrefix, StringComparison.Ordinal) == true;

    /// <summary>
    /// A new token for an AI to play for the player the token stands for (none, if it stands for no one): what it
    /// claims, it claims as an AI, and it may not change their account, nor make more such tokens. Signing out with
    /// it lets it go; deleting the account, every one.
    /// </summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    public async Task<string?> AiToken([Required] string token, CancellationToken cancellationToken)
    {
        if (await this.Owner(token, cancellationToken) is not { } player)
        {
            return null;
        }

        string issued = AiTokenPrefix + SeatTokens.Issue();
        await this._store.AddSession(player.Id, SeatTokens.Hash(issued), cancellationToken);
        return issued;
    }

    /// <summary>
    /// The player as anyone may see them: their name, description and the services they show; none, if no such
    /// player is known.
    /// </summary>
    public Task<Player?> Profile(PlayerId id, CancellationToken cancellationToken) =>
        this._store.ById(id, cancellationToken);

    /// <summary>
    /// Makes <paramref name="login"/> the login of the player <paramref name="code"/> was issued to, and signs them
    /// in, with a new token; from then on, they may sign in with either. If it is another player's, that player is
    /// merged into them (Stated): their every login, session (an AI's too), seat and standing becomes this player's,
    /// and they are no more; this player keeps their own name (Proposed).
    /// </summary>
    /// <exception cref="LinkExpiredException">The code is spent or was never issued, or its player is gone.</exception>
    public async Task<SignedIn> Link([Required] string code, Login login, CancellationToken cancellationToken)
    {
        Player player =
            this._links.Redeem(code) is { } id && await this._store.ById(id, cancellationToken) is { } known
                ? known
                : throw new LinkExpiredException();
        Player? holder = await this._store.ByLogin(login, cancellationToken);
        if (holder is null)
        {
            await this._store.AddLogin(player.Id, login, cancellationToken);
            player = player with { Logins = player.Logins.Add(login) };
        }
        else if (holder.Id != player.Id)
        {
            await this._games.Reclaim(holder.Id, player.Id, cancellationToken);
            await this._store.Merge(holder.Id, player.Id, cancellationToken);
            player = player with { Logins = player.Logins.AddRange(holder.Logins) };
        }

        return await this.Session(player, cancellationToken);
    }

    /// <summary>
    /// Lets go the login, with the service <paramref name="provider"/>, of the player the token stands for (none,
    /// if it stands for no one); having none with that service is no matter.
    /// </summary>
    /// <exception cref="LastLoginException">It is the only login the player has.</exception>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    public async Task<Player?> Unlink(
        [Required] string token,
        [Required] string provider,
        CancellationToken cancellationToken
    )
    {
        if (await this.Owner(token, cancellationToken) is not { } player)
        {
            return null;
        }

        ImmutableList<Login> kept = player.Logins.RemoveAll(login => login.Provider == provider);
        if (kept.IsEmpty)
        {
            throw new LastLoginException(provider);
        }

        foreach (Login login in player.Logins.Except(kept))
        {
            await this._store.RemoveLogin(player.Id, login, cancellationToken);
        }

        return player with { Logins = kept };
    }

    /// <summary>
    /// Names the player the token stands for (none, if it stands for no one) <paramref name="name"/>, without
    /// control characters or the spaces around it, cut to <see cref="MaximumNameLength"/>; a name with nothing left
    /// leaves theirs as it was. No two players may go by one username, capitals or not (Stated).
    /// </summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    /// <exception cref="NameTakenException">Another player goes by the name.</exception>
    public async Task<Player?> Rename([Required] string token, string? name, CancellationToken cancellationToken)
    {
        if (await this.Owner(token, cancellationToken) is not { } player)
        {
            return null;
        }

        if (NameOf(name) is not { } named)
        {
            return player;
        }

        if (await this.IsTaken(named, player.Id, cancellationToken))
        {
            throw new NameTakenException(named);
        }

        await this._store.Rename(player.Id, named, cancellationToken);
        return player with { Name = named };
    }

    /// <summary>
    /// Keeps one of the page's settings (the theme, say) for the player the token stands for, an empty value
    /// forgetting it, so that every browser they sign in on starts from it; none, if it stands for no one.
    /// </summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    /// <exception cref="PreferenceRefusedException">
    /// The name is empty, or it or the value too long (<see cref="MaximumPreferenceLength"/>), or the player would
    /// keep too many (<see cref="MostPreferences"/>).
    /// </exception>
    public async Task<Player?> Prefer(
        [Required] string token,
        [Required(AllowEmptyStrings = true)] string name,
        [Required(AllowEmptyStrings = true)] string value,
        CancellationToken cancellationToken
    )
    {
        if (await this.Owner(token, cancellationToken) is not { } player)
        {
            return null;
        }

        ImmutableSortedDictionary<string, string> preferences = value.Length == 0
            ? player.Preferences.Remove(name)
            : player.Preferences.SetItem(name, value);
        bool isKept = name.Length is > 0 and <= MaximumPreferenceLength
            && value.Length <= MaximumPreferenceLength
            && preferences.Count <= MostPreferences;
        await (isKept
            ? this._store.KeepPreferences(player.Id, preferences, cancellationToken)
            : throw new PreferenceRefusedException());
        return player with { Preferences = preferences };
    }

    /// <summary>
    /// Says what the profile of the player the token stands for (none, if it stands for no one) shows anyone: a
    /// description of themselves, without control characters but line breaks, or the spaces around it, cut to
    /// <see cref="MaximumDescriptionLength"/>; and which of the services they sign in with it names, any other
    /// left out.
    /// </summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    public async Task<Player?> Describe(
        [Required] string token,
        string? description,
        IEnumerable<string>? shown,
        CancellationToken cancellationToken
    )
    {
        if (await this.Owner(token, cancellationToken) is not { } player)
        {
            return null;
        }

        string described = DescriptionOf(description);
        ImmutableSortedSet<string> chosen = Player.NoneShown.Union(
            (shown ?? []).Where(service => player.Logins.Any(login => login.Provider == service))
        );
        await this._store.Describe(player.Id, described, chosen, cancellationToken);
        return player with { Description = described, Shown = chosen };
    }

    /// <summary>
    /// The description without control characters but line breaks (each "\r\n" one), or the spaces around it, cut to
    /// the longest allowed.
    /// </summary>
    private static string DescriptionOf(string? description)
    {
        string kept = new(
            [
                .. (description ?? string.Empty)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Where(letter => letter == '\n' || !char.IsControl(letter)),
            ]
        );
        string trimmed = kept.Trim();
        return trimmed[..Math.Min(trimmed.Length, MaximumDescriptionLength)].TrimEnd();
    }

    /// <summary>The player the token stands for, if it stands for one.</summary>
    public Task<Player?> Find([Required] string token, CancellationToken cancellationToken) =>
        this._store.BySession(SeatTokens.Hash(token), cancellationToken);

    /// <summary>
    /// What to call whoever holds each person's seat: their name, if they were signed in when they claimed it;
    /// <see cref="Deleted"/> if they have since deleted their account; if not signed in, their nickname as a guest
    /// (<see cref="Nicknames.Shown"/>). A seat an AI plays says so: "Ada (AI)", "Sage (Ada's AI)", "Bot (AI guest)".
    /// Bots' and open
    /// seats are left out.
    /// </summary>
    public async Task<ImmutableDictionary<HumanSeat, string>> NamesOf(
        [Required] IEnumerable<SeatController> seats,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<HumanSeat> people = [.. seats.OfType<HumanSeat>().Distinct()];
        ImmutableDictionary<PlayerId, string> names =
            await this._store.Names([.. people.Select(seat => seat.Claimant).OfType<PlayerId>()], cancellationToken);
        return people.ToImmutableDictionary(
            seat => seat,
            seat => seat.Claimant is { } claimant
                ? Nicknames.Labelled(names.GetValueOrDefault(claimant, Deleted), seat.IsAi, seat.Nickname)
                : Nicknames.Shown(seat.Nickname, seat.IsAi)
        );
    }

    /// <summary>Signs out: the token stands for no one from now on.</summary>
    public Task SignOut([Required] string token, CancellationToken cancellationToken) =>
        this._store.RemoveSession(SeatTokens.Hash(token), cancellationToken);

    /// <summary>
    /// Forgets the player the token stands for: their name, logins and sessions, so that nothing left says who
    /// they were. Whether there was one to forget.
    /// </summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    public async Task<bool> Delete([Required] string token, CancellationToken cancellationToken)
    {
        if (await this.Owner(token, cancellationToken) is not { } player)
        {
            return false;
        }

        await this._store.Remove(player.Id, cancellationToken);
        return true;
    }

    /// <summary>
    /// The name, without control characters or the spaces around it, cut to the longest allowed; none, if nothing is
    /// left.
    /// </summary>
    private static string? NameOf(string? name) =>
        Cut(new string([.. (name ?? string.Empty).Where(letter => !char.IsControl(letter))]).Trim());

    private static string? Cut(string name) =>
        name.Length == 0 ? null : name[..Math.Min(name.Length, MaximumNameLength)].TrimEnd();

    /// <summary>The player the token stands for, if it may change their account.</summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    private Task<Player?> Owner(string token, CancellationToken cancellationToken) =>
        IsAiToken(token) ? throw new AiTokenRefusedException() : this.Find(token, cancellationToken);

    private async Task<Player> Add(Player player, CancellationToken cancellationToken)
    {
        await this._store.Add(player, cancellationToken);
        return player;
    }

    /// <summary>The player signed in, now, with a new token.</summary>
    private async Task<SignedIn> Session(Player player, CancellationToken cancellationToken)
    {
        string token = SeatTokens.Issue();
        await this._store.AddSession(player.Id, SeatTokens.Hash(token), cancellationToken);
        await this._store.MarkSignedIn(player.Id, this._clock.GetUtcNow(), cancellationToken);
        return new SignedIn(player, token);
    }
}
