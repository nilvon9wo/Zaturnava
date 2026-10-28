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
        foreach (SeatEntity seat in current.Seats)
        {
            GameRecordMapping.Update(seat, record.Controllers[seat.Seat]);
        }

        current.Actions.AddRange(GameRecordMapping.NewActions(record, current.Actions.Count));
        await this.SaveChanges(record.Id, cancellationToken);
    }

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
