using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class TacticianBotTest
{
    private const int Seed = 7;
    private static readonly SageWeights Nothing = new(0, 0, 0, 0, 0, 0, 0);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenTheEnemyCouldAnswerTheMoveThatLooksBest_PlaysTheOneThatHoldsUpToItsReply()
    {
        // Arrange - moving to 2 looks better at once, but the enemy's best reply leaves Tactician 1 at most; moving
        // to 1, its best reply leaves 4
        TreeRules rules = new(
            ImmutableDictionary<int, TreeNode>.Empty
                .Add(0, new TreeNode(TreeRules.Seat, 0, [1, 2]))
                .Add(1, new TreeNode(TreeRules.Enemy, 0, [3, 4]))
                .Add(2, new TreeNode(TreeRules.Enemy, 5, [5, 6]))
                .Add(3, new TreeNode(TreeRules.Seat, 0, [7, 8]))
                .Add(4, new TreeNode(TreeRules.Seat, 0, [9, 10]))
                .Add(5, new TreeNode(TreeRules.Seat, 0, [11, 12]))
                .Add(6, new TreeNode(TreeRules.Seat, 0, [13, 14]))
                .Add(7, TreeNode.Over(4))
                .Add(8, TreeNode.Over(2))
                .Add(9, TreeNode.Over(3))
                .Add(10, TreeNode.Over(9))
                .Add(11, TreeNode.Over(1))
                .Add(12, TreeNode.Over(0))
                .Add(13, TreeNode.Over(6))
                .Add(14, TreeNode.Over(7))
        );

        // Act
        GameAction action = await Tactician(Nothing with { Score = 1 }).Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(TreeRules.Move(0, 1), action);
    }

    [Fact]
    public async Task Decide_WhenEachMovesFirstReplyShowsItWorseThanAnother_StillFindsTheBest()
    {
        // Arrange - the enemy's best reply to moving to 1 leaves Tactician 4, and to moving to 2, 1 at most; every
        // line ends where no one can do anything more
        TreeRules rules = new(
            ImmutableDictionary<int, TreeNode>.Empty
                .Add(0, new TreeNode(TreeRules.Seat, 0, [1, 2]))
                .Add(1, new TreeNode(TreeRules.Enemy, 5, [3, 4]))
                .Add(2, new TreeNode(TreeRules.Enemy, 0, [5, 6]))
                .Add(3, new TreeNode(TreeRules.Seat, 0, [7, 8]))
                .Add(4, new TreeNode(TreeRules.Seat, 0, [9, 10]))
                .Add(5, new TreeNode(TreeRules.Seat, 0, [11, 12]))
                .Add(6, new TreeNode(TreeRules.Seat, 0, [13, 14]))
                .Add(7, Stuck(4))
                .Add(8, Stuck(2))
                .Add(9, Stuck(3))
                .Add(10, Stuck(9))
                .Add(11, Stuck(1))
                .Add(12, Stuck(0))
                .Add(13, Stuck(6))
                .Add(14, Stuck(7))
        );

        // Act
        GameAction action = await Tactician(Nothing with { Score = 1 }).Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(TreeRules.Move(0, 1), action);
    }

    [Fact]
    public async Task Decide_WhenItForksAHorseAndABoatThatWouldTakeBack_TakesTheBoat()
    {
        // Arrange - Tactician's Boat between an enemy Horse and an enemy Boat, nothing guarding it: taking the Horse,
        // worth less, would leave the enemy's Boat to take Tactician's back along the line; taking the Boat leaves
        // the Horse nothing to take
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(3, 2), LineRules.Move(3, 4), new PassAction(LineRules.Seat)],
            (3, LineRules.Boat, LineRules.Seat),
            (2, LineRules.Horse, LineRules.Enemy),
            (4, LineRules.Boat, LineRules.Enemy)
        );

        // Act
        GameAction action = await new TacticianBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(LineRules.Move(3, 4), action);
    }

    [Fact]
    public async Task Decide_WhenEveryMoveWouldLeaveAPieceToBeTaken_Passes()
    {
        // Arrange - stepping to 4 would leave Tactician's Horse beside an enemy Pawn
        PassAction pass = new(LineRules.Seat);
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(2, 4), pass],
            (2, LineRules.Horse, LineRules.Seat),
            (5, LineRules.Pawn, LineRules.Enemy)
        );

        // Act
        GameAction action = await new TacticianBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(pass, action);
    }

    [Fact]
    public async Task Decide_WhenItCanOnlyPass_Passes()
    {
        // Arrange
        PassAction pass = new(LineRules.Seat);
        DecisionRequest request = LineRules.Request([pass], (3, LineRules.Pawn, LineRules.Seat));

        // Act
        GameAction action = await new TacticianBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(pass, action);
    }

    [Fact]
    public async Task Decide_WhenAnAllyWouldRatherScoreForItself_ExpectsItToRatherThanPlayForTactician()
    {
        // Arrange - handing the ally the move could score Tactician 10, but the ally would rather score 5 itself;
        // Tactician counts only its own points, but for a game over
        TreeRules rules = new(
            ImmutableDictionary<int, TreeNode>.Empty
                .Add(0, new TreeNode(TreeRules.Seat, 0, [1, 2]))
                .Add(1, new TreeNode(TreeRules.Ally, 0, [3, 4]))
                .Add(2, TreeNode.Over(3))
                .Add(3, new TreeNode(TreeRules.Enemy, 10, [5]))
                .Add(4, new TreeNode(TreeRules.Enemy, 0, [6]) { AllyPoints = 5 })
                .Add(5, TreeNode.Over(10))
                .Add(6, TreeNode.Over(0))
        );

        // Act
        GameAction action = await Tactician(Nothing with { Score = new Sided(1, 0, 1) })
            .Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(TreeRules.Move(0, 2), action);
    }

    [Theory]
    [InlineData(4, "roll")]
    [InlineData(5, "move")]
    public async Task Decide_WhenItMayRollOrMoveForCertain_TakesWhicheverItExpectsMoreOf(int certain, string chosen)
    {
        // Arrange - the die scores 3 or 6, so 4.5 on average
        DecisionRequest request = new ChanceRules(certain).Request();

        // Act
        GameAction action = await Tactician(Nothing with { Score = 1 }).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(chosen == "roll" ? ChanceRules.Roll : ChanceRules.Deliberate, action);
    }

    [Fact]
    public async Task Decide_WhenATakingIsFreeAndItCouldLookAheadForever_TakesItWithinItsBudget()
    {
        // Arrange - Tactician's Pawn beside an enemy Horse no one guards; passing changes nothing, so there is no end
        // to how far it could look
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(3, 4), LineRules.Move(3, 2), new PassAction(LineRules.Seat)],
            (3, LineRules.Pawn, LineRules.Seat),
            (4, LineRules.Horse, LineRules.Enemy)
        );

        // Act
        GameAction action = await new TacticianBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(LineRules.Move(3, 4), action);
    }

    [Fact]
    public async Task Decide_WhenAMoveWouldLeaveAPieceToBeTaken_KeepsItSafe()
    {
        // Arrange - stepping to 4 would leave Tactician's Horse beside an enemy Pawn
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(2, 4), LineRules.Move(2, 1)],
            (2, LineRules.Horse, LineRules.Seat),
            (5, LineRules.Pawn, LineRules.Enemy)
        );

        // Act
        GameAction action = await new TacticianBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(LineRules.Move(2, 1), action);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        // Act
        Task<GameAction> deciding =
            new TacticianBot(new Random(Seed)).Decide(new ChanceRules(0).Request(), cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => deciding);
    }

    [Fact]
    public void Profile_WhenAsked_IsTacticiansWithoutAi()
    {
        // Arrange
        TacticianBot bot = new(new Random(Seed));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((TacticianBot.BotId, "Tactician", false), (profile.Id, profile.Name, profile.UsesAi));
    }

    /// <summary>A node where the enemy can do nothing, seat 0 having scored <paramref name="points"/>.</summary>
    private static TreeNode Stuck(int points) => new(TreeRules.Enemy, points, []);

    private static TacticianBot Tactician(SageWeights weights) =>
        new(new Random(Seed), TacticianBot.Budget, TacticianBot.Width, weights);
}
