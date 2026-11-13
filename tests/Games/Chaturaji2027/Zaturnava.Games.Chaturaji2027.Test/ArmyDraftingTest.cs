using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// The armies drafted, on Chaturaji 2027's board, each kingdom two squares - seat 0's a1 and b1 (cells 0 and 1) - each
/// seat with 6 to spend on Pawns (1), Boats (2) and Rajas (5); an army filled at random gains a Pawn on b1.
/// </summary>
public sealed class ArmyDraftingTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly CellId A1 = new(0);
    private static readonly CellId B1 = new(1);

    private static readonly Drafting Drafting = new(
        6,
        [new ArmySquare(0, 0), new ArmySquare(1, 0)],
        (_, _) => [Chaturaji2027Pieces.Pawn, Chaturaji2027Pieces.Boat, Chaturaji2027Pieces.Raja],
        fill => new ArmyFilled([new ArmyPiece(Chaturaji2027Pieces.Pawn, 1, 0)], fill.Throne ?? new ArmySquare(0, 0))
    );

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with { Drafting = Drafting }
    );

    [Fact]
    public void Drafted_WhenTheVariantLetsPlayersDraft_EmptiesTheBoardForTheFirstSeatToDraftWithTheWholeBudget()
    {
        // Arrange
        GameState dealt = this._rules.CreateInitialState();

        // Act
        GameState drafted = this._rules.Drafted(dealt, 7);

        // Assert
        Assert.Equal(
            (true, 0, 0, Black, 7, 6, true, null as DiceRequest),
            (
                this._rules.IsDraftable,
                drafted.Position.Occupants.Count,
                drafted.Thrones!.Count,
                drafted.DecidingSeat!.Value,
                drafted.Draft!.Seed,
                drafted.Draft.Left[Red],
                drafted.RollOff.IsEmpty,
                drafted.AwaitedDice
            )
        );
    }

    [Fact]
    public void Sized_WhenTheVariantResizes_PlaysItAtTheSizesItAllowsOnceEachAndElseNowhere()
    {
        // Arrange - a variant whose creator may set it to 8 cells a side alone
        BoardSize eight = new(8, 8, 4, 2);
        Chaturaji2027Rules rules = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Chaturaji2027Variant.Standard with
            {
                Board = eight,
                Resized = size => size == eight ? Chaturaji2027Variant.Standard with { Id = "resized" } : null,
            }
        );

        // Act
        IRulesApplying sized = rules.Sized(eight)!;

        // Assert
        Assert.Equal(
            ((BoardSize?)eight, "resized", true, true, true, true, (BoardSize?)eight, true),
            (
                rules.SizableBoard,
                sized.Id,
                ReferenceEquals(sized, rules.Sized(eight)),
                rules.Sized(new BoardSize(9, 9, 4, 2)) is null,
                new Chaturaji2027Rules().Sized(eight) is null,
                new Chaturaji2027Rules().SizableBoard is null,
                rules.Board,
                new Chaturaji2027Rules().Board is null
            )
        );
    }

    [Fact]
    public void Drafted_WhenTheGamesCreatorSetsABudgetAndADrafter_HasThatSeatAloneDraftWithIt()
    {
        // Arrange
        GameState dealt = this._rules.CreateInitialState();

        // Act
        GameState drafted = this._rules.Drafted(dealt, 7, new DraftSettings(9, Yellow));

        // Assert
        Assert.Equal(
            (6, 9, Yellow, Yellow, "0,2,3"),
            (
                this._rules.DraftBudget,
                drafted.Draft!.Left[Red],
                drafted.Draft.Drafter,
                drafted.DecidingSeat!.Value,
                string.Join(",", drafted.Draft.Done.Select(seat => seat.Value).Order())
            )
        );
    }

    [Fact]
    public void Drafted_WhenTheGamesCreatorSetsMoreThanAnArmyCouldSpend_HoldsEachToItsKingdomFullOfTheDearest()
    {
        // Arrange - two squares, the dearest kind a Raja (5)
        GameState dealt = this._rules.CreateInitialState();

        // Act
        GameState drafted = this._rules.Drafted(dealt, 7, new DraftSettings(1_000_000));

        // Assert
        Assert.Equal(((int?)10, 10), (this._rules.DraftBudgetLimit, drafted.Draft!.Left[Red]));
    }

    [Fact]
    public void Apply_WhenTheOneDrafterIsDone_GivesEverySeatItsArmyAliveAndBeginsTheGame()
    {
        // Arrange - Yellow drafts for all, and has its army filled: a Pawn on its second square, throne on its first
        GameState state = this._rules.Drafted(this._rules.CreateInitialState(), 7, new DraftSettings(Drafter: Yellow));

        // Act
        GameState started = this._rules.Apply(state, new FillArmyAction(Yellow)).State;

        // Assert
        Piece black = started.Position.PieceAt(B1)!;
        Assert.Equal(
            (Chaturaji2027Pieces.Pawn, Black, A1, 5, 4, 4, true),
            (
                black.Kind,
                black.Owner,
                started.Thrones![Black],
                started.Scores[Black],
                started.Position.Occupants.Count,
                started.Thrones.Count,
                started.Draft is null
            )
        );
    }

    [Fact]
    public void DraftOffered_WhileTheArmiesAreDraftedAndNotOtherwise_OffersEveryKindDearestFirst()
    {
        // Arrange
        GameState drafted = this.Begun();

        // Act
        (string During, int Before) offered = (
            string.Join(",", this._rules.DraftOffered(drafted).Select(kind => kind.Value)),
            this._rules.DraftOffered(this._rules.CreateInitialState()).Count
        );

        // Assert
        Assert.Equal(("raja,boat,pawn", 0), offered);
    }

    [Fact]
    public void Drafted_WhenTheVariantLetsNoOneDraft_LeavesTheGameAsDealt()
    {
        // Arrange
        Chaturaji2027Rules rules = new();
        GameState dealt = rules.CreateInitialState();

        // Act
        GameState drafted = rules.Drafted(dealt, 7);

        // Assert
        Assert.Equal(
            (false, dealt, (int?)null, (int?)null),
            (rules.IsDraftable, drafted, rules.DraftBudget, rules.DraftBudgetLimit)
        );
    }

    [Fact]
    public void LegalActions_BeforeTheSeatHasAThrone_OffersItsKingdomsSquaresForOneAndFillingAtRandom()
    {
        // Arrange
        GameState drafted = this.Begun();

        // Act
        ImmutableList<GameAction> choices = this._rules.LegalActions(drafted);

        // Assert
        ImmutableList<GameAction> expected =
            [new ThroneAction(Black, A1), new ThroneAction(Black, B1), new FillArmyAction(Black)];
        Assert.Equal(expected, choices);
    }

    [Fact]
    public void LegalActions_OnceTheSeatHasAThrone_OffersEachKindItCanPayForOnEachFreeSquareDearestFirst()
    {
        // Arrange - Black has bought a Pawn on a1, leaving 5, and drafts again
        GameState state = this.Begun() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };
        state = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Pawn, A1)).State;
        state = state with { DecidingSeat = Black };

        // Act
        ImmutableList<GameAction> choices = this._rules.LegalActions(state);

        // Assert
        ImmutableList<GameAction> expected =
        [
            new DraftAction(Black, Chaturaji2027Pieces.Raja, B1),
            new DraftAction(Black, Chaturaji2027Pieces.Boat, B1),
            new DraftAction(Black, Chaturaji2027Pieces.Pawn, B1),
            new FillArmyAction(Black),
            new PassAction(Black),
        ];
        Assert.Equal(expected, choices);
    }

    [Fact]
    public void Apply_WhenASeatBuysAPiece_PlacesItSpendsItsPriceAndPassesTheTurnOn()
    {
        // Arrange
        GameState state = this.Begun() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };

        // Act
        Transition bought = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Boat, B1));

        // Assert
        Piece boat = bought.State.Position.PieceAt(B1)!;
        Assert.Equal(
            (Chaturaji2027Pieces.Boat, Black, 4, Yellow),
            (boat.Kind, boat.Owner, bought.State.Draft!.Left[Black], bought.State.DecidingSeat!.Value)
        );
    }

    [Theory]
    [InlineData("raja", 0, true)]
    [InlineData("pawn", 4, false)]
    public void Apply_WhenASeatCanBuyNothingMore_HasItDoneWithoutAsking(string kind, int left, bool isDone)
    {
        // Arrange - 5 left: a Raja spends it all; a Pawn leaves 4, enough for more on b1
        GameState state = this.Begun() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };
        state = state with { Draft = state.Draft! with { Left = state.Draft.Left.SetItem(Black, 5) } };

        // Act
        GameState bought = this._rules.Apply(state, new DraftAction(Black, new PieceKindId(kind), A1)).State;

        // Assert
        Assert.Equal((left, isDone), (bought.Draft!.Left[Black], bought.Draft.Done.Contains(Black)));
    }

    [Fact]
    public void Apply_WhenASeatFillsItsLastFreeSquare_HasItDoneWithoutAsking()
    {
        // Arrange - a Pawn on a1 already; a Pawn on b1 leaves 4, but no square
        GameState state = this.Begun() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };
        state = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Pawn, A1)).State with
        {
            DecidingSeat = Black,
        };

        // Act
        GameState bought = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Pawn, B1)).State;

        // Assert
        Assert.Equal((4, true), (bought.Draft!.Left[Black], bought.Draft.Done.Contains(Black)));
    }

    [Fact]
    public void Apply_WhenASeatSetsItsThrone_SetsItThere()
    {
        // Arrange
        GameState state = this.Begun();

        // Act
        GameState throned = this._rules.Apply(state, new ThroneAction(Black, B1)).State;

        // Assert
        Assert.Equal((B1, Yellow), (throned.Thrones![Black], throned.DecidingSeat!.Value));
    }

    [Fact]
    public void Apply_WhenASeatIsDone_PassesItOverAfterwards()
    {
        // Arrange - Yellow is done already; Black has its throne
        GameState state = this.Begun() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
            Draft = this.Begun().Draft! with { Done = [Yellow] },
        };

        // Act
        GameState passed = this._rules.Apply(state, new PassAction(Black)).State;

        // Assert
        Assert.Equal((Green, true), (passed.DecidingSeat!.Value, passed.Draft!.Done.Contains(Black)));
    }

    [Fact]
    public void Apply_WhenASeatHasItsArmyFilled_PlacesWhatTheVariantFillsItWithAndSetsItsThrone()
    {
        // Arrange
        GameState state = this.Begun();

        // Act
        GameState filled = this._rules.Apply(state, new FillArmyAction(Black)).State;

        // Assert
        Assert.Equal(
            (Chaturaji2027Pieces.Pawn, A1, 5, true),
            (
                filled.Position.PieceAt(B1)!.Kind,
                filled.Thrones![Black],
                filled.Draft!.Left[Black],
                filled.Draft.Done.Contains(Black)
            )
        );
    }

    [Fact]
    public void Apply_WhenTheLastSeatIsDone_StartsTheGameFromTheArmiesDraftedWithWhatEachKeptAsItsScore()
    {
        // Arrange - Black, Yellow and Green are done; Black kept 6, having bought nothing
        GameState state = this.Begun() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Red, new CellId(7)),
            Draft = this.Begun().Draft! with { Done = [Black, Yellow, Green] },
            DecidingSeat = Red,
        };

        // Act
        GameState started = this._rules.Apply(state, new FillArmyAction(Red)).State;

        // Assert
        Assert.Equal(
            (null as ArmyDraft, started.Position, 6, 5, 6, 0, 4, Black, true),
            (
                started.Draft,
                started.Start,
                started.Scores[Black],
                started.Scores[Red],
                started.Unspent[Black],
                started.EarnedBy(Red),
                started.RollOff.Count,
                started.DecidingSeat!.Value,
                started.AwaitedDice is not null
            )
        );
    }

    [Fact]
    public void Apply_WhenASeatSetsItsThroneOrBuysAPiece_TellsWhatItDidAndWhere()
    {
        // Arrange
        GameState state = this.Begun();

        // Act
        GameState throned = this._rules.Apply(state, new ThroneAction(Black, A1)).State with { DecidingSeat = Black };
        ImmutableList<GameEvent> told =
        [
            .. this._rules.Apply(state, new ThroneAction(Black, A1)).Events,
            .. this._rules.Apply(throned, new DraftAction(Black, Chaturaji2027Pieces.Boat, B1)).Events,
        ];

        // Assert
        ImmutableList<GameEvent> expected =
            [new ThroneSet(Black, A1), new PieceDrafted(Black, Chaturaji2027Pieces.Boat, B1, 2)];
        Assert.Equal(expected, told);
    }

    [Fact]
    public void Apply_WhenASeatSpendsAllItHas_TellsItIsDoneKeepingNothing()
    {
        // Arrange
        GameState state = this.Begun() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };
        state = state with { Draft = state.Draft! with { Left = state.Draft.Left.SetItem(Black, 5) } };

        // Act
        Transition bought = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Raja, A1));

        // Assert
        ImmutableList<GameEvent> expected =
            [new PieceDrafted(Black, Chaturaji2027Pieces.Raja, A1, 5), new DraftFinished(Black, 0)];
        Assert.Equal(expected, bought.Events);
    }

    [Fact]
    public void Apply_WhenASeatIsDoneOrFilledAtRandom_TellsHowManyPiecesItGainedAndWhatItKept()
    {
        // Arrange - Black has its throne, so may be done
        GameState state = this.Begun() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };

        // Act
        ImmutableList<GameEvent> told =
        [
            .. this._rules.Apply(state, new PassAction(Black)).Events,
            .. this._rules.Apply(this.Begun(), new FillArmyAction(Black)).Events,
        ];

        // Assert
        ImmutableList<GameEvent> expected =
            [new DraftFinished(Black, 6), new ArmyFilledAtRandom(Black, 1), new DraftFinished(Black, 5)];
        Assert.Equal(expected, told);
    }

    [Fact]
    public void Apply_WhenTheOneDrafterIsDone_TellsThatEveryOtherArmyIsDraftedAlike()
    {
        // Arrange
        GameState state = this._rules.Drafted(this._rules.CreateInitialState(), 7, new DraftSettings(Drafter: Yellow));

        // Act
        Transition started = this._rules.Apply(state, new FillArmyAction(Yellow));

        // Assert
        ImmutableList<GameEvent> expected =
            [new ArmyFilledAtRandom(Yellow, 1), new DraftFinished(Yellow, 5), new ArmiesCopied(Yellow)];
        Assert.Equal(expected, started.Events);
    }

    [Fact]
    public void Apply_WhenTheActionIsNoneOfTheDrafts_ThrowsIllegalActionException()
    {
        // Arrange
        GameState state = this.Begun();

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, new PassAction(Black))
        );

        // Assert
        Assert.Equal("That pass is not allowed now.", exception.Message);
    }

    [Fact]
    public void ChancesOfAndMayProposeEnd_WhileTheArmiesAreDrafted_OfferNothing()
    {
        // Arrange
        GameState state = this.Begun();

        // Act
        (int chances, bool mayPropose) =
            (this._rules.ChancesOf(state, Yellow).Count, this._rules.MayProposeEnd(state, Yellow));

        // Assert
        Assert.Equal((0, false), (chances, mayPropose));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(1, false)]
    public void Drafted_WhenTheGamesCreatorSetsItAllAtOnce_SaysSoUnlessOneSeatDraftsForAll(int? drafter, bool expected)
    {
        // Arrange
        DraftSettings settings = new(Drafter: drafter is { } seat ? new SeatId(seat) : null) { IsSimultaneous = true };

        // Act
        GameState drafted = this._rules.Drafted(this._rules.CreateInitialState(), 7, settings);

        // Assert
        Assert.Equal(expected, drafted.Draft!.IsSimultaneous);
    }

    [Fact]
    public void ChancesOf_WhileDraftedAllAtOnce_OffersEverySeatButTheDecidingOneItsChoices()
    {
        // Arrange - Black decides
        GameState state = this.BegunAllAtOnce();

        // Act
        (ImmutableList<GameAction> yellow, int black) =
            (this._rules.ChancesOf(state, Yellow), this._rules.ChancesOf(state, Black).Count);

        // Assert - a throne on either square of Yellow's kingdom, or filling its army
        Assert.Equal(
            ("throne,throne,fill-army", true, 0),
            (
                string.Join(",", yellow.Select(chance => chance.Describe().Kind)),
                yellow.All(chance => chance.Actor == Yellow),
                black
            )
        );
    }

    [Fact]
    public void Apply_WhenASeatDraftsOutOfTurnAllAtOnce_DoesSoLeavingTheTurnWhereItWas()
    {
        // Arrange - Black decides
        GameState state = this.BegunAllAtOnce();
        ThroneAction throne = (ThroneAction)this._rules.ChancesOf(state, Yellow)[0];

        // Act
        GameState acted = this._rules.Apply(state, throne).State;

        // Assert
        Assert.Equal((throne.Cell, Black), (acted.Thrones![Yellow], acted.DecidingSeat!.Value));
    }

    [Fact]
    public void Apply_WhenASeatDraftsOutOfTurnInTurn_ThrowsIllegalActionException()
    {
        // Arrange - Black decides
        GameState state = this.Begun();
        GameAction throne = this._rules.ChancesOf(this.BegunAllAtOnce(), Yellow)[0];

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, throne)
        );

        // Assert
        Assert.Equal("That throne is not allowed now.", exception.Message);
    }

    [Fact]
    public void LegalActions_OnceTheSeatHasAThroneAllAtOnce_OffersTakingBackEachPieceAndMovingTheThroneToo()
    {
        // Arrange - Black's throne on a1, a Pawn bought there
        GameState state = this.BegunAllAtOnce() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };
        state = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Pawn, A1)).State with
        {
            DecidingSeat = Black,
        };

        // Act
        ImmutableList<GameAction> choices = this._rules.LegalActions(state);

        // Assert
        ImmutableList<GameAction> expected =
        [
            new DraftAction(Black, Chaturaji2027Pieces.Raja, B1),
            new DraftAction(Black, Chaturaji2027Pieces.Boat, B1),
            new DraftAction(Black, Chaturaji2027Pieces.Pawn, B1),
            new UndraftAction(Black, A1),
            new ThroneAction(Black, B1),
            new FillArmyAction(Black),
            new PassAction(Black),
        ];
        Assert.Equal(expected, choices);
    }

    [Fact]
    public void Apply_WhenASeatTakesBackAPiece_RefundsItsPriceAndTellsSo()
    {
        // Arrange - Black's Boat on b1, leaving 4
        GameState state = this.BegunAllAtOnce() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };
        state = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Boat, B1)).State with
        {
            DecidingSeat = Black,
        };

        // Act
        Transition taken = this._rules.Apply(state, new UndraftAction(Black, B1));

        // Assert
        Assert.Equal(
            (6, null, new PieceUndrafted(Black, Chaturaji2027Pieces.Boat, B1, 2)),
            (taken.State.Draft!.Left[Black], taken.State.Position.PieceAt(B1), Assert.Single(taken.Events))
        );
    }

    [Fact]
    public void Apply_WhenASeatSpendsAllItHasAllAtOnce_LeavesItDraftingUntilItSaysItIsDone()
    {
        // Arrange - 5 left: a Raja spends it all
        GameState state = this.BegunAllAtOnce() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };
        state = state with { Draft = state.Draft! with { Left = state.Draft.Left.SetItem(Black, 5) } };

        // Act
        GameState bought = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Raja, A1)).State;

        // Assert
        Assert.Equal((0, false), (bought.Draft!.Left[Black], bought.Draft.Done.Contains(Black)));
    }

    [Fact]
    public void Apply_WhenAPieceIsBoughtOnceAnotherIsTakenBack_GivesItAnIdNoOtherHas()
    {
        // Arrange - Pawns bought on a1 (id 0) and b1 (id 1), the one on a1 taken back
        GameState state = this.BegunAllAtOnce() with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, A1),
        };
        state = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Pawn, A1)).State;
        state = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Pawn, B1)).State;
        state = this._rules.Apply(state, new UndraftAction(Black, A1)).State;

        // Act
        GameState bought = this._rules.Apply(state, new DraftAction(Black, Chaturaji2027Pieces.Pawn, A1)).State;

        // Assert
        Assert.Equal(2, bought.Position.PieceAt(A1)!.Id.Value);
    }

    [Fact]
    public void ChancesOf_WhenASeatDraftingAllAtOnceIsDone_OffersItNothingMore()
    {
        // Arrange - Yellow sets its throne, then is done, out of turn
        GameState state = this.BegunAllAtOnce();
        state = this._rules.Apply(state, this._rules.ChancesOf(state, Yellow)[0]).State;
        state = this._rules.Apply(state, new PassAction(Yellow)).State;

        // Act
        ImmutableList<GameAction> chances = this._rules.ChancesOf(state, Yellow);

        // Assert
        Assert.Equal((true, 0), (state.Draft!.Done.Contains(Yellow), chances.Count));
    }

    private GameState Begun() => this._rules.Drafted(this._rules.CreateInitialState(), 7);

    private GameState BegunAllAtOnce() =>
        this._rules.Drafted(this._rules.CreateInitialState(), 7, new DraftSettings { IsSimultaneous = true });
}
