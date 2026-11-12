using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class FillingArmiesTest
{
    private static readonly SeatId First = new(0);
    private static readonly CellId Left = new(0);
    private static readonly CellId Free = new(2);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Decide_WhenItHasNoThroneYet_SetsItWhereTheArmyFilledAtRandomWouldHaveIt()
    {
        // Arrange - the army filled at random would have its throne on the free cell
        TinyRules rules = Filling(state => state with { Thrones = state.Thrones!.SetItem(First, Free) });
        ImmutableList<GameAction> choices =
            [new ThroneAction(First, Left), new ThroneAction(First, Free), new FillArmyAction(First)];

        // Act
        GameAction action = await this.Decided(rules, Throneless(rules), choices);

        // Assert
        Assert.Equal(new ThroneAction(First, Free), action);
    }

    [Theory]
    [InlineData("Pawn", "draft Pawn")]
    [InlineData("King", "fill-army")]
    public async Task Decide_WhenTheArmyFilledAtRandomWouldAddAPiece_DraftsItIfOfferedElseHasTheArmyFilled(
        string added,
        string expected
    )
    {
        // Arrange - a Pawn offered on the free cell
        TinyRules rules = Filling(state => state with
        {
            Position = state.Position.Place(Free, new Piece(new PieceId(9), new PieceKindId(added), First)),
        });
        ImmutableList<GameAction> choices =
            [new DraftAction(First, TinyRules.Pawn, Free), new FillArmyAction(First), new PassAction(First)];

        // Act
        GameAction action = await this.Decided(rules, Throned(rules), choices);

        // Assert
        Assert.Equal(expected, action is DraftAction draft ? $"draft {draft.Piece.Value}" : action.Describe().Kind);
    }

    [Fact]
    public async Task Decide_WhenTheArmyFilledAtRandomWouldAddNothing_IsDone()
    {
        // Arrange - nothing more to add
        TinyRules rules = Filling(state => state);
        ImmutableList<GameAction> choices =
            [new DraftAction(First, TinyRules.Pawn, Free), new FillArmyAction(First), new PassAction(First)];

        // Act
        GameAction action = await this.Decided(rules, Throned(rules), choices);

        // Assert
        Assert.Equal(new PassAction(First), action);
    }

    [Fact]
    public async Task DecideAndSeize_OncePlayHasBegun_AskTheBot()
    {
        // Arrange
        TinyRules rules = new(5, hasFaceOff: true);
        GameState state = rules.CreateInitialState();
        ImmutableList<GameAction> chances = rules.ChancesOf(state, First);
        FillingArmies filling = new(new Choosing());

        // Act
        GameAction decided = await filling.Decide(
            new DecisionRequest(rules, state, First, rules.LegalActions(state)),
            this._cancellation
        );
        GameAction? seized = await filling.Seize(new DecisionRequest(rules, state, First, chances), this._cancellation);

        // Assert
        Assert.Equal((rules.LegalActions(state)[0], chances[0]), (decided, seized));
    }

    private static TinyRules Filling(Func<GameState, GameState> filled) => new(5) { Filled = filled };

    private static GameState Throneless(TinyRules rules) =>
        rules.CreateInitialState() with { Thrones = [] };

    private static GameState Throned(TinyRules rules) =>
        rules.CreateInitialState() with { Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(First, Left) };

    private Task<GameAction> Decided(TinyRules rules, GameState state, ImmutableList<GameAction> choices) =>
        new FillingArmies(new Choosing()).Decide(new DecisionRequest(rules, state, First, choices), this._cancellation);

    /// <summary>A bot that takes the first legal action, or the first chance.</summary>
    private sealed class Choosing : IDeciding
    {
        public Task<GameAction> Decide([Required] DecisionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(request.LegalActions[0]);

        public Task<GameAction?> Seize([Required] DecisionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<GameAction?>(request.LegalActions[0]);
    }
}
