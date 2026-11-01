using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;
using Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Entities;

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore;

/// <summary>The players who have signed in, their logins and their sessions, in a relational database.</summary>
public sealed class EfPlayerStore(GameDbContext context) : IPlayerPersisting
{
    private readonly GameDbContext _context = context;

    public async Task<Player?> ByLogin(Login login, CancellationToken cancellationToken)
    {
        PlayerEntity? player = await this._context.Players
            .Include(entity => entity.Logins)
            .FirstOrDefaultAsync(
                entity => entity.Logins.Any(held => held.Provider == login.Provider && held.Key == login.Key),
                cancellationToken
            );
        return ToPlayer(player);
    }

    public async Task<Player?> ById(PlayerId player, CancellationToken cancellationToken) =>
        ToPlayer(
            await this._context.Players
                .Include(entity => entity.Logins)
                .FirstOrDefaultAsync(entity => entity.Id == player.Value, cancellationToken)
        );

    public async Task<Player?> BySession([Required] string tokenHash, CancellationToken cancellationToken)
    {
        PlayerEntity? player = await this._context.Players
            .Include(entity => entity.Logins)
            .FirstOrDefaultAsync(
                entity => this._context.Sessions.Any(
                    session => session.TokenHash == tokenHash && session.PlayerId == entity.Id
                ),
                cancellationToken
            );
        return ToPlayer(player);
    }

    public async Task<ImmutableDictionary<PlayerId, string>> Names(
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    )
    {
        List<string> ids = [.. players.Select(player => player.Value).Distinct(StringComparer.Ordinal)];
        List<PlayerEntity> known = await this._context.Players
            .Where(entity => ids.Contains(entity.Id))
            .ToListAsync(cancellationToken);
        return known.ToImmutableDictionary(entity => new PlayerId(entity.Id), entity => entity.Name);
    }

    public async Task Add([Required] Player player, CancellationToken cancellationToken)
    {
        _ = this._context.Players.Add(
            new PlayerEntity
            {
                Id = player.Id.Value,
                Name = player.Name,
                Logins =
                [
                    .. player.Logins.Select(login =>
                        new LoginEntity { Provider = login.Provider, Key = login.Key, PlayerId = player.Id.Value }
                    ),
                ],
            }
        );
        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task Rename(PlayerId player, [Required] string name, CancellationToken cancellationToken) =>
        _ = await this._context.Players
            .Where(entity => entity.Id == player.Value)
            .ExecuteUpdateAsync(setters => setters.SetProperty(entity => entity.Name, name), cancellationToken);

    public async Task AddLogin(PlayerId player, Login login, CancellationToken cancellationToken)
    {
        _ = this._context.Set<LoginEntity>()
            .Add(new LoginEntity { Provider = login.Provider, Key = login.Key, PlayerId = player.Value });
        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveLogin(PlayerId player, Login login, CancellationToken cancellationToken) =>
        _ = await this._context.Set<LoginEntity>()
            .Where(held => held.PlayerId == player.Value && held.Provider == login.Provider && held.Key == login.Key)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task AddSession(PlayerId player, [Required] string tokenHash, CancellationToken cancellationToken)
    {
        _ = this._context.Sessions.Add(new SessionEntity { TokenHash = tokenHash, PlayerId = player.Value });
        _ = await this._context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSession([Required] string tokenHash, CancellationToken cancellationToken) =>
        _ = await this._context.Sessions
            .Where(session => session.TokenHash == tokenHash)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task Remove(PlayerId player, CancellationToken cancellationToken)
    {
        _ = await this._context.Sessions
            .Where(session => session.PlayerId == player.Value)
            .ExecuteDeleteAsync(cancellationToken);
        _ = await this._context.Set<LoginEntity>()
            .Where(login => login.PlayerId == player.Value)
            .ExecuteDeleteAsync(cancellationToken);
        _ = await this._context.Players
            .Where(entity => entity.Id == player.Value)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static Player? ToPlayer(PlayerEntity? entity) =>
        entity is null
            ? null
            : new Player(
                new PlayerId(entity.Id),
                entity.Name,
                [.. entity.Logins.Select(login => new Login(login.Provider, login.Key))]
            );
}
