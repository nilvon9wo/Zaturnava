using System.Collections.Concurrent;
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
}
