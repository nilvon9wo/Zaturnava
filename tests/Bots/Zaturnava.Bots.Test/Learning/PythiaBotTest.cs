using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test.Learning;

public sealed class PythiaBotTest
{
    private const int Seed = 7;
    private static readonly MoveAction Take = LineRules.Move(3, 4);
    private static readonly MoveAction Step = LineRules.Move(3, 2);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenItHasNoNetworkForTheRuleset_TakesWhatSageWouldJudgeBest()
    {
        // Arrange - a Pawn beside an enemy Horse no one guards
        DecisionRequest request = Free();

        // Act
        GameAction action = await new PythiaBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Take, action);
    }

    [Fact]
    public async Task Decide_WhenItsNetworkIsOfAnotherSize_TakesWhatSageWouldJudgeBest()
    {
        // Arrange
        DecisionRequest request = Free();
        ValueNetworkData network = ValueNetwork.Untrained(3, 2, new Random(Seed)).ToData(request.Rules.Id);

        // Act
        GameAction action = await Pythia(network, PythiaBot.Horizon).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Take, action);
    }

    [Fact]
    public async Task Decide_WhenItsNetworkJudgesTakingWorse_StepsAsideInstead()
    {
        // Arrange - taught that taking the Horse loses, and stepping aside wins; looking one decision ahead
        DecisionRequest request = Free();
        ValueNetworkData network = Taught.Network(
            request.Rules,
            (request.Rules.Apply(request.State, Take).State, LineRules.Seat, -1),
            (request.Rules.Apply(request.State, Step).State, LineRules.Seat, 1)
        );

        // Act
        GameAction action = await Pythia(network, 1).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Step, action);
    }

    [Fact]
    public async Task Decide_WhenTheEnemyCouldAnswerTheMoveThatLooksBest_PlaysTheOneThatHoldsUpToItsReply()
    {
        // Arrange - moving to 2 scores at once, but the enemy's best reply leaves Pythia 1 at most; moving to 1, its
        // best reply leaves 4
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
        GameAction action = await new PythiaBot(new Random(Seed)).Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(TreeRules.Move(0, 1), action);
    }

    [Fact]
    public async Task Decide_WhenALineHasOnlyOneThingToDoForever_StillLooksNoFurtherThanItsHorizon()
    {
        // Arrange - moving to 1 hands the enemy a move that leads back to where it was, and nothing else, forever;
        // moving to 2 ends the game a point up
        TreeRules rules = new(
            ImmutableDictionary<int, TreeNode>.Empty
                .Add(0, new TreeNode(TreeRules.Seat, 0, [1, 2]))
                .Add(1, new TreeNode(TreeRules.Enemy, 0, [1]))
                .Add(2, TreeNode.Over(1))
        );

        // Act
        GameAction action = await Pythia([], 2).Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(TreeRules.Move(0, 2), action);
    }

    [Fact]
    public async Task Decide_WhenEveryLineEndsTheSame_PlaysOneOfThem()
    {
        // Arrange
        TreeRules rules = new(
            ImmutableDictionary<int, TreeNode>.Empty
                .Add(0, new TreeNode(TreeRules.Seat, 0, [1, 2]))
                .Add(1, TreeNode.Over(0))
                .Add(2, TreeNode.Over(0))
        );

        // Act
        GameAction action = await new PythiaBot(new Random(Seed)).Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Contains(action, (GameAction[])[TreeRules.Move(0, 1), TreeRules.Move(0, 2)]);
    }

    [Theory]
    [InlineData(2, "roll")]
    [InlineData(7, "move")]
    public async Task Decide_WhenItMayRollOrMoveForCertain_TakesWhicheverItExpectsMoreOf(int certain, string chosen)
    {
        // Arrange - the die scores 3 or 6, so 4.5 on average
        DecisionRequest request = new ChanceRules(certain).Request();

        // Act
        GameAction action = await new PythiaBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(chosen == "roll" ? ChanceRules.Roll : ChanceRules.Deliberate, action);
    }

    [Fact]
    public async Task Decide_WhenEveryMoveWouldLeaveAPieceToBeTaken_Passes()
    {
        // Arrange - stepping to 4 would leave Pythia's Horse beside an enemy Pawn
        PassAction pass = new(LineRules.Seat);
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(2, 4), pass],
            (2, LineRules.Horse, LineRules.Seat),
            (5, LineRules.Pawn, LineRules.Enemy)
        );

        // Act
        GameAction action = await new PythiaBot(new Random(Seed)).Decide(request, this._cancellation);

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
        GameAction action = await new PythiaBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(pass, action);
    }

    [Theory]
    [InlineData(20, 5, true)]
    [InlineData(5, 20, false)]
    public async Task Decide_WhenAskedToEndTheGameAndPlayingOnIsJudgedEven_AgreesOnlyIfItsTeamLeads(
        int ours,
        int theirs,
        bool agrees
    )
    {
        // Arrange
        AnswerEndAction agree = new(LineRules.Seat, true);
        DecisionRequest request = LineRules.Request(
            [agree, new AnswerEndAction(LineRules.Seat, false)],
            (3, LineRules.Pawn, LineRules.Seat)
        );
        GameState state = request.State with
        {
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(new SeatId(2), ours).Add(LineRules.Enemy, theirs),
            Proposal = new EndProposal(LineRules.Enemy, false, LineRules.Seat, null),
        };
        ValueNetworkData network = Taught.Network(request.Rules, (state, LineRules.Seat, 0));

        // Act
        GameAction action = await Pythia(network, PythiaBot.Horizon)
            .Decide(request with { State = state }, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(LineRules.Seat, agrees), action);
    }

    [Fact]
    public async Task Decide_WhenAskedToEndAGameItHasNoNetworkFor_AnswersAsSageDoes()
    {
        // Arrange - its team behind, by less than it may hope to take back
        DecisionRequest asked = Proposals.Asked(19, 20);

        // Act
        GameAction action = await new PythiaBot(new Random(Seed)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Proposals.Black, false), action);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        // Act
        Task decided = new PythiaBot(new Random(Seed)).Decide(Free(), cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => decided);
    }

    [Fact]
    public void Profile_WhenAsked_IsAFreeAiCalledPythia()
    {
        // Act
        BotProfile profile = new PythiaBot(new Random(Seed)).Profile;

        // Assert
        Assert.Equal(
            (PythiaBot.BotId, "Pythia", true, false),
            (profile.Id, profile.Name, profile.UsesAi, profile.IsPaid)
        );
    }

    private static DecisionRequest Free() =>
        LineRules.Request([Take, Step], (3, LineRules.Pawn, LineRules.Seat), (4, LineRules.Horse, LineRules.Enemy));

    private static PythiaBot Pythia(ValueNetworkData network, int horizon) =>
        Pythia(ImmutableDictionary<string, ValueNetworkData>.Empty.Add(network.RulesetId, network), horizon);

    private static PythiaBot Pythia(ImmutableDictionary<string, ValueNetworkData> networks, int horizon) =>
        new(new Random(Seed), PythiaBot.Iterations, horizon, networks);
}
