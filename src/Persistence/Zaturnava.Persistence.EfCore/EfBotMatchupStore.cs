using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Bots;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

/// <summary>How the server's bots have done against each other, in a relational database, one row a pairing.</summary>
public sealed class EfBotMatchupStore(GameDbContext context) : IBotMatchupPersisting
{
    private readonly GameDbContext _context = context;

    public async Task<ImmutableList<BotMatchup>> Matchups(CancellationToken cancellationToken)
    {
        List<BotMatchupEntity> entities = await this._context.BotMatchups.AsNoTracking().ToListAsync(cancellationToken);
        return
        [
            .. entities.Select(entity =>
                new BotMatchup(entity.Ruleset, entity.First, entity.Second)
                {
                    Won = entity.Won,
                    Lost = entity.Lost,
                    Drawn = entity.Drawn,
                }
            ),
        ];
    }

    public async Task Add([Required] BotMatchup games, CancellationToken cancellationToken)
    {
        BotMatchupEntity kept = await this._context.BotMatchups.FindAsync(
            [games.Ruleset, games.First, games.Second],
            cancellationToken
        ) ?? this._context.BotMatchups.Add(
            new BotMatchupEntity { Ruleset = games.Ruleset, First = games.First, Second = games.Second }
        ).Entity;
        kept.Won += games.Won;
        kept.Lost += games.Lost;
        kept.Drawn += games.Drawn;
        _ = await this._context.SaveChangesAsync(cancellationToken);
    }
}
