using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Signing in, which is optional: a provider vouches for a login, and the player whose login it is (made the first
/// time, named as the provider names them) gets a token, as a seat's is made (<see cref="SeatTokens"/>), to show
/// from then on. Only its hash is kept. A player may sign in with several services (<see cref="Link"/>), name
/// themselves as they please (<see cref="Rename"/>), show others links on their profile (<see cref="SetLinks"/>),
/// and give an AI a token of its own to play for them (<see cref="AiToken"/>), which may do nothing to their account.
/// </summary>
public sealed class PlayerService(IPlayerPersisting store, LinkCodes links)
{
    /// <summary>The longest a player's name may be, in characters; a longer one is cut.</summary>
    public const int MaximumNameLength = 40;

    /// <summary>The most links a profile may show; any more are let go.</summary>
    public const int MaximumLinks = 5;

    /// <summary>The longest a link may be, in characters.</summary>
    public const int MaximumLinkLength = 200;

    /// <summary>How an AI's token begins: it is part of what is hashed, so it cannot be taken off.</summary>
    public const string AiTokenPrefix = "ai-";

    /// <summary>A new player's name, when the provider gives none.</summary>
    public const string Unnamed = "Player";

    /// <summary>Whoever holds a person's seat without having signed in.</summary>
    public const string Guest = "guest";

    /// <summary>Whoever held a person's seat as a player who has since deleted their account.</summary>
    public const string Deleted = "deleted player";

    private readonly IPlayerPersisting _store = store;
    private readonly LinkCodes _links = links;

    /// <summary>
    /// Signs in the player whose login it is: the one already known, else a new one, named
    /// <paramref name="name"/>; either way with a new token.
    /// </summary>
    public async Task<SignedIn> SignIn(Login login, string? name, CancellationToken cancellationToken)
    {
        Player player = await this._store.ByLogin(login, cancellationToken)
            ?? await this.Add(new Player(PlayerId.New(), NameOf(name) ?? Unnamed, [login]), cancellationToken);
        return await this.Session(player, cancellationToken);
    }

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
    /// Shows <paramref name="links"/> on the profile of the player the token stands for (none, if it stands for no
    /// one), in place of any shown before: each without the spaces around it, blanks and repeats left out, the first
    /// <see cref="MaximumLinks"/> kept.
    /// </summary>
    /// <exception cref="BadLinkException">A link is not an http or https address, or is too long.</exception>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    public async Task<Player?> SetLinks(
        [Required] string token,
        [Required] IEnumerable<string?> links,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<string> kept =
        [
            .. links
                .Select(link => (link ?? string.Empty).Trim())
                .Where(link => link.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Take(MaximumLinks)
                .Select(Checked),
        ];
        if (await this.Owner(token, cancellationToken) is not { } player)
        {
            return null;
        }

        await this._store.SetLinks(player.Id, kept, cancellationToken);
        return player with { Links = kept };
    }

    /// <summary>The player as anyone may see them: their name and links; none, if no such player is known.</summary>
    public Task<Player?> Profile(PlayerId id, CancellationToken cancellationToken) =>
        this._store.ById(id, cancellationToken);

    /// <summary>
    /// Makes <paramref name="login"/> the login of the player <paramref name="code"/> was issued to, and signs them
    /// in, with a new token; from then on, they may sign in with either.
    /// </summary>
    /// <exception cref="LinkExpiredException">The code is spent or was never issued, or its player is gone.</exception>
    /// <exception cref="LoginTakenException">The login is another player's.</exception>
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
            throw new LoginTakenException(login.Provider);
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
    /// leaves theirs as it was.
    /// </summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
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

        await this._store.Rename(player.Id, named, cancellationToken);
        return player with { Name = named };
    }

    /// <summary>The player the token stands for, if it stands for one.</summary>
    public Task<Player?> Find([Required] string token, CancellationToken cancellationToken) =>
        this._store.BySession(SeatTokens.Hash(token), cancellationToken);

    /// <summary>
    /// What to call whoever holds each person's seat: their name, if they were signed in when they claimed it;
    /// <see cref="Deleted"/> if they have since deleted their account; if not signed in, their nickname as a guest
    /// (<see cref="Nicknames.Shown"/>). A seat an AI plays says so: "Ada (AI)", "Bot (AI guest)". Bots' and open
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
                ? Nicknames.Labelled(names.GetValueOrDefault(claimant, Deleted), seat.IsAi)
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

    /// <summary>The link, if it is an http or https address, short enough.</summary>
    /// <exception cref="BadLinkException">It is not.</exception>
    private static string Checked(string link) =>
        link.Length <= MaximumLinkLength
        && Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? link
            : throw new BadLinkException(link);

    /// <summary>The player the token stands for, if it may change their account.</summary>
    /// <exception cref="AiTokenRefusedException">The token is an AI's.</exception>
    private Task<Player?> Owner(string token, CancellationToken cancellationToken) =>
        IsAiToken(token) ? throw new AiTokenRefusedException() : this.Find(token, cancellationToken);

    private async Task<Player> Add(Player player, CancellationToken cancellationToken)
    {
        await this._store.Add(player, cancellationToken);
        return player;
    }

    /// <summary>The player signed in, with a new token.</summary>
    private async Task<SignedIn> Session(Player player, CancellationToken cancellationToken)
    {
        string token = SeatTokens.Issue();
        await this._store.AddSession(player.Id, SeatTokens.Hash(token), cancellationToken);
        return new SignedIn(player, token);
    }
}
