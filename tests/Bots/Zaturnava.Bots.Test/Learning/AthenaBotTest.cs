using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Bots.Learning;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test.Learning;

public sealed class AthenaBotTest
{
    private const int Seed = 7;
    private static readonly MoveAction Take = LineRules.Move(3, 4);
    private static readonly MoveAction Step = LineRules.Move(3, 2);
    private static readonly AnswerEndAction Agree = new(LineRules.Seat, true);
    private static readonly AnswerEndAction Refuse = new(LineRules.Seat, false);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenItHasNoNetworkForTheRuleset_PlaysAsSageDoes()
    {
        // Arrange - a Pawn beside an enemy Horse no one guards
        DecisionRequest request = Free();

        // Act
        GameAction action = await new AthenaBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Take, action);
    }

    [Fact]
    public async Task Decide_WhenItsNetworkIsOfAnotherSize_PlaysAsSageDoes()
    {
        // Arrange
        DecisionRequest request = Free();
        ValueNetworkData network = ValueNetwork.Untrained(3, 2, new Random(Seed)).ToData(request.Rules.Id);

        // Act
        GameAction action = await Athena(network).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Take, action);
    }

    [Fact]
    public async Task Decide_WhenItsNetworkJudgesTakingWorse_StepsAsideInstead()
    {
        // Arrange - taught that taking the Horse loses, and stepping aside wins
        DecisionRequest request = Free();
        ValueNetworkData network = Taught.Network(
            request.Rules,
            (request.Rules.Apply(request.State, Take).State, LineRules.Seat, -1),
            (request.Rules.Apply(request.State, Step).State, LineRules.Seat, 1)
        );

        // Act
        GameAction action = await Athena(network).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(Step, action);
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
        DecisionRequest request = LineRules.Request([Agree, Refuse], (3, LineRules.Pawn, LineRules.Seat));
        GameState state = request.State with
        {
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(new SeatId(2), ours).Add(LineRules.Enemy, theirs),
            Proposal = new EndProposal(LineRules.Enemy, false, LineRules.Seat, null),
        };
        ValueNetworkData network = Taught.Network(request.Rules, (state, LineRules.Seat, 0));

        // Act
        GameAction action = await Athena(network).Decide(request with { State = state }, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(LineRules.Seat, agrees), action);
    }

    [Fact]
    public async Task Decide_WhenAskedToEndAGameItHasNoNetworkFor_AnswersAsSageDoes()
    {
        // Arrange - its team behind
        DecisionRequest asked = Proposals.Asked(5, 20);

        // Act
        GameAction action = await new AthenaBot(new Random(Seed)).Decide(asked, this._cancellation);

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
        Task decided = new AthenaBot(new Random(Seed)).Decide(Free(), cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => decided);
    }

    [Fact]
    public void Profile_WhenAsked_IsAFreeAiCalledAthena()
    {
        // Act
        BotProfile profile = new AthenaBot(new Random(Seed)).Profile;

        // Assert
        Assert.Equal(
            (AthenaBot.BotId, "Athena", true, false),
            (profile.Id, profile.Name, profile.UsesAi, profile.IsPaid)
        );
    }

    private static DecisionRequest Free() =>
        LineRules.Request([Take, Step], (3, LineRules.Pawn, LineRules.Seat), (4, LineRules.Horse, LineRules.Enemy));

    private static AthenaBot Athena(ValueNetworkData network) =>
        new(new Random(Seed), ImmutableDictionary<string, ValueNetworkData>.Empty.Add(network.RulesetId, network));
}
