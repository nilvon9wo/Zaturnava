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
    public void StateOf_WhenTheFirstActionDealsTheGame_DealsItByItsSeed()
    {
        // Arrange - the loaded dice roll the seed, 4, which the tiny rules score to the first seat
        ImmutableList<ActionDescriptor> actions = [ScenarioStart.Dealt(new LoadedDice(4))];

        // Act
        GameState state = ScenarioStart.StateOf(actions, new TinyRules(6, isDealt: true));

        // Assert
        Assert.Equal(
            (true, false, 4),
            (
                ScenarioStart.IsStart(actions[0]),
                ScenarioStart.IsStart(new PassAction(First).Describe()),
                state.Scores[First]
            )
        );
    }

    [Fact]
    public void StateOf_WhenTheGameWasDealtBeforeTerrainWas_DealsItNone()
    {
        // Arrange - a deal of seed 4 as games were dealt before terrain was, and one dealt now
        ActionDescriptor now = ScenarioStart.Dealt(new LoadedDice(4));
        ActionDescriptor before = now with { Arguments = now.Arguments.Remove("terrain") };
        TinyRules rules = new(6, isDealt: true);

        // Act
        (int Before, int Now) terrain = (
            ScenarioStart.StateOf([before], rules).Terrain,
            ScenarioStart.StateOf([now], rules).Terrain
        );

        // Assert
        Assert.Equal((0, 1), terrain);
    }

    [Fact]
    public void StateOf_WhenTheGameWasDealtBeforePalacePiecesWere_DealsItsArmiesNone()
    {
        // Arrange - a deal of seed 4 as games were dealt before armies held palace pieces, and one dealt now
        ActionDescriptor now = ScenarioStart.Dealt(new LoadedDice(4));
        ActionDescriptor before = now with { Arguments = now.Arguments.Remove("palace-pieces") };
        TinyRules rules = new(6, isDealt: true);

        // Act
        (bool Before, bool Now) held = (
            ScenarioStart.StateOf([before], rules).HasPalacePieces,
            ScenarioStart.StateOf([now], rules).HasPalacePieces
        );

        // Assert
        Assert.Equal((false, true), held);
    }

    [Fact]
    public void StateOf_WhenTheGameWasDealtBeforeEdgePalacesWere_DealsItNone()
    {
        // Arrange - a deal of seed 4 as games were dealt before a palace could stand partly outside a kingdom
        ActionDescriptor now = ScenarioStart.Dealt(new LoadedDice(4));
        ActionDescriptor before = now with { Arguments = now.Arguments.Remove("edge-palaces") };
        TinyRules rules = new(6, isDealt: true);

        // Act
        (bool Before, bool Now) atEdge = (
            ScenarioStart.StateOf([before], rules).HasEdgePalaces,
            ScenarioStart.StateOf([now], rules).HasEdgePalaces
        );

        // Assert
        Assert.Equal((false, true), atEdge);
    }

    [Fact]
    public void StateOf_WhenTheGameIsDrafted_DealsItThenHasItsArmiesDraftedBySeed()
    {
        // Arrange
        ImmutableList<ActionDescriptor> actions =
            [ScenarioStart.Dealt(new LoadedDice(4), drafting: DraftSettings.Default)];

        // Act
        GameState state = ScenarioStart.StateOf(actions, new TinyRules(6, isDealt: true));

        // Assert
        Assert.Equal((4, 4, 1), (state.Draft!.Seed, state.Scores[First], state.Terrain));
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(30, 1, false)]
    [InlineData(null, null, true)]
    public void DraftingOf_WhenTheGameWasDraftedAsSet_ReplaysItSoAndSaysHow(int? budget, int? drafter, bool isSecret)
    {
        // Arrange
        DraftSettings set = new(budget, drafter is { } seat ? new SeatId(seat) : null) { IsSecret = isSecret };
        ImmutableList<ActionDescriptor> actions = [ScenarioStart.Dealt(new LoadedDice(4), drafting: set)];

        // Act
        GameState state = ScenarioStart.StateOf(actions, new TinyRules(6, isDealt: true));

        // Assert
        Assert.Equal(
            (set, budget ?? 0, set.Drafter),
            (ScenarioStart.DraftingOf(actions), state.Draft!.Left[First], state.Draft.Drafter)
        );
    }

    [Fact]
    public void RulesOf_WhenTheGamesBoardWasSet_PlaysItOnThatBoardAndElseAsTheRulesetIs()
    {
        // Arrange
        TinyRules rules = new(6, isDealt: true) { SizableBoard = new BoardSize(4, 1, 1) };
        ImmutableList<ActionDescriptor> sized = [ScenarioStart.Dealt(new LoadedDice(4), board: new BoardSize(3, 1, 1))];
        ImmutableList<ActionDescriptor> unsized = [ScenarioStart.Dealt(new LoadedDice(4))];

        // Act
        (string Sized, string Unsized) names =
            (ScenarioStart.RulesOf(sized, rules).Name, ScenarioStart.RulesOf(unsized, rules).Name);

        // Assert
        Assert.Equal(
            (("Tiny 3", "Tiny"), (BoardSize?)new BoardSize(3, 1, 1), (BoardSize?)null),
            (names, ScenarioStart.BoardOf(sized), ScenarioStart.BoardOf(unsized))
        );
    }

    [Fact]
    public void RulesOf_WhenTheGamesBoardMayNoLongerBeSet_ThrowsArgumentException()
    {
        // Arrange - a board of one cell a side, which the tiny rules are never played on
        ImmutableList<ActionDescriptor> actions =
            [ScenarioStart.Dealt(new LoadedDice(4), board: new BoardSize(1, 1, 1))];
        TinyRules rules = new(6, isDealt: true) { SizableBoard = new BoardSize(4, 1, 1) };

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ScenarioStart.RulesOf(actions, rules));

        // Assert
        Assert.StartsWith(
            "Tiny is not played on BoardSize { Side = 1, KingdomWidth = 1, KingdomDepth = 1 }.",
            exception.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void IsDrafted_WhenTheGameWasNotDrafted_IsFalseElseTrue()
    {
        // Arrange
        ImmutableList<ImmutableList<ActionDescriptor>> games =
        [
            [],
            [ScenarioStart.Describe(Tiny())],
            [ScenarioStart.Dealt(new LoadedDice(4), areAlike: true)],
            [ScenarioStart.Dealt(new LoadedDice(4), drafting: DraftSettings.Default)],
        ];

        // Act
        ImmutableList<bool> drafted = [.. games.Select(ScenarioStart.IsDrafted)];

        // Assert
        Assert.Equal([false, false, false, true], drafted);
    }

    [Fact]
    public void AreAlike_WhenTheGameWasNotDealtAlike_IsFalseElseTrue()
    {
        // Arrange
        ImmutableList<ImmutableList<ActionDescriptor>> games =
        [
            [],
            [ScenarioStart.Describe(Tiny())],
            [ScenarioStart.Dealt(new LoadedDice(4))],
            [ScenarioStart.Dealt(new LoadedDice(4), areAlike: true)],
        ];

        // Act
        ImmutableList<bool> alike = [.. games.Select(ScenarioStart.AreAlike)];

        // Assert
        Assert.Equal([false, false, false, true], alike);
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
