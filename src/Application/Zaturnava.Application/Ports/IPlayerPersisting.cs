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

    /// <summary>The player whose session the token's hash is, if any.</summary>
    Task<Player?> BySession([Required] string tokenHash, CancellationToken cancellationToken);

    /// <summary>Keeps a new player, with their logins.</summary>
    Task Add([Required] Player player, CancellationToken cancellationToken);

    /// <summary>Keeps a session for <paramref name="player"/>, known by its token's hash.</summary>
    Task AddSession(PlayerId player, [Required] string tokenHash, CancellationToken cancellationToken);
}
