using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>Where games are kept. In memory, a relational database, or anything else.</summary>
public interface IGamePersisting
{
    Task<GameRecord?> Find(GameId id, CancellationToken cancellationToken);

    Task<ImmutableList<GameRecord>> List(CancellationToken cancellationToken);

    Task Add([Required] GameRecord record, CancellationToken cancellationToken);

    /// <summary>Stores <paramref name="record"/> over the one whose version is one less.</summary>
    /// <exception cref="ConcurrencyConflictException">The stored game has changed since it was read.</exception>
    Task Replace([Required] GameRecord record, CancellationToken cancellationToken);
}
