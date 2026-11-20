using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class NoviceBotTest
{
    private const int Tries = 400;

    /// <summary>Enough turns that shares a few in a hundred apart show apart.</summary>
    private const int FaintTries = 20000;
    private static readonly SeatId Seat = ScoringRules.Seat;
    private static readonly GameAction Small = Move(1);
    private static readonly GameAction Big = Move(2);
    private static readonly GameAction Other = Move(3);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_OverManyTurns_TakesTheBestMoreOftenTheGreaterItsShareOfMidas()
    {
        // Arrange - three moves, one scoring the most: Random takes it a third of the time, Midas always
        DecisionRequest request = Request();

        // Act
        (int echo, int icarus) = (
            await this.BestTaken(NoviceBot.Echo(new Random(1)), request),
            await this.BestTaken(NoviceBot.Icarus(new Random(1)), request)
        );

        // Assert - about half (a quarter, and a third of the rest), and about two thirds; never always
        Assert.True(
            echo is > Tries * 2 / 5 and < Tries * 3 / 5 && icarus is > Tries * 3 / 5 and < Tries * 3 / 4,
            $"Echo took the best {echo} times, Icarus {icarus}, of {Tries}"
        );
    }

    [Fact]
    public async Task Decide_OverManyTurns_TheFaintTakeTheBestLittleMoreThanRandomAndEachMoreThanTheLast()
    {
        // Arrange - Random takes the best a third of the time; Morpheus, Phaethon and Tantalus a little more each
        DecisionRequest request = Request();

        // Act
        ImmutableList<int> taken =
        [
            .. await Task.WhenAll(
                NoviceBot.Faint(new Random(1)).Select(bot => this.BestTaken(bot, request, FaintTries))
            ),
        ];

        // Assert
        Assert.True(
            taken[0] > FaintTries * 3 / 10 && taken[0] < taken[2] && taken[1] < taken[2] && taken[2] < FaintTries / 2,
            $"Taken: {string.Join(",", taken)} of {FaintTries}"
        );
    }

    [Fact]
    public void Faint_WhenAsked_IsThreePlainAlgorithmsForTheFirstGames()
    {
        // Arrange - the bots for the very first games

        // Act
        string named = string.Join(
            ",",
            NoviceBot.Faint(new Random(1)).Select(bot => $"{bot.Profile.Id}:{bot.Profile.UsesAi}")
        );

        // Assert
        Assert.Equal("morpheus:False,phaethon:False,tantalus:False", named);
    }

    [Fact]
    public async Task Seize_WhenNothingIsOffered_SeizesNothing()
    {
        // Arrange
        DecisionRequest request = Request() with { LegalActions = [] };
        NoviceBot bot = NoviceBot.Icarus(new Random(1));

        // Act
        GameAction?[] seized = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => bot.Seize(request, this._cancellation))
        );

        // Assert
        Assert.Equal(10, seized.Count(action => action is null));
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithmForBeginners()
    {
        // Arrange
        ImmutableList<BotProfile> profiles =
            [NoviceBot.Echo(new Random(1)).Profile, NoviceBot.Icarus(new Random(1)).Profile];

        // Act
        string named = string.Join(",", profiles.Select(profile => $"{profile.Id}:{profile.Name}:{profile.UsesAi}"));

        // Assert
        Assert.Equal("echo:Echo:False,icarus:Icarus:False", named);
    }

    private async Task<int> BestTaken(NoviceBot bot, DecisionRequest request, int tries = Tries)
    {
        int taken = 0;
        for (int turn = 0; turn < tries; turn++)
        {
            taken += await bot.Decide(request, this._cancellation) == Big ? 1 : 0;
        }

        return taken;
    }

    private static MoveAction Move(int to) => new(Seat, new CellId(0), new CellId(to));

    private static DecisionRequest Request()
    {
        ScoringRules rules = new(
            ImmutableDictionary<GameAction, int>.Empty.Add(Small, 1).Add(Big, 5).Add(Other, 0)
        );
        return new DecisionRequest(rules, ScoringRules.State(), Seat, rules.LegalActions(ScoringRules.State()));
    }
}
