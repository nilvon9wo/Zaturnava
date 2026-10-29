using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Scenarios;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Scenarios;

public sealed class ScenarioStartTest
{
    private static readonly SeatId First = new(0);
    private static readonly SeatId Second = new(1);

    private readonly TinyRules _rules = new(6);

    [Fact]
    public void StateFrom_WhenGivenPiecesTurnScoresPrisonersAndDice_SetsThemUpWithTheirStartingIds()
    {
        // Arrange - two Pawns of the first seat's, where a new game has one; the second seat's Pawn held
        Scenario scenario = Tiny() with
        {
            Pieces = ImmutableSortedDictionary<string, string>.Empty
                .Add("a1", "seat 0 pawn")
                .Add("c1", "Seat 0 Pawn")
                .Add("d1", "Seat 1 King"),
            Turn = "Seat 1",
            Scores = ImmutableSortedDictionary<string, int>.Empty.Add("Seat 1", 4),
            Prisoners = [new ScenarioPrisoner("Seat 1 Pawn", "Seat 0")],
            Dice = [2, 2],
        };

        // Act
        GameState state = ScenarioStart.StateFrom(scenario, this._rules);

        // Assert
        Assert.Equal(
            (
                new Piece(new PieceId(0), TinyRules.Pawn, First),
                new Piece(new PieceId(4), TinyRules.Pawn, First),
                new Piece(new PieceId(5), TinyRules.King, Second)
            ),
            (
                state.Position.PieceAt(new CellId(0)),
                state.Position.PieceAt(new CellId(2)),
                state.Position.PieceAt(new CellId(3))
            )
        );
        Assert.Equal(
            (Second, 1, 0, 4),
            (state.DecidingSeat, state.TurnNumber, state.Scores.GetValueOrDefault(First), state.Scores[Second])
        );
        Assert.Equal([new Prisoner(new Piece(new PieceId(2), TinyRules.Pawn, Second), First)], state.Prisoners);
        Assert.Equal([2, 2], state.Dice);
    }

    [Fact]
    public void StateFrom_WhenItSaysLittle_StartsTheFirstSeatOnAnEmptyBoard()
    {
        // Arrange - nothing but its name

        // Act
        GameState state = ScenarioStart.StateFrom(Tiny(), this._rules);

        // Assert
        Assert.Equal((First, 0, true), (state.DecidingSeat, state.Position.Occupants.Count, state.RollOff.IsEmpty));
    }

    [Theory]
    [InlineData("e1", "Seat 0 Pawn", "There is no cell 'e1'.")]
    [InlineData("a1", "Seat 7 Pawn", "'Seat 7 Pawn' is not a seat and a kind of piece, as 'Black Pawn' is.")]
    [InlineData("a1", "Seat 0 Queen", "There is no kind of piece 'Queen'.")]
    public void StateFrom_WhenItNamesWhatTheRulesetLacks_ThrowsInvalidScenarioException(
        string cell,
        string piece,
        string expected
    )
    {
        // Arrange
        Scenario scenario = Tiny() with { Pieces = ImmutableSortedDictionary<string, string>.Empty.Add(cell, piece) };

        // Act
        InvalidScenarioException exception =
            Assert.Throws<InvalidScenarioException>(() => ScenarioStart.StateFrom(scenario, this._rules));

        // Assert
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public void StateOf_WhenTheFirstActionCarriesAScenario_StartsFromIt()
    {
        // Arrange
        ImmutableList<ActionDescriptor> actions = [ScenarioStart.Describe(Tiny() with { Turn = "Seat 1" })];

        // Act
        GameState state = ScenarioStart.StateOf(actions, this._rules);

        // Assert
        Assert.Equal((Second, 0), (state.DecidingSeat, state.Position.Occupants.Count));
    }

    [Fact]
    public void StateOf_WhenNoActionCarriesAScenario_StartsANewGame()
    {
        // Arrange - no actions at all

        // Act
        GameState state = ScenarioStart.StateOf([], this._rules);

        // Assert
        Assert.Equal(this._rules.CreateInitialState().Position.Occupants, state.Position.Occupants);
    }

    private static Scenario Tiny() => new(TinyRules.RulesetId, "Tiny", "For trying");
}
