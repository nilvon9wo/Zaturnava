using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>
/// The players who have signed in, and their sessions: each a token's hash (never the token), standing for the
/// player until it is let go.
/// </summary>
public interface IPlayerPersisting
{
    /// <summary>The player whose login it is, if any.</summary>
    Task<Player?> ByLogin(Login login, CancellationToken cancellationToken);

    /// <summary>The player, if still known.</summary>
    Task<Player?> ById(PlayerId player, CancellationToken cancellationToken);

    /// <summary>The player whose session the token's hash is, if any.</summary>
    Task<Player?> BySession([Required] string tokenHash, CancellationToken cancellationToken);

    /// <summary>The name of each of <paramref name="players"/> still known; one forgotten is left out.</summary>
    Task<ImmutableDictionary<PlayerId, string>> Names(
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    );

    /// <summary>Keeps a new player, with their logins.</summary>
    Task Add([Required] Player player, CancellationToken cancellationToken);

    /// <summary>Names the player anew.</summary>
    Task Rename(PlayerId player, [Required] string name, CancellationToken cancellationToken);

    /// <summary>Shows <paramref name="links"/> on the player's profile, in place of any shown before.</summary>
    Task SetLinks(PlayerId player, [Required] ImmutableList<string> links, CancellationToken cancellationToken);

    /// <summary>Makes <paramref name="login"/>, which is no one's, the player's too.</summary>
    Task AddLogin(PlayerId player, Login login, CancellationToken cancellationToken);

    /// <summary>The player's <paramref name="login"/> is theirs no more. None to let go is no matter.</summary>
    Task RemoveLogin(PlayerId player, Login login, CancellationToken cancellationToken);

    /// <summary>Keeps a session for <paramref name="player"/>, known by its token's hash.</summary>
    Task AddSession(PlayerId player, [Required] string tokenHash, CancellationToken cancellationToken);

    /// <summary>Lets the session go: its token stands for no one any more. None to let go is no matter.</summary>
    Task RemoveSession([Required] string tokenHash, CancellationToken cancellationToken);

    /// <summary>Forgets the player altogether: their name, their logins and every session of theirs.</summary>
    Task Remove(PlayerId player, CancellationToken cancellationToken);
}
