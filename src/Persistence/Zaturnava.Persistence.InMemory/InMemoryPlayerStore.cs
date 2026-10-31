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
}
