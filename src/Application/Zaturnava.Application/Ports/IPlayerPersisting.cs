using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>
/// The players who have signed in, and their sessions: each a token's hash (never the token), standing for the
/// player until it is let go; and where they, and the server's bots, stand at each game.
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

    /// <summary>
    /// Whether a player other than <paramref name="except"/> is named <paramref name="name"/>, capitals or not.
    /// </summary>
    Task<bool> IsNameTaken([Required] string name, PlayerId? except, CancellationToken cancellationToken);

    /// <summary>
    /// When each of <paramref name="players"/> last signed in; one forgotten, or not signed in since this was kept,
    /// is left out.
    /// </summary>
    Task<ImmutableDictionary<PlayerId, DateTimeOffset>> LastSignedIn(
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    );

    /// <summary>Keeps that the player signed in at <paramref name="time"/>.</summary>
    Task MarkSignedIn(PlayerId player, DateTimeOffset time, CancellationToken cancellationToken);

    /// <summary>Keeps a new player, with their logins.</summary>
    Task Add([Required] Player player, CancellationToken cancellationToken);

    /// <summary>Names the player anew.</summary>
    Task Rename(PlayerId player, [Required] string name, CancellationToken cancellationToken);

    /// <summary>Keeps the page's settings the player prefers (<see cref="Player.Preferences"/>), as given.</summary>
    Task KeepPreferences(
        PlayerId player,
        [Required] ImmutableSortedDictionary<string, string> preferences,
        CancellationToken cancellationToken
    );

    /// <summary>Makes <paramref name="login"/>, which is no one's, the player's too.</summary>
    Task AddLogin(PlayerId player, Login login, CancellationToken cancellationToken);

    /// <summary>The player's <paramref name="login"/> is theirs no more. None to let go is no matter.</summary>
    Task RemoveLogin(PlayerId player, Login login, CancellationToken cancellationToken);

    /// <summary>Keeps a session for <paramref name="player"/>, known by its token's hash.</summary>
    Task AddSession(PlayerId player, [Required] string tokenHash, CancellationToken cancellationToken);

    /// <summary>Lets the session go: its token stands for no one any more. None to let go is no matter.</summary>
    Task RemoveSession([Required] string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Makes <paramref name="from"/>'s logins, sessions and standings <paramref name="into"/>'s (a standing at a game
    /// both have one at, <see cref="Standing.Merged"/>), then forgets <paramref name="from"/>. Both are still known.
    /// </summary>
    Task Merge(PlayerId from, PlayerId into, CancellationToken cancellationToken);

    /// <summary>
    /// Forgets the player altogether: their name, their logins, every session of theirs and where they stand.
    /// </summary>
    Task Remove(PlayerId player, CancellationToken cancellationToken);

    /// <summary>
    /// Where each of <paramref name="players"/> stands at <paramref name="game"/>; one who has never finished a
    /// game of it, or is forgotten, is left out.
    /// </summary>
    Task<ImmutableDictionary<PlayerId, Standing>> Standings(
        [Required] string game,
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    );

    /// <summary>Keeps where each player stands, in place of where they stood.</summary>
    Task Keep([Required] ImmutableList<Standing> standings, CancellationToken cancellationToken);

    /// <summary>
    /// The <paramref name="count"/> players standing highest at <paramref name="game"/>: by Elo, then experience,
    /// then by id, so that ties keep an order.
    /// </summary>
    Task<ImmutableList<Standing>> Leaders([Required] string game, int count, CancellationToken cancellationToken);

    /// <summary>
    /// The <paramref name="count"/> players who have done best at <paramref name="game"/> against bots alone, of those
    /// who have finished any such game: by games won so, then fewest lost, then by id.
    /// </summary>
    Task<ImmutableList<Standing>> LeadersAgainstBots(
        [Required] string game,
        int count,
        CancellationToken cancellationToken
    );

    /// <summary>Where each of <paramref name="bots"/> stands at <paramref name="game"/>, if it stands there.</summary>
    Task<ImmutableDictionary<string, BotStanding>> BotStandings(
        [Required] string game,
        [Required] ImmutableList<string> bots,
        CancellationToken cancellationToken
    );

    /// <summary>Keeps where each bot stands, in place of where it stood.</summary>
    Task KeepBots([Required] ImmutableList<BotStanding> standings, CancellationToken cancellationToken);

    /// <summary>
    /// The <paramref name="count"/> bots standing highest at <paramref name="game"/>: by games won, then fewest lost,
    /// then by id.
    /// </summary>
    Task<ImmutableList<BotStanding>> BotLeaders(
        [Required] string game,
        int count,
        CancellationToken cancellationToken
    );
}
