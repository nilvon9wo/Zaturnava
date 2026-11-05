using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>
/// The chances a bot takes, whoever decides: here three face-offs for seat 0, scoring it 2, 5 and nothing, but the
/// first scoring seat 1, its enemy, 4 as well.
/// </summary>
public sealed class ChancesTest
{
    private const int Seed = 7;

    private static readonly SeatId Seat = ScoringRules.Seat;
    private static readonly SeatId Enemy = new(1);
    private static readonly FaceOffAction Even = new(Seat, new CellId(0), new CellId(9));
    private static readonly FaceOffAction Best = new(Seat, new CellId(1), new CellId(9));
    private static readonly FaceOffAction Nothing = new(Seat, new CellId(2), new CellId(9));

    private static readonly GameState State = ScoringRules.State() with
    {
        Teams = TeamGraph.FromTeams([[Seat], [Enemy]]),
    };

    private static readonly ScoringRules Rules = new(
        ImmutableDictionary<GameAction, int>.Empty.Add(Even, 2).Add(Best, 5).Add(Nothing, 0)
    )
    {
        Results = ImmutableDictionary<GameAction, GameState>.Empty
            .Add(Even, State with { Scores = State.Scores.SetItem(Enemy, 4) }),
    };

    public static TheoryData<string, IDeciding> Bots =>
        new()
        {
            { "Argus", new ArgusBot(new Random(Seed)) },
            { "Athena", new AthenaBot(new Random(Seed), []) },
            { "Midas", new MidasBot(new Random(Seed)) },
            { "Sage", new SageBot(new Random(Seed)) },
            { "Solomon", new SolomonBot(new Random(Seed)) },
        };

    [Theory]
    [MemberData(nameof(Bots), DisableDiscoveryEnumeration = true)]
    public async Task Seize_WhenChancesWouldLeaveItsTeamFurtherAhead_TakesTheOneThatLeavesItFurthest(
        string why,
        IDeciding bot
    )
    {
        // Arrange
        DecisionRequest request = new(Rules, State, Seat, [Even, Best, Nothing]);

        // Act
        GameAction? taken = await bot.Seize(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Best, taken);
        Assert.NotEmpty(why);
    }

    public static TheoryData<string, IDeciding> AllBots =>
        new()
        {
            { "Argus", new ArgusBot(new Random(Seed)) },
            { "Athena", new AthenaBot(new Random(Seed), []) },
            { "Midas", new MidasBot(new Random(Seed)) },
            { "Random", new RandomBot(new Random(Seed)) },
            { "Sage", new SageBot(new Random(Seed)) },
            { "Solomon", new SolomonBot(new Random(Seed)) },
        };

    [Theory]
    [MemberData(nameof(AllBots), DisableDiscoveryEnumeration = true)]
    public async Task Seize_WhenCancelled_Throws(string why, IDeciding bot)
    {
        // Arrange
        DecisionRequest request = new(Rules, State, Seat, [Best]);

        // Act
        Task<GameAction?> seizing = bot.Seize(request, new CancellationToken(canceled: true));

        // Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => seizing);
        Assert.NotEmpty(why);
    }

    [Fact]
    public async Task Seize_WhenNoChanceWouldLeaveItsTeamFurtherAhead_LetsThemGoBy()
    {
        // Arrange
        DecisionRequest request = new(Rules, State, Seat, [Even, Nothing]);

        // Act
        GameAction? taken = await new SageBot(new Random(Seed)).Seize(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(taken);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task Seize_WhenTheRandomBotIsOffered_TakesAChanceOrNoneAtRandom(int pick, bool isTaken)
    {
        // Arrange
        DecisionRequest request = new(Rules, State, Seat, [Best]);
        RandomBot bot = new(new Picking(pick));

        // Act
        GameAction? taken = await bot.Seize(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(isTaken, taken is not null);
    }

    /// <summary>A "random" source that always picks <paramref name="pick"/>.</summary>
    private sealed class Picking(int pick) : Random
    {
        public override int Next(int maxValue) => pick;
    }
}
