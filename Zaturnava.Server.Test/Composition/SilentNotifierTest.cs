using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Server.Composition;

namespace Net.NowhereAtAll.Zaturnava.Server.Test.Composition;

public sealed class SilentNotifierTest
{
    [Fact]
    public async Task GameChanged_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        SilentNotifier notifier = new();

        // Act
        Task notifying = notifier.GameChanged(new GameId("g"), 1, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => notifying);
    }
}
