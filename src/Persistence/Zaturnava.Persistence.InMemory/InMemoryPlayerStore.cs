using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Persistence.InMemory;

public sealed class InMemoryPlayerStore : IPlayerPersisting
{
    private readonly ConcurrentDictionary<PlayerId, Player> _players = new();
    private readonly ConcurrentDictionary<string, PlayerId> _sessions = new(StringComparer.Ordinal);

    public Task<Player?> ByLogin(Login login, CancellationToken cancellationToken) =>
        Task.FromResult(this._players.Values.FirstOrDefault(player => player.Logins.Contains(login)));

    public Task<Player?> ById(PlayerId player, CancellationToken cancellationToken) =>
        Task.FromResult(this._players.GetValueOrDefault(player));

    public Task<Player?> BySession([Required] string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(
            this._sessions.TryGetValue(tokenHash, out PlayerId player)
                ? this._players.GetValueOrDefault(player)
                : null
        );

    public Task<ImmutableDictionary<PlayerId, string>> Names(
        [Required] ImmutableList<PlayerId> players,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            players
                .Distinct()
                .Select(this._players.GetValueOrDefault)
                .OfType<Player>()
                .ToImmutableDictionary(player => player.Id, player => player.Name)
        );

    public Task Add([Required] Player player, CancellationToken cancellationToken)
    {
        this._players[player.Id] = player;
        return Task.CompletedTask;
    }

    public Task Rename(PlayerId player, [Required] string name, CancellationToken cancellationToken) =>
        this.Change(player, known => known with { Name = name });

    public Task SetLinks(
        PlayerId player,
        [Required] ImmutableList<string> links,
        CancellationToken cancellationToken
    ) =>
        this.Change(player, known => known with { Links = links });

    public Task AddLogin(PlayerId player, Login login, CancellationToken cancellationToken) =>
        this.Change(player, known => known with { Logins = known.Logins.Add(login) });

    public Task RemoveLogin(PlayerId player, Login login, CancellationToken cancellationToken) =>
        this.Change(player, known => known with { Logins = known.Logins.Remove(login) });

    public Task AddSession(PlayerId player, [Required] string tokenHash, CancellationToken cancellationToken)
    {
        this._sessions[tokenHash] = player;
        return Task.CompletedTask;
    }

    public Task RemoveSession([Required] string tokenHash, CancellationToken cancellationToken)
    {
        _ = this._sessions.TryRemove(tokenHash, out _);
        return Task.CompletedTask;
    }

    public Task Remove(PlayerId player, CancellationToken cancellationToken)
    {
        _ = this._players.TryRemove(player, out _);
        ImmutableList<string> sessions =
            [.. this._sessions.Where(session => session.Value == player).Select(session => session.Key)];
        sessions.ForEach(session => _ = this._sessions.TryRemove(session, out _));
        return Task.CompletedTask;
    }

    /// <summary>Changes the player, if still known.</summary>
    private Task Change(PlayerId player, Func<Player, Player> change)
    {
        if (this._players.TryGetValue(player, out Player? known))
        {
            this._players[player] = change(known);
        }

        return Task.CompletedTask;
    }
}
