using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Application.Players;

/// <summary>
/// Signing in, which is optional: a provider vouches for a login, and the player whose login it is (made the first
/// time, named as the provider names them) gets a token, as a seat's is made (<see cref="SeatTokens"/>), to show
/// from then on. Only its hash is kept.
/// </summary>
public sealed class PlayerService(IPlayerPersisting store)
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

    /// <summary>
    /// Signs in the player whose login it is: the one already known, else a new one, named
    /// <paramref name="name"/>; either way with a new token.
    /// </summary>
    public async Task<SignedIn> SignIn(Login login, string? name, CancellationToken cancellationToken)
    {
        Player player = await this._store.ByLogin(login, cancellationToken)
            ?? await this.Add(new Player(PlayerId.New(), NameOf(name), [login]), cancellationToken);
        string token = SeatTokens.Issue();
        await this._store.AddSession(player.Id, SeatTokens.Hash(token), cancellationToken);
        return new SignedIn(player, token);
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

    /// <summary>The name, without the spaces around it, cut to the longest allowed; or <see cref="Unnamed"/>.</summary>
    private static string NameOf(string? name) =>
        name?.Trim() is { Length: > 0 } trimmed
            ? trimmed[..Math.Min(trimmed.Length, MaximumNameLength)].TrimEnd()
            : Unnamed;

    private async Task<Player> Add(Player player, CancellationToken cancellationToken)
    {
        await this._store.Add(player, cancellationToken);
        return player;
    }
}
