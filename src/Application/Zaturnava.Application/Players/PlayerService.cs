using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Signing in, which is optional: a provider vouches for a login, and the player whose login it is (made the first
/// time, named as the provider names them) gets a token, as a seat's is made (<see cref="SeatTokens"/>), to show
/// from then on. Only its hash is kept. A player may sign in with several services (<see cref="Link"/>), and name
/// themselves as they please (<see cref="Rename"/>).
/// </summary>
public sealed class PlayerService(IPlayerPersisting store, LinkCodes links)
{
    /// <summary>The longest a player's name may be, in characters; a longer one is cut.</summary>
    public const int MaximumNameLength = 40;

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
    public async Task<string?> LinkCode([Required] string token, CancellationToken cancellationToken) =>
        await this.Find(token, cancellationToken) is { } player ? this._links.Issue(player.Id) : null;

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
    public async Task<Player?> Unlink(
        [Required] string token,
        [Required] string provider,
        CancellationToken cancellationToken
    )
    {
        if (await this.Find(token, cancellationToken) is not { } player)
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
    public async Task<Player?> Rename([Required] string token, string? name, CancellationToken cancellationToken)
    {
        if (await this.Find(token, cancellationToken) is not { } player)
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
    public async Task<bool> Delete([Required] string token, CancellationToken cancellationToken)
    {
        if (await this.Find(token, cancellationToken) is not { } player)
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
