using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class AmongPeopleTest
{
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenAsked_TellsTheBotWhichSeatsPeoplePlay()
    {
        // Arrange - a person plays seat 1
        TinyRules rules = new(5);
        GameState state = rules.CreateInitialState();
        Recording bot = new();
        AmongPeople among = new(bot, [new SeatId(1)]);

        // Act
        GameAction action = await among.Decide(
            new DecisionRequest(rules, state, new SeatId(0), rules.LegalActions(state)),
            this._cancellation
        );

        // Assert
        Assert.Equal(rules.LegalActions(state)[0], action);
        Assert.Equal([new SeatId(1)], bot.People);
    }

    /// <summary>A bot that remembers whom it was told people play, and takes the first legal action.</summary>
    private sealed class Recording : IDeciding
    {
        public ImmutableHashSet<SeatId> People { get; private set; } = [];

        public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken)
        {
            this.People = request.People;
            return Task.FromResult(request.LegalActions[0]);
        }
    }
}
