using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Server.Composition;

/// <summary>
/// Tells nobody a game changed: until a push channel (WebSockets) is plugged in here, clients see
/// changes by asking for the game again.
/// </summary>
internal sealed class SilentNotifier : IGameNotifying
{
    public Task GameChanged(GameId id, int version, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
}
