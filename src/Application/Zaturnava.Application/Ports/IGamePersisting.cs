using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>Where games are kept. In memory, a relational database, or anything else.</summary>
public interface IGamePersisting
{
    Task<GameRecord?> Find(GameId id, CancellationToken cancellationToken);

    Task<ImmutableList<GameRecord>> List(CancellationToken cancellationToken);

    /// <summary>
    /// The page of games <paramref name="query"/> asks for, of the <paramref name="rulesets"/> given, newest change
    /// first (games never changed since this was kept last), read as stored: no game is replayed.
    /// </summary>
    Task<GamePage> Page(
        [Required] GameQuery query,
        [Required] IReadOnlySet<string> rulesets,
        CancellationToken cancellationToken
    );

    /// <summary>The games saved before whether each is over was kept.</summary>
    Task<ImmutableList<GameRecord>> Unsummarized(CancellationToken cancellationToken);

    /// <summary>Keeps whether the game is over, as worked out, leaving its version as it is.</summary>
    Task MarkOver(GameId id, bool isOver, CancellationToken cancellationToken);

    Task Add([Required] GameRecord record, CancellationToken cancellationToken);

    /// <summary>Stores <paramref name="record"/> over the one whose version is one less.</summary>
    /// <exception cref="ConcurrencyConflictException">The stored game has changed since it was read.</exception>
    Task Replace([Required] GameRecord record, CancellationToken cancellationToken);
}
