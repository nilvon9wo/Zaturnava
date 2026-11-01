using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

/// <summary>
/// Games in a relational database. A save adds only the actions that are new, and the game's
/// version is a concurrency token, so two requests racing on one game cannot both win.
/// </summary>
public sealed class EfGameStore(GameDbContext context) : IGamePersisting
{
    private readonly GameDbContext _context = context;

    public async Task<GameRecord?> Find(GameId id, CancellationToken cancellationToken)
    {
        GameEntity? entity = await this.Load(id, cancellationToken);
        return entity is null
            ? null
            : GameRecordMapping.ToRecord(entity);
    }

    public async Task<ImmutableList<GameRecord>> List(CancellationToken cancellationToken)
    {
        List<GameEntity> entities = await this.Games().OrderBy(game => game.Id).ToListAsync(cancellationToken);
        return [.. entities.Select(GameRecordMapping.ToRecord)];
    }

    public async Task<GamePage> Page(
        [Required] GameQuery query,
        [Required] IReadOnlySet<string> rulesets,
        CancellationToken cancellationToken
    )
    {
        IQueryable<GameEntity> matching = this._context.Games
            .AsNoTracking()
            .Where(game => rulesets.Contains(game.RulesetId));
        matching = query.HasOpenSeats is { } open
            ? matching.Where(game => game.Seats.Any(seat => seat.Kind == OpenSeat.KindName) == open)
            : matching;
        matching = query.PeopleOnly is { } people
            ? matching.Where(game => game.Seats.All(seat => seat.Kind != BotSeat.KindName) == people)
            : matching;
        matching = query.IsOver is { } over
            ? matching.Where(game => (game.IsOver ?? false) == over)
            : matching;
        int total = await matching.CountAsync(cancellationToken);
        List<ListedRow> rows = await matching
            .OrderByDescending(game => game.LastChanged)
            .ThenBy(game => game.Id)
            .Skip(query.Skipped)
            .Take(query.PageSize)
            .Select(game => new ListedRow(game, game.Seats.ToList(), game.Actions.Count))
            .ToListAsync(cancellationToken);
        return new GamePage(
            [.. rows.Select(row => GameRecordMapping.ToListing(row.Game, row.Seats, row.Moves))],
            total
        );
    }

    public async Task<ImmutableList<GameRecord>> Unsummarized(CancellationToken cancellationToken)
    {
        List<GameEntity> entities =
            await this.Games().Where(game => game.IsOver == null).ToListAsync(cancellationToken);
        return [.. entities.Select(GameRecordMapping.ToRecord)];
    }

    public async Task MarkOver(GameId id, bool isOver, CancellationToken cancellationToken) =>
        _ = await this._context.Games
            .Where(game => game.Id == id.Value)
            .ExecuteUpdateAsync(set => set.SetProperty(game => game.IsOver, isOver), cancellationToken);

    public async Task Add([Required] GameRecord record, CancellationToken cancellationToken)
    {
        _ = this._context.Games.Add(GameRecordMapping.ToEntity(record));
        await this.SaveChanges(record.Id, cancellationToken);
    }

    public async Task Replace([Required] GameRecord record, CancellationToken cancellationToken)
    {
        GameEntity? stored = await this.Load(record.Id, cancellationToken);
        bool isNextVersion = stored?.Version == record.Version - 1;
        GameEntity current = isNextVersion
            ? stored!
            : throw new ConcurrencyConflictException(record.Id);
        current.Version = record.Version;
        current.IsOver = record.IsOver;
        current.LastChanged = GameRecordMapping.ToUtc(record.LastChanged);
        foreach (SeatEntity seat in current.Seats)
        {
            GameRecordMapping.Update(seat, record.Controllers[seat.Seat]);
        }

        current.Actions.AddRange(GameRecordMapping.NewActions(record, current.Actions.Count));
        await this.SaveChanges(record.Id, cancellationToken);
    }

    /// <summary>A game's row as a lobby lists it: with its seats and how many actions, but not the actions.</summary>
    private sealed record ListedRow(GameEntity Game, List<SeatEntity> Seats, int Moves);

    private IQueryable<GameEntity> Games() =>
        this._context.Games
            .Include(game => game.Seats)
            .Include(game => game.Actions);

    private Task<GameEntity?> Load(GameId id, CancellationToken cancellationToken) =>
        this.Games().FirstOrDefaultAsync(game => game.Id == id.Value, cancellationToken);

    private async Task SaveChanges(GameId id, CancellationToken cancellationToken)
    {
        try
        {
            _ = await this._context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            this._context.ChangeTracker.Clear();
            throw new ConcurrencyConflictException(id, exception);
        }
    }
}
