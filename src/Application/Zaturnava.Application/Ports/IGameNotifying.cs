using Net.NowhereAtAll.Zaturnava.Application.Games;

namespace Net.NowhereAtAll.Zaturnava.Application.Ports;

/// <summary>
/// Tells whoever is watching that a game changed: nobody (clients poll), a WebSocket hub, push
/// notifications, e-mail - whatever the host plugs in.
/// </summary>
public interface IGameNotifying
{
    Task GameChanged(GameId id, int version, CancellationToken cancellationToken);
}
