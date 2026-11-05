using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Decisions;

public sealed class DecidingTest
{
    private static readonly SeatId Seat = new(0);

    [Fact]
    public async Task Seize_WhenTheDeciderSaysNoOther_LetsEveryChanceGoBy()
    {
        // Arrange
        GameState state = new(Position.Empty, [], TeamGraph.FromTeams([]), Seat, 1, [], []);
        DecisionRequest request = new(null!, state, Seat, [new PassAction(Seat)]);

        // Act
        GameAction? taken = await SeizeOf(new Passer(), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(taken);
    }

    [Fact]
    public async Task Seize_WhenCancelled_Throws()
    {
        // Arrange
        GameState state = new(Position.Empty, [], TeamGraph.FromTeams([]), Seat, 1, [], []);
        DecisionRequest request = new(null!, state, Seat, [new PassAction(Seat)]);

        // Act
        Task<GameAction?> seizing = SeizeOf(new Passer(), request, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => seizing);
    }

    private static Task<GameAction?> SeizeOf<TDeciding>(
        TDeciding decider,
        DecisionRequest request,
        CancellationToken cancellationToken
    )
        where TDeciding : IDeciding =>
        decider.Seize(request, cancellationToken);
}

/// <summary>A decider that always passes, and says nothing of chances.</summary>
file sealed class Passer : IDeciding
{
    public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<GameAction>(new PassAction(request.Seat));
}
