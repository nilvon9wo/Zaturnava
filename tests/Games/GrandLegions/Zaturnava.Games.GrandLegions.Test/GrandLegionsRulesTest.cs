using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class GrandLegionsRulesTest
{
    /// <summary>The roll-off: the first seat throws a six, the others ones.</summary>
    private static readonly ImmutableList<int> OpeningSix = [6, 1, 1, 1];

    [Fact]
    public void Quick_WhenRead_IsASmallAMediumAndALargeGame()
    {
        // Arrange - nothing beyond the rules

        // Act
        ImmutableList<(string Id, string Name, int Cells)> games =
        [
            .. GrandLegionsRules.Quick.Select(rules =>
                (rules.Id, rules.Name, rules.Topology.Cells.Count)
            ),
        ];

        // Assert
        Assert.Equal(
            [
                ("grand-legions-small", "Grand Legions (small)", 36),
                ("grand-legions-medium", "Grand Legions (medium)", 64),
                ("grand-legions-large", "Grand Legions (large)", 144),
            ],
            games
        );
    }

    [Fact]
    public void IsHeavy_ForEachPiece_IsTrueOnlyForTheHeavyClass()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Medium);

        // Act
        ImmutableList<bool> heavy = [.. GrandLegionsPieces.Catalogue.Select(piece => rules.IsHeavy(piece.Id))];

        // Assert
        Assert.Equal(
            [.. GrandLegionsPieces.Catalogue.Select(piece => piece.Class == PieceClass.Heavy)],
            heavy
        );
    }

    [Fact]
    public void CreateInitialState_ForTheMediumGame_SetsFourArmiesEachLeaderOnItsThrone()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Medium);

        // Act
        GameState state = rules.CreateInitialState();

        // Assert
        Assert.Equal(32, state.Position.Occupants.Count);
        Assert.All(
            rules.Thrones,
            throne => Assert.True(
                state.Position.PieceAt(throne.Value) is { } leader
                && rules.IsLeader(leader.Kind)
                && leader.Owner == throne.Key
            )
        );
    }

    [Theory]
    [InlineData(1, "Artillery")]
    [InlineData(2, "Cavalry")]
    [InlineData(5, "Infantry or Leader")]
    public void NameOfDie_WhenRead_NamesTheClassesItMoves(int value, string expected)
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        string name = rules.NameOfDie(value);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void WorthOf_WhenAsked_IsThePiecesValue()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Large);

        // Act
        (int Worth, string Name) worth = (
            rules.WorthOf(GrandLegionsPieces.DragonKing),
            rules.NameOf(GrandLegionsPieces.DragonKing)
        );

        // Assert
        Assert.Equal((78, "Dragon King (Shogi)"), worth);
    }

    [Theory]
    [InlineData("grand-legions-small")]
    [InlineData("grand-legions-medium")]
    [InlineData("grand-legions-large")]
    public void Apply_WhenTheFirstSeatWinsTheRollOffOnASix_MayMoveInfantryOrItsLeader(string id)
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.Quick.Single(quick => quick.Id == id);
        GameState state = rules.CreateInitialState();

        // Act
        GameState opened = OpeningSix.Aggregate(
            state,
            (rolled, value) => rules.Apply(rolled, new DiceAction(rolled.DecidingSeat!.Value, [value])).State
        );

        // Assert
        ImmutableList<MoveAction> moves = [.. rules.LegalActions(opened).OfType<MoveAction>()];
        Assert.Equal(new SeatId(0), opened.DecidingSeat);
        Assert.NotEmpty(moves);
        Assert.All(
            moves,
            move => Assert.Contains(
                GrandLegionsPieces.ById[opened.Position.PieceAt(move.From)!.Kind].Class,
                new[] { PieceClass.Infantry, PieceClass.Leader }
            )
        );
    }

    [Theory]
    [InlineData("grand-legions-small")]
    [InlineData("grand-legions-medium")]
    [InlineData("grand-legions-large")]
    public void Apply_WhenPlayedOutByAnyLegalAction_EndsTheGame(string id)
    {
        // Arrange - dice and choices alike at random, the same each run
        Chaturaji2027Rules rules = GrandLegionsRules.Quick.Single(quick => quick.Id == id);
        GameState state = rules.CreateInitialState();
        Random random = new(2027);

        // Act
        for (int step = 0; step < 20_000 && state.Outcome is null; step++)
        {
            ImmutableList<GameAction> legal = rules.LegalActions(state);
            GameAction action = state.AwaitedDice is { } dice
                ? new DiceAction(
                    state.DecidingSeat!.Value,
                    [.. Enumerable.Range(0, dice.Count).Select(_ => random.Next(1, dice.Sides + 1))]
                )
                : legal[random.Next(legal.Count)];
            state = rules.Apply(state, action).State;
        }

        // Assert
        Assert.NotNull(state.Outcome);
    }

    [Fact]
    public void DiceOf_WhenTheDiceNameClasses_LetsEveryPieceOfThemMove()
    {
        // Arrange - nothing beyond the six-sided dice

        // Act
        DiceProfile dice = GrandLegionsRules.DiceOf(ClassDice.SixSided);

        // Assert
        Assert.Equal(
            (6, 3),
            (dice.Sides, dice.KindsOf(1).Count)
        );
        Assert.Contains(GrandLegionsPieces.Grasshopper, dice.KindsOf(1));
        Assert.Contains(GrandLegionsPieces.WesternKing, dice.KindsOf(6));
    }
}
