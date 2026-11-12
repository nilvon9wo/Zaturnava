using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class FillingArmiesTest
{
    private static readonly SeatId First = new(0);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;
    private readonly TinyRules _rules = new(5, hasFaceOff: true);

    [Fact]
    public async Task Decide_WhenItMayHaveItsArmyFilled_HasItFilledWithoutAskingTheBot()
    {
        // Arrange - a throne, or the army filled at random
        GameState state = this._rules.CreateInitialState();
        ImmutableList<GameAction> choices = [new ThroneAction(First, new(0)), new FillArmyAction(First)];
        FillingArmies filling = new(new Choosing());

        // Act
        GameAction action = await filling.Decide(
            new DecisionRequest(this._rules, state, First, choices),
            this._cancellation
        );

        // Assert
        Assert.Equal(new FillArmyAction(First), action);
    }

    [Fact]
    public async Task DecideAndSeize_OncePlayHasBegun_AskTheBot()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();
        ImmutableList<GameAction> chances = this._rules.ChancesOf(state, First);
        FillingArmies filling = new(new Choosing());

        // Act
        GameAction decided = await filling.Decide(
            new DecisionRequest(this._rules, state, First, this._rules.LegalActions(state)),
            this._cancellation
        );
        GameAction? seized = await filling.Seize(
            new DecisionRequest(this._rules, state, First, chances),
            this._cancellation
        );

        // Assert
        Assert.Equal((this._rules.LegalActions(state)[0], chances[0]), (decided, seized));
    }

    /// <summary>A bot that takes the first legal action, or the first chance.</summary>
    private sealed class Choosing : IDeciding
    {
        public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(request.LegalActions[0]);

        public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<GameAction?>(request.LegalActions[0]);
    }
}
