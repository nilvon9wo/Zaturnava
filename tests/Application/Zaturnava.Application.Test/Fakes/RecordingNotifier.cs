using System.Collections.Concurrent;
using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Ports;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

internal sealed class RecordingNotifier : IGameNotifying
{
    public ConcurrentQueue<(GameId Id, int Version)> Changes { get; } = new();

    public Task GameChanged(GameId id, int version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.Changes.Enqueue((id, version));
        return Task.CompletedTask;
    }
}
