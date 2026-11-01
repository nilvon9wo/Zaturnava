using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class Chaturaji2027VariantTest
{
    private static readonly SeatId Black = new(0);

    /// <summary>A board with no row past the third: nothing may go there.</summary>
    private static readonly Chaturaji2027Variant Fenced = Chaturaji2027Variant.Standard with
    {
        Id = "fenced",
        Name = "Fenced",
        DieNames = ImmutableDictionary<int, string>.Empty.Add(1, "Ships"),
        MayMove = (grid, move) => grid.RowOf(move.To) < 3,
    };

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Fenced
    );

    [Fact]
    public void Identity_WhenAVariantIsGiven_IsTheVariants()
    {
        // Arrange - nothing beyond the rules

        // Act
        (string Id, string Name) identity = (this._rules.Id, this._rules.Name);

        // Assert
        Assert.Equal(("fenced", "Fenced"), identity);
    }

    [Fact]
    public void Game_WhenTheVariantIsOneSizeOfAGame_IsThatGameAtThatSize()
    {
        // Arrange
        Chaturaji2027Rules rules = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Fenced with { Game = new GameSize("fences", "Fences", "small") }
        );

        // Act
        (string Id, string Name, string? Size) game = (rules.GameId, rules.GameName, rules.Size);

        // Assert
        Assert.Equal(("fences", "Fences", "small"), game);
    }

    [Fact]
    public void Game_WhenTheVariantIsAGameOfOneSize_IsItsOwnAtNoSize()
    {
        // Arrange - nothing beyond the rules

        // Act
        (string Id, string Name, string? Size) game = (this._rules.GameId, this._rules.GameName, this._rules.Size);

        // Assert
        Assert.Equal(("fenced", "Fenced", null), game);
    }

    [Theory]
    [InlineData(1, "Ships")]
    [InlineData(2, "Horse")]
    public void NameOfDie_WhenTheVariantNamesSomeValues_NamesThoseItsWay(int value, string expected)
    {
        // Arrange - nothing beyond the rules

        // Act
        string name = this._rules.NameOfDie(value);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void LegalActions_WhenTheBoardForbidsSomeMoves_OffersOnlyTheOthers()
    {
        // Arrange - Black's Pawns may not step to the third row; its Horse may still leap within the first two
        GameState state = this._rules.CreateInitialState() with { TurnNumber = 2, RollOff = [], AwaitedDice = null };

        // Act
        ImmutableList<MoveAction> moves = [.. this._rules.LegalActions(state).OfType<MoveAction>()];

        // Assert
        SquareGridTopology grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);
        Assert.All(moves, move => Assert.True(grid.RowOf(move.To) < 3));
        Assert.DoesNotContain(this._rules.AttacksOf(state, Black), attack => grid.RowOf(attack.Target) >= 3);
    }

    [Fact]
    public void Thrones_WhenRead_AreWhereEachSeatsFirstLeaderStarts()
    {
        // Arrange
        SquareGridTopology grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

        // Act
        ImmutableDictionary<SeatId, CellId> thrones = Fenced.Thrones(grid);

        // Assert
        Assert.Equal(Chaturaji2027Setup.Thrones(grid), thrones);
    }
}
