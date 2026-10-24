using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class RandomBotTest
{
    private const int Seed = 7;
    private static readonly SeatId Seat = new(0);

    [Fact]
    public async Task Decide_WhenGivenLegalActions_ReturnsOneOfThem()
    {
        // Arrange
        ImmutableList<GameAction> legal = [new PassAction(Seat), new PassAction(new SeatId(1))];
        DecisionRequest request = new(EmptyState(), Seat, legal);
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(action, legal);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        DecisionRequest request = new(EmptyState(), Seat, [new PassAction(Seat)]);
        RandomBot bot = new(new Random(Seed));

        // Act
        Task<GameAction> deciding = bot.Decide(request, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => deciding);
    }

    private static GameState EmptyState() =>
        new(Position.Empty, [], TeamGraph.FromTeams([]), Seat, 1, [], []);
}
