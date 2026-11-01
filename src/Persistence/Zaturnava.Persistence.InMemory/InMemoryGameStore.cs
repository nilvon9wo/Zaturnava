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

    public Task<GamePage> Page(
        [Required] GameQuery query,
        [Required] IReadOnlySet<string> rulesets,
        CancellationToken cancellationToken
    )
    {
        ImmutableList<GameRecord> matching =
        [
            .. this._games.Values
                .Where(game => rulesets.Contains(game.RulesetId))
                .Where(game => query.HasOpenSeats is not { } open || game.IsWaiting == open)
                .Where(game => query.PeopleOnly is not { } people || !game.Controllers.Any(IsBot) == people)
                .Where(game => query.IsOver is not { } over || (game.IsOver ?? false) == over)
                .OrderByDescending(game => game.LastChanged)
                .ThenBy(game => game.Id.Value, StringComparer.Ordinal),
        ];
        GamePage page = new([.. matching.Skip(query.Skipped).Take(query.PageSize).Select(ListingOf)], matching.Count);
        return Task.FromResult(page);
    }

    public Task<ImmutableList<GameRecord>> Unsummarized(CancellationToken cancellationToken)
    {
        ImmutableList<GameRecord> games = [.. this._games.Values.Where(game => game.IsOver is null)];
        return Task.FromResult(games);
    }

    public Task MarkOver(GameId id, bool isOver, CancellationToken cancellationToken)
    {
        this._games[id] = this._games[id] with { IsOver = isOver };
        return Task.CompletedTask;
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

    private static bool IsBot(SeatController controller) => controller is BotSeat;

    private static GameListing ListingOf(GameRecord game) =>
        new(game.Id, game.RulesetId, game.Version, game.Controllers, game.Actions.Count, game.IsOver ?? false);
}
