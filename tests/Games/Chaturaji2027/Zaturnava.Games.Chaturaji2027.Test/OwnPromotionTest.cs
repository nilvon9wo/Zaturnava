using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// A piece's own promotion, in a variant where a Pawn may become an Elephant or a Boat in the far two rows, and a
/// Horse an Elephant on the last row.
/// </summary>
public sealed class OwnPromotionTest
{
    private static readonly SeatId Black = new(0);

    private static readonly Chaturaji2027Variant Promoting = Chaturaji2027Variant.Standard with
    {
        OwnPromotions = ImmutableDictionary<PieceKindId, OwnPromotion>.Empty
            .Add(
                Chaturaji2027Pieces.Pawn,
                new OwnPromotion([Chaturaji2027Pieces.Elephant, Chaturaji2027Pieces.Boat], 2)
            )
            .Add(Chaturaji2027Pieces.Horse, new OwnPromotion([Chaturaji2027Pieces.Elephant], 1)),
    };

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Promoting
    );

    [Theory]
    [InlineData("pawn", "elephant boat", "in the far 2 rows")]
    [InlineData("horse", "elephant", "on the last row")]
    public void GuideOf_WhenAKindHasAPromotionOfItsOwn_TellsWhatAndWhereAsPlayed(
        string kind,
        string into,
        string where
    )
    {
        // Arrange - nothing beyond the rules

        // Act
        PieceGuide guide = this._rules.GuideOf(new PieceKindId(kind));

        // Assert
        Assert.Equal(
            (into, true, where),
            (string.Join(" ", guide.PromotesTo.Select(promoted => promoted.Value)), guide.IsPromotionPlayed,
                guide.PromotesWhere)
        );
    }

    [Theory]
    [InlineData(1, "elephant boat")]
    [InlineData(2, "")]
    public void LegalActions_WhenAPawnStandsRowsShortOfTheFarEdge_OffersItsOwnPromotionOnlyInItsRows(
        int rowsAhead,
        string expected
    )
    {
        // Arrange
        GameState state = this.BlackDeciding(Ahead(rowsAhead), Chaturaji2027Pieces.Pawn);

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            expected,
            string.Join(" ", actions.OfType<PromoteOwnAction>().Select(promotion => promotion.Becomes.Value))
        );
    }

    [Fact]
    public void LegalActions_WhenThePiecesSeatHasRetired_OffersNoOwnPromotion()
    {
        // Arrange
        GameState state = this.BlackDeciding(Ahead(0), Chaturaji2027Pieces.Horse) with { Retired = [Black] };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(actions, action => action is PromoteOwnAction);
    }

    [Fact]
    public void Apply_WhenAPawnIsPromotedByItsOwnPromotion_IsThatKindStillItselfAndTheTurnGoesOn()
    {
        // Arrange
        CellId cell = Ahead(1);
        GameState state = this.BlackDeciding(cell, Chaturaji2027Pieces.Pawn);

        // Act
        GameState promoted = this._rules.Apply(
            state,
            new PromoteOwnAction(Black, cell, Chaturaji2027Pieces.Boat)
        ).State;

        // Assert
        Assert.Equal(
            (new Piece(new PieceId(1), Chaturaji2027Pieces.Boat, Black), (SeatId?)Black),
            (promoted.Position.PieceAt(cell), promoted.DecidingSeat)
        );
    }

    /// <summary>The cell of the c-file <paramref name="rows"/> rows short of Black's far edge.</summary>
    private static CellId Ahead(int rows) => new(((7 - rows) * 8) + 2);

    /// <summary>
    /// Black to decide, at the start of its turn, with a piece of <paramref name="kind"/> on <paramref name="cell"/>,
    /// and a Pawn of Yellow's on e5, so that the game goes on.
    /// </summary>
    private GameState BlackDeciding(CellId cell, PieceKindId kind) =>
        this._rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Position = Position.Empty
                .Place(new CellId(36), new Piece(new PieceId(2), Chaturaji2027Pieces.Pawn, new SeatId(1)))
                .Place(cell, new Piece(new PieceId(1), kind, Black)),
        };
}
