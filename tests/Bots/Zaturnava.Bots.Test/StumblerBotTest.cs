using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class StumblerBotTest
{
    private const int Tries = 60;

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_OverManyTurns_PlaysOnlyTheBetterPartOfItsMovesAsItsShareSays()
    {
        // Arrange - a Pawn beside an enemy Horse no one guards: taking it, stepping away, or passing
        DecisionRequest request = Request();

        // Act - Epimetheus keeps the best one of three; Narcissus the best two
        ImmutableHashSet<GameAction> epimetheus = await this.Played(StumblerBot.Epimetheus(new Random(1)), request);
        ImmutableHashSet<GameAction> narcissus = await this.Played(StumblerBot.Narcissus(new Random(1)), request);

        // Assert
        Assert.Equal(
            (1, true, 2, true),
            (
                epimetheus.Count,
                epimetheus.Contains(LineRules.Move(3, 4)),
                narcissus.Count,
                narcissus.Contains(LineRules.Move(3, 4))
            )
        );
    }

    [Fact]
    public async Task Decide_WhenCancelled_IsCancelled()
    {
        // Arrange
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        // Act
        Task<GameAction> decided = StumblerBot.Narcissus(new Random(1)).Decide(Request(), cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => decided);
    }

    [Fact]
    public async Task Seize_WhenNothingIsOffered_SeizesNothing()
    {
        // Arrange
        DecisionRequest request = Request() with { LegalActions = [] };

        // Act
        GameAction? seized = await StumblerBot.Epimetheus(new Random(1)).Seize(request, this._cancellation);

        // Assert
        Assert.Null(seized);
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithmForBeginners()
    {
        // Arrange
        ImmutableList<BotProfile> profiles =
        [
            StumblerBot.Epimetheus(new Random(1)).Profile,
            StumblerBot.Narcissus(new Random(1)).Profile,
        ];

        // Act
        string named = string.Join(",", profiles.Select(profile => $"{profile.Id}:{profile.Name}:{profile.UsesAi}"));

        // Assert
        Assert.Equal("epimetheus:Epimetheus:False,narcissus:Narcissus:False", named);
    }

    private async Task<ImmutableHashSet<GameAction>> Played(StumblerBot bot, DecisionRequest request)
    {
        ImmutableHashSet<GameAction> played = [];
        for (int turn = 0; turn < Tries; turn++)
        {
            played = played.Add(await bot.Decide(request, this._cancellation));
        }

        return played;
    }

    private static DecisionRequest Request() =>
        LineRules.Request(
            [LineRules.Move(3, 4), LineRules.Move(3, 2), new PassAction(LineRules.Seat)],
            (3, LineRules.Pawn, LineRules.Seat),
            (4, LineRules.Horse, LineRules.Enemy)
        );
}
