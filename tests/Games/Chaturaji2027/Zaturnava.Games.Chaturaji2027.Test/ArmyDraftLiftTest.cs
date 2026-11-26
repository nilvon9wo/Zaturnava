using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Restrictions bought off while drafting (specification §7), on the board of <see cref="ArmyDraftingTest"/>: each
/// kingdom two squares, a1 and b1 for Black, 4 to spend (6, but no more than two Boats are worth) on Pawns (1) and
/// Boats (2); a Boat's area restriction lifted for 1, nothing else liftable.
/// </summary>
public sealed class ArmyDraftLiftTest
{
    private static readonly SeatId Black = new(0);
    private static readonly CellId A1 = new(0);
    private static readonly CellId B1 = new(1);

    private static readonly Drafting Drafting = new(
        6,
        [new ArmySquare(0, 0), new ArmySquare(1, 0)],
        (_, _) => [Chaturaji2027Pieces.Pawn, Chaturaji2027Pieces.Boat],
        fill => new ArmyFilled([], fill.Throne ?? new ArmySquare(0, 0))
    )
    {
        LiftPrice = (kind, restriction) =>
            kind == Chaturaji2027Pieces.Boat && restriction == LiftKind.Area ? 1 : null,
    };

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with { Drafting = Drafting }
    );

    [Fact]
    public void LegalActions_WhenTheSeatHasABoatAndAPawn_OffersBuyingOffTheBoatsAreaRestrictionAlone()
    {
        // Arrange - Black's Pawn on a1 and Boat on b1, 1 left
        GameState state = this.Bought(this.Begun(), (Chaturaji2027Pieces.Pawn, A1), (Chaturaji2027Pieces.Boat, B1));

        // Act
        ImmutableList<LiftAction> lifts = [.. this._rules.LegalActions(state).OfType<LiftAction>()];

        // Assert
        Assert.Equal([new LiftAction(Black, B1, LiftKind.Area)], lifts);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void LegalActions_WhenTheSeatHasLeft_OffersTheLiftOnlyIfItCanPayForIt(int left, bool expected)
    {
        // Arrange - Black's Boat on b1
        GameState state = this.Bought(this.Begun(), (Chaturaji2027Pieces.Boat, B1));
        state = state with { Draft = state.Draft! with { Left = state.Draft.Left.SetItem(Black, left) } };

        // Act
        bool offered = this._rules.LegalActions(state).OfType<LiftAction>().Any();

        // Assert
        Assert.Equal(expected, offered);
    }

    [Fact]
    public void Apply_WhenASeatBuysOffARestriction_SpendsItsPriceKeepsTheLiftAndTellsSo()
    {
        // Arrange - Black's Boat on b1, 2 left
        GameState state = this.Bought(this.BegunAllAtOnce(), (Chaturaji2027Pieces.Boat, B1));

        // Act
        Transition lifted = this._rules.Apply(state, new LiftAction(Black, B1, LiftKind.Area));

        // Assert
        Piece boat = lifted.State.Position.PieceAt(B1)!;
        Assert.Equal(
            (1, true, false, new RestrictionLifted(Black, Chaturaji2027Pieces.Boat, B1, LiftKind.Area, 1)),
            (
                lifted.State.Draft!.Left[Black],
                lifted.State.IsLifted(boat, LiftKind.Area),
                this._rules.ChancesOf(lifted.State, Black).OfType<LiftAction>().Any(),
                Assert.Single(lifted.Events)
            )
        );
    }

    [Fact]
    public void Apply_WhenASeatTakesBackAPieceItBoughtALiftFor_RefundsBothAndForgetsTheLift()
    {
        // Arrange - Black's Boat on b1, its area restriction bought off: 1 left
        GameState state = this.Bought(this.BegunAllAtOnce(), (Chaturaji2027Pieces.Boat, B1));
        state = this._rules.Apply(state, new LiftAction(Black, B1, LiftKind.Area)).State;

        // Act
        Transition taken = this._rules.Apply(state, new UndraftAction(Black, B1));

        // Assert
        Assert.Equal(
            (4, 0, new PieceUndrafted(Black, Chaturaji2027Pieces.Boat, B1, 3)),
            (taken.State.Draft!.Left[Black], taken.State.Lifts.Count, Assert.Single(taken.Events))
        );
    }

    [Fact]
    public void Apply_WhenADraftInTurnLeavesOnlyALiftToBuy_LeavesTheSeatDraftingUntilItBuysIt()
    {
        // Arrange - Black's Pawn on a1: 3 left
        GameState state = this.Bought(this.Begun(), (Chaturaji2027Pieces.Pawn, A1));

        // Act - a Boat on b1 fills the kingdom, 1 left: only its lift is left to buy, then nothing
        GameState bought = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Boat, B1)).State with
        {
            DecidingSeat = Black,
        };
        GameState lifted = this._rules.Apply(bought, new LiftAction(Black, B1, LiftKind.Area)).State;

        // Assert
        Assert.Equal((false, true), (bought.Draft!.Done.Contains(Black), lifted.Draft!.Done.Contains(Black)));
    }

    [Fact]
    public void Apply_WhenTheOneDrafterIsDone_GivesEveryArmysCopyOfAPieceItsLiftToo()
    {
        // Arrange - Black drafts for all: throne on a1, a Boat on b1, its area restriction bought off
        GameState state = this._rules.Drafted(this._rules.CreateInitialState(), 7, new DraftSettings(Drafter: Black));
        state = this._rules.Apply(state, new ThroneAction(Black, A1)).State;
        state = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Boat, B1)).State;
        state = this._rules.Apply(state, new LiftAction(Black, B1, LiftKind.Area)).State;

        // Act
        GameState started = this._rules.Apply(state, new PassAction(Black)).State;

        // Assert - every army's Boat lifted
        ImmutableList<Piece> boats = [.. started.Position.Occupants.Values];
        Assert.Equal(
            (4, 4),
            (boats.Count, boats.Count(boat => started.IsLifted(boat, LiftKind.Area)))
        );
    }

    /// <summary>Black's throne set on a1, and each piece bought on its cell, Black deciding.</summary>
    private GameState Bought(GameState state, params (PieceKindId Kind, CellId Cell)[] pieces) =>
        pieces.Aggregate(
            state with { Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1) },
            (bought, piece) =>
                this._rules.Apply(bought, new DraftAction(Black, piece.Kind, piece.Cell)).State with
                {
                    DecidingSeat = Black,
                }
        );

    private GameState Begun() => this._rules.Drafted(this._rules.CreateInitialState(), 7);

    private GameState BegunAllAtOnce() =>
        this._rules.Drafted(this._rules.CreateInitialState(), 7, new DraftSettings { IsSimultaneous = true });
}
