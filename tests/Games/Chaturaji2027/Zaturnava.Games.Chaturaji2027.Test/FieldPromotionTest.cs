using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Promotion in the field, in a variant where a piece two turns on an enemy throne may become a Raja there, and a
/// Pawn has its own promotion, into an Elephant, in the far two rows.
/// </summary>
public sealed class FieldPromotionTest
{
    private const int Turns = 2;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Red = new(1);

    private static readonly Chaturaji2027Variant Promoting = Chaturaji2027Variant.Standard with
    {
        FieldPromotion = new FieldPromotion(Chaturaji2027Pieces.Raja, Turns),
        OwnPromotions = ImmutableDictionary<PieceKindId, OwnPromotion>.Empty.Add(
            Chaturaji2027Pieces.Pawn,
            new OwnPromotion([Chaturaji2027Pieces.Elephant], 2)
        ),
    };

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Promoting
    );

    private CellId RedThrone => this._rules.Thrones[Red];

    [Theory]
    [InlineData(Turns - 1, false)]
    [InlineData(Turns, true)]
    public void LegalActions_WhenAPieceHasStoodOnAnEnemyThrone_OffersTheLeaderOnlyOnceItHasStoodLongEnough(
        int turns,
        bool isOffered
    )
    {
        // Arrange - Black's Boat on Red's throne
        GameState state = this.BlackDeciding(this.RedThrone, Chaturaji2027Pieces.Boat, turns);

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        PromoteOwnAction promotion = new(Black, this.RedThrone, Chaturaji2027Pieces.Raja);
        Assert.Equal(isOffered, actions.Contains(promotion));
    }

    [Fact]
    public void LegalActions_WhenThePiecesSeatHasRetired_OffersNoFieldPromotion()
    {
        // Arrange
        GameState state = this.BlackDeciding(this.RedThrone, Chaturaji2027Pieces.Boat, Turns) with
        {
            Retired = [Black],
        };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(actions, action => action is PromoteOwnAction);
    }

    [Theory]
    [InlineData("boat", "red", Turns + 1)]
    [InlineData("boat", "black", 0)]
    [InlineData("raja", "red", 0)]
    public void Apply_WhenATurnPasses_CountsATurnMoreOnlyForAPieceNotALeaderOnAnEnemysThrone(
        string kind,
        string throne,
        int counted
    )
    {
        // Arrange - Black's piece on a throne, Red's an enemy's or Black's own, some turns there already
        CellId cell = this._rules.Thrones[throne == "red" ? Red : Black];
        GameState state = this.BlackDeciding(cell, new PieceKindId(kind), Turns);

        // Act
        GameState passed = this._rules.Apply(state, new PassAction(Black)).State;

        // Assert
        Assert.Equal(counted, passed.ThroneTenure.GetValueOrDefault(new PieceId(1)));
    }

    [Fact]
    public void Apply_WhenAPieceIsPromotedInTheField_BecomesTheLeaderAndScoresAsOneEnteringTheThrone()
    {
        // Arrange
        GameState state = this.BlackDeciding(this.RedThrone, Chaturaji2027Pieces.Boat, Turns);

        // Act
        Transition promoted = this._rules.Apply(
            state,
            new PromoteOwnAction(Black, this.RedThrone, Chaturaji2027Pieces.Raja)
        );

        // Assert
        Piece raja = new(new PieceId(1), Chaturaji2027Pieces.Raja, Black);
        Assert.Equal(
            (raja, new ThroneEntered(raja, Red, Promoting.Scoring.ThroneEntryBonus)),
            (promoted.State.Position.PieceAt(this.RedThrone), promoted.Events.OfType<ThroneEntered>().Single())
        );
    }

    [Fact]
    public void Apply_WhenAPieceIsPromotedByItsOwnPromotionWhereTheFieldPromotes_ScoresNothingForIt()
    {
        // Arrange - Black's Pawn a row short of the far edge, its own promotion's row
        CellId cell = new((6 * 8) + 2);
        GameState state = this.BlackDeciding(cell, Chaturaji2027Pieces.Pawn, 0);

        // Act
        Transition promoted = this._rules.Apply(state, new PromoteOwnAction(Black, cell, Chaturaji2027Pieces.Elephant));

        // Assert
        Assert.Equal(
            (state.Scores[Black], false),
            (promoted.State.Scores[Black], promoted.Events.OfType<ThroneEntered>().Any())
        );
    }

    /// <summary>
    /// Black to decide, at the start of its turn, with a piece of <paramref name="kind"/> on <paramref name="cell"/>,
    /// there <paramref name="turns"/> turns, and a Pawn of Red's on e5, so that the game goes on.
    /// </summary>
    private GameState BlackDeciding(CellId cell, PieceKindId kind, int turns) =>
        this._rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            Position = Position.Empty
                .Place(new CellId(36), new Piece(new PieceId(2), Chaturaji2027Pieces.Pawn, Red))
                .Place(cell, new Piece(new PieceId(1), kind, Black)),
            ThroneTenure = [new(new PieceId(1), turns)],
        };
}
