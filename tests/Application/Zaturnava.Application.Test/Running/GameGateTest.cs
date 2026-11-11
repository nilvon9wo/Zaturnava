using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Running;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class GameGateTest
{
    private static readonly GameId Game = new("g");
    private static readonly GameId Other = new("o");

    private readonly GameGate _gate = new();

    [Fact]
    public async Task Enter_WhenNoOneElseIsChangingTheGame_LetsTheRequestThroughAtOnce()
    {
        // Arrange - nothing beyond the gate

        // Act
        Task<IDisposable> entering = this._gate.Enter(Game);

        // Assert
        Assert.True(entering.IsCompleted);
        (await entering).Dispose();
    }

    [Fact]
    public async Task Enter_WhenAnotherIsChangingTheGame_WaitsUntilItIsDone()
    {
        // Arrange
        IDisposable first = await this._gate.Enter(Game);

        // Act
        Task<IDisposable> entering = this._gate.Enter(Game);
        bool waited = !entering.IsCompleted;
        first.Dispose();
        using IDisposable second = await entering;

        // Assert
        Assert.True(waited);
    }

    [Fact]
    public async Task Enter_WhenAnotherIsChangingAnotherGame_LetsTheRequestThroughAtOnce()
    {
        // Arrange
        using IDisposable other = await this._gate.Enter(Other);

        // Act
        Task<IDisposable> entering = this._gate.Enter(Game);

        // Assert
        Assert.True(entering.IsCompleted);
        (await entering).Dispose();
    }

    [Fact]
    public async Task Busy_WhenOneIsDoneWhileAnotherStillWaits_KeepsTheGameUntilTheLastIsDone()
    {
        // Arrange
        IDisposable first = await this._gate.Enter(Game);
        Task<IDisposable> entering = this._gate.Enter(Game);
        first.Dispose();
        int whileWaiting = this._gate.Busy;
        IDisposable second = await entering;

        // Act
        second.Dispose();

        // Assert
        Assert.Equal((1, 0), (whileWaiting, this._gate.Busy));
    }
}
