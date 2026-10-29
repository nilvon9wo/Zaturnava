using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class SageBotTest
{
    private const int Seed = 7;
    private static readonly SeatId Seat = ScriptedRules.Seat;
    private static readonly SeatId Enemy = ScriptedRules.Enemy;
    private static readonly SeatId Ally = ScriptedRules.Ally;
    private static readonly PassAction Pass = new(Seat);
    private static readonly SageWeights Nothing = new(0, 0, 0, 0, 0, 0, 0);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(4, "roll")]
    [InlineData(5, "move")]
    public async Task Decide_WhenItMayRollOrMoveForCertain_TakesWhicheverItExpectsMoreOf(int certain, string chosen)
    {
        // Arrange - the die scores 3 or 6, so 4.5 on average
        DecisionRequest request = new ChanceRules(certain).Request();

        // Act
        GameAction action = await new SageBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(chosen == "roll" ? ChanceRules.Roll : ChanceRules.Deliberate, action);
    }

    [Fact]
    public async Task Decide_WhenATakingIsFree_TakesIt()
    {
        // Arrange - Sage's Pawn beside an enemy Horse no one guards
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(3, 4), LineRules.Move(3, 2), new PassAction(LineRules.Seat)],
            (3, LineRules.Pawn, LineRules.Seat),
            (4, LineRules.Horse, LineRules.Enemy)
        );

        // Act
        GameAction action = await new SageBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(LineRules.Move(3, 4), action);
    }

    [Fact]
    public async Task Decide_WhenAMoveWouldLeaveAPieceToBeTaken_KeepsItSafe()
    {
        // Arrange - stepping to 4 would leave Sage's Horse beside an enemy Pawn
        DecisionRequest request = LineRules.Request(
            [LineRules.Move(2, 4), LineRules.Move(2, 1)],
            (2, LineRules.Horse, LineRules.Seat),
            (5, LineRules.Pawn, LineRules.Enemy)
        );

        // Act
        GameAction action = await new SageBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(LineRules.Move(2, 1), action);
    }

    [Theory]
    [InlineData("won", "end")]
    [InlineData("drawn", "score")]
    [InlineData("lost", "score")]
    public async Task Decide_WhenAnActionEndsTheGame_WeighsItsResultAboveAnyPoints(string result, string chosen)
    {
        // Arrange - one action ends the game; the other scores 5
        ImmutableList<SeatId> winners = result switch
        {
            "won" => [Seat, Ally],
            "drawn" => [],
            _ => [Enemy],
        };
        MoveAction end = LineRules.Move(0, 1);
        MoveAction score = LineRules.Move(0, 2);
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty
                .Add(end, ScriptedRules.State() with { Outcome = new GameOutcome(winners, "over", "over") })
                .Add(score, ScriptedRules.State() with { Scores = ImmutableDictionary<SeatId, int>.Empty.Add(Seat, 5) })
        );

        // Act
        GameAction action = await new SageBot(new Random(Seed)).Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(chosen == "end" ? end : score, action);
    }

    [Fact]
    public async Task Decide_WhenWeighingPrisoners_WouldRatherHoldTheEnemysThanHaveItsOwnHeld()
    {
        // Arrange - either way an ally holds one of Sage's Pawns, which counts for nothing
        Prisoner allied = new(Piece(9, LineRules.Pawn, Seat), Ally);
        MoveAction holding = LineRules.Move(0, 1);
        MoveAction held = LineRules.Move(0, 2);
        Prisoner theirs = new(Piece(1, LineRules.Horse, Enemy), Seat);
        Prisoner ours = new(Piece(2, LineRules.Horse, Seat), Enemy);
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty
                .Add(holding, ScriptedRules.State() with { Prisoners = [allied, theirs] })
                .Add(held, ScriptedRules.State() with { Prisoners = [allied, ours] })
        );

        // Act
        GameAction action = await new SageBot(new Random(Seed), Nothing with { Prisoners = 1 })
            .Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(holding, action);
    }

    [Fact]
    public async Task Decide_WhenWeighingThrones_WouldRatherSitOnAnEnemysThanHaveAnEnemyOnItsOwn()
    {
        // Arrange - either way its ally's Raja sits on its own throne, and a Pawn on an enemy's, neither counting
        MoveAction sitting = LineRules.Move(0, 1);
        MoveAction sat = LineRules.Move(0, 2);
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty
                .Add(sitting, ScriptedRules.State(
                    (1, LineRules.Raja, Seat),
                    (2, LineRules.Raja, Ally),
                    (3, LineRules.Pawn, Seat)
                ))
                .Add(sat, ScriptedRules.State(
                    (0, LineRules.Raja, Enemy),
                    (2, LineRules.Raja, Ally),
                    (3, LineRules.Pawn, Seat),
                    (5, LineRules.Raja, Seat)
                ))
        );

        // Act
        GameAction action = await new SageBot(new Random(Seed), Nothing with { Thrones = 1 })
            .Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(sitting, action);
    }

    [Fact]
    public async Task Decide_WhenWeighingLeaders_WouldRatherKeepItsOwnThanTheEnemyKeepTheirs()
    {
        // Arrange
        MoveAction ours = LineRules.Move(0, 1);
        MoveAction theirs = LineRules.Move(0, 2);
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty
                .Add(ours, ScriptedRules.State((5, LineRules.Raja, Seat), (6, LineRules.Pawn, Enemy)))
                .Add(theirs, ScriptedRules.State((5, LineRules.Raja, Enemy), (6, LineRules.Pawn, Seat)))
        );

        // Act
        GameAction action = await new SageBot(new Random(Seed), Nothing with { Leaders = 1 })
            .Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(ours, action);
    }

    [Fact]
    public async Task Decide_WhenOnlyPassingIsLeft_Passes()
    {
        // Arrange
        ScriptedRules rules = new(ImmutableDictionary<GameAction, GameState>.Empty.Add(Pass, ScriptedRules.State()));

        // Act
        GameAction action = await new SageBot(new Random(Seed)).Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(Pass, action);
    }

    [Fact]
    public async Task Decide_WhenOfferedARansom_AnswersAsSolomonDoes()
    {
        // Arrange - worth 3 for 1
        DecisionRequest offered = Ransoms.Offered([20], [10]);

        // Act
        GameAction action = await new SageBot(new Random(Seed)).Decide(offered, this._cancellation);

        // Assert
        Assert.Equal("accept", action.Describe().Kind);
    }

    [Fact]
    public async Task Decide_WhenItsTurnIsOver_GivesItsAllysPieceBackFirst()
    {
        // Arrange
        DecisionRequest request = Ransoms.Window();

        // Act
        GameAction action = await new SageBot(new Random(Seed)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new OfferAction(Ransoms.Black, Ransoms.Green, [new PieceId(30)], []), action);
    }

    [Theory]
    [InlineData(3, 2, true)]
    [InlineData(2, 2, false)]
    [InlineData(1, 2, false)]
    public async Task Decide_WhenAskedToEndTheGame_AgreesOnlyIfItsTeamLeads(int ours, int theirs, bool agrees)
    {
        // Arrange
        DecisionRequest asked = Proposals.Asked(ours, theirs);

        // Act
        GameAction action = await new SageBot(new Random(Seed)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Proposals.Black, agrees), action);
    }

    [Fact]
    public async Task Decide_WhenCancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        // Act
        Task<GameAction> deciding = new SageBot(new Random(Seed)).Decide(new ChanceRules(0).Request(), cancelled.Token);

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => deciding);
    }

    [Fact]
    public void Learned_WhenAsked_HasWeightsForChaturaji2027()
    {
        // Arrange - nothing beyond what Sage has learned

        // Act
        ImmutableDictionary<string, SageWeights> learned = SageWeights.Learned;

        // Assert
        Assert.Equal(SageWeights.Names.Count, learned["chaturaji-2027"].Values.Count);
    }

    [Fact]
    public void With_WhenGivenAWeight_ChangesThatOneOnly()
    {
        // Arrange
        SageWeights weights = SageWeights.Default;

        // Act
        SageWeights changed = weights.With(SageWeights.Names.IndexOf("thrones"), 7);

        // Assert
        Assert.Equal(weights with { Thrones = 7 }, changed);
    }

    [Fact]
    public async Task Decide_WhenItHasLearnedTheRuleset_WeighsAsItLearned()
    {
        // Arrange - learned for these rules, Sage weighs only leaders; else only the score
        MoveAction ours = LineRules.Move(0, 1);
        MoveAction scoring = LineRules.Move(0, 2);
        ScriptedRules rules = new(
            ImmutableDictionary<GameAction, GameState>.Empty
                .Add(ours, ScriptedRules.State((5, LineRules.Raja, Seat)))
                .Add(scoring, ScriptedRules.State() with { Scores = Scored(5) })
        );
        SageBot bot = new(
            new Random(Seed),
            Nothing with { Score = 1 },
            ImmutableDictionary<string, SageWeights>.Empty.Add(rules.Id, Nothing with { Leaders = 1 })
        );

        // Act
        GameAction action = await bot.Decide(rules.Request(), this._cancellation);

        // Assert
        Assert.Equal(ours, action);
    }

    [Fact]
    public void Profile_WhenAsked_IsAPlainAlgorithmCalledSage()
    {
        // Arrange
        SageBot bot = new(new Random(Seed));

        // Act
        BotProfile profile = bot.Profile;

        // Assert
        Assert.Equal((SageBot.BotId, "Sage", false), (profile.Id, profile.Name, profile.UsesAi));
    }

    private static Piece Piece(int id, PieceKindId kind, SeatId owner) => new(new PieceId(id), kind, owner);

    private static ImmutableDictionary<SeatId, int> Scored(int points) =>
        ImmutableDictionary<SeatId, int>.Empty.Add(Seat, points);
}
