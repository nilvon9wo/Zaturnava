using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

public sealed class InMemoryGameStore : IGamePersisting
{
    private readonly ConcurrentDictionary<GameId, GameRecord> _games = new();

    public Task<GameRecord?> Find(GameId id, CancellationToken cancellationToken) =>
        Task.FromResult(this._games.GetValueOrDefault(id));

    public Task<ImmutableList<GameRecord>> List(CancellationToken cancellationToken)
    {
        IEnumerable<GameRecord> ordered = this._games.Values.OrderBy(game => game.Id.Value, StringComparer.Ordinal);
        ImmutableList<GameRecord> games = [.. ordered];
        return Task.FromResult(games);
    }

    public Task Add([Required] GameRecord record, CancellationToken cancellationToken) =>
        this._games.TryAdd(record.Id, record)
            ? Task.CompletedTask
            : Task.FromException(new ConcurrencyConflictException(record.Id));

    public Task Replace([Required] GameRecord record, CancellationToken cancellationToken)
    {
        GameRecord? stored = this._games.GetValueOrDefault(record.Id);
        bool isNextVersion = stored?.Version == record.Version - 1;
        bool isReplaced = isNextVersion && this._games.TryUpdate(record.Id, record, stored!);
        return isReplaced
            ? Task.CompletedTask
            : Task.FromException(new ConcurrencyConflictException(record.Id));
    }
}
