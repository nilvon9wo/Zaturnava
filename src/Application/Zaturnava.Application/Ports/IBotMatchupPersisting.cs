using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Bots;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>
/// How the server's bots have done against each other, pair by pair at each ruleset, in the games played to rate
/// them; each game kept as soon as it ends, so that whatever was gathered still counts however the run stops.
/// </summary>
public interface IBotMatchupPersisting
{
    /// <summary>Every pairing played, at every ruleset.</summary>
    Task<ImmutableList<BotMatchup>> Matchups(CancellationToken cancellationToken);

    /// <summary>Adds the games of <paramref name="games"/> to those its pairing has played already.</summary>
    Task Add([Required] BotMatchup games, CancellationToken cancellationToken);
}
