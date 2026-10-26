using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class RandomBotTest
{
    private const int Seed = 7;
    private static readonly SeatId Seat = ScoringRules.Seat;
    private static readonly ScoringRules Rules = new([]);

    [Fact]
    public async Task Decide_WhenGivenLegalActions_ReturnsOneOfThem()
    {
        // Arrange
        ImmutableList<GameAction> legal = [new PassAction(Seat), new PassAction(new SeatId(1))];
        DecisionRequest request = new(Rules, ScoringRules.State(), Seat, legal);
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(action, legal);
    }

    [Fact]
    public async Task Decide_WhenItMayHandItsTurnToAnAlly_KeepsIt()
    {
        // Arrange
        DecisionRequest request =
            new(Rules, ScoringRules.State(), Seat, [new HandOverAction(Seat, new SeatId(2)), new PassAction(Seat)]);
        RandomBot bot = new(new Random(Seed));

        // Act
        GameAction action = await bot.Decide(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new PassAction(Seat), action);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        DecisionRequest request = new(Rules, ScoringRules.State(), Seat, [new PassAction(Seat)]);
        RandomBot bot = new(new Random(Seed));

        // Act
        Task<GameAction> deciding = bot.Decide(request, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => deciding);
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithm()
    {
        // Arrange
        RandomBot bot = new(new Random(Seed));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((RandomBot.BotId, "Random", false), (profile.Id, profile.Name, profile.UsesAi));
    }
}
