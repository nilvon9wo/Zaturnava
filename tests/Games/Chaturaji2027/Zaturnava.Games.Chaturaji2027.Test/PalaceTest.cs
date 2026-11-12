using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

/// <summary>
/// Palaces, on Chaturaji 2027's board, dealt as it starts, each kingdom 4 × 3 - seat 0's a1 to d3 - so that a palace
/// may stand about b2 or c2 (cells 9 and 10); Black's Raja, so its throne, on d1 (cell 3). An army filled at random
/// gains a Pawn on its a1, its throne d1 unless it has one.
/// </summary>
public sealed class PalaceTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly CellId B2 = new(9);
    private static readonly CellId C2 = new(10);

    /// <summary>Black's palace about b2: a1 to c3.</summary>
    private const string AboutB2 = "0,1,2,8,9,10,16,17,18";

    /// <summary>Black's palace about c2: b1 to d3.</summary>
    private const string AboutC2 = "1,2,3,9,10,11,17,18,19";

    private readonly Chaturaji2027Rules _rules = RulesOf(3, 4);

    [Fact]
    public void Deal_WhenDealtWithPalaces_SetsEachArmysPalaceAboutTheCentreNearestItsThrone()
    {
        // Arrange
        GameState initial = this._rules.CreateInitialState();

        // Act
        GameState dealt = this._rules.Deal(initial, 4, dealsPalaces: true);

        // Assert
        Assert.Equal(
            (AboutC2, 4, true),
            (
                Text(dealt.Palaces![Black]),
                dealt.Palaces.Count,
                dealt.Palaces.All(palace => palace.Value.Contains(dealt.Thrones![palace.Key]))
            )
        );
    }

    [Fact]
    public void Deal_WhenNotDealtWithPalacesOrTheKingdomHasNoRoomOrNoneDrafts_GivesNoPalaces()
    {
        // Arrange
        Chaturaji2027Rules narrow = RulesOf(2, 4);
        Chaturaji2027Rules undrafted = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Chaturaji2027Variant.Standard with { Dealer = (_, _, _) => Chaturaji2027Setup.Start }
        );

        // Act
        (bool Unasked, bool Narrow, bool Undrafted) none = (
            this._rules.Deal(this._rules.CreateInitialState(), 4).Palaces is null,
            narrow.Deal(narrow.CreateInitialState(), 4, dealsPalaces: true).Palaces is null,
            undrafted.Deal(undrafted.CreateInitialState(), 4, dealsPalaces: true).Palaces is null
        );

        // Assert
        Assert.Equal((true, true, true), none);
    }

    [Fact]
    public void LegalActions_BeforeTheSeatHasAPalace_OffersEachSquareOneMayStandAboutAndFillingAtRandom()
    {
        // Arrange
        GameState drafted = this.Begun();

        // Act
        ImmutableList<GameAction> choices = this._rules.LegalActions(drafted);

        // Assert
        ImmutableList<GameAction> expected =
            [new PalaceAction(Black, B2), new PalaceAction(Black, C2), new FillArmyAction(Black)];
        Assert.Equal((Text(expected), 0), (Text(choices), drafted.Palaces!.Count));
    }

    [Fact]
    public void Apply_WhenASeatPlacesItsPalace_SetsItThereAndOffersItsSquaresAloneForItsThrone()
    {
        // Arrange
        GameState drafted = this.Begun();

        // Act
        GameState placed = this._rules.Apply(drafted, new PalaceAction(Black, B2)).State;
        ImmutableList<GameAction> next = this._rules.LegalActions(placed with { DecidingSeat = Black });

        // Assert
        ImmutableList<GameAction> thrones =
        [
            .. placed.Palaces![Black].Select(cell => (GameAction)new ThroneAction(Black, cell)),
            new FillArmyAction(Black),
        ];
        Assert.Equal(
            (AboutB2, Yellow, Text(thrones)),
            (Text(placed.Palaces[Black]), placed.DecidingSeat!.Value, Text(next))
        );
    }

    [Fact]
    public void Apply_WhenASeatWithAPalaceButNoThroneIsFilled_SetsItsThroneInThePalacesMiddle()
    {
        // Arrange
        GameState placed = this._rules.Apply(this.Begun(), new PalaceAction(Black, B2)).State with
        {
            DecidingSeat = Black,
        };

        // Act
        GameState filled = this._rules.Apply(placed, new FillArmyAction(Black)).State;

        // Assert
        Assert.Equal((B2, AboutB2), (filled.Thrones![Black], Text(filled.Palaces![Black])));
    }

    [Fact]
    public void Apply_WhenASeatWithNoPalaceIsFilled_SetsItsPalaceAboutTheThroneFilled()
    {
        // Arrange
        GameState drafted = this.Begun();

        // Act
        GameState filled = this._rules.Apply(drafted, new FillArmyAction(Black)).State;

        // Assert
        Assert.Equal((new CellId(3), AboutC2), (filled.Thrones![Black], Text(filled.Palaces![Black])));
    }

    [Fact]
    public void Apply_WhenTheOneDrafterIsDone_GivesEverySeatItsPalaceHoldingItsThrone()
    {
        // Arrange
        GameState drafted = this._rules.Drafted(
            this._rules.Deal(this._rules.CreateInitialState(), 4, dealsPalaces: true),
            7,
            new DraftSettings(Drafter: Black)
        );
        GameState placed = this._rules.Apply(drafted, new PalaceAction(Black, B2)).State;

        // Act
        GameState started = this._rules.Apply(placed, new FillArmyAction(Black)).State;

        // Assert
        Assert.Equal(
            (4, true, true, null as ArmyDraft),
            (
                started.Palaces!.Count,
                started.Palaces.All(palace => palace.Value.Count == 9),
                started.Palaces.All(palace => palace.Value.Contains(started.Thrones![palace.Key])),
                started.Draft
            )
        );
    }

    [Fact]
    public void Drafted_WhenTheKingdomHasRoomForOnePalaceAlone_SetsEverySeatsAndAsksOnlyForItsThrone()
    {
        // Arrange
        Chaturaji2027Rules square = RulesOf(3, 3);
        GameState dealt = square.CreateInitialState() with { Palaces = [] };

        // Act
        GameState drafted = square.Drafted(dealt, 7);

        // Assert
        Assert.Equal(
            (4, AboutB2, 10),
            (drafted.Palaces!.Count, Text(drafted.Palaces[Black]), square.LegalActions(drafted).Count)
        );
    }

    [Fact]
    public void Drafted_WhenTheGameHasNoPalaces_GivesNoneAndOffersTheWholeKingdomForTheThrone()
    {
        // Arrange
        GameState dealt = this._rules.Deal(this._rules.CreateInitialState(), 4);

        // Act
        GameState drafted = this._rules.Drafted(dealt, 7);

        // Assert
        Assert.Equal(
            (null, 12),
            (drafted.Palaces, this._rules.LegalActions(drafted).Count(action => action is ThroneAction))
        );
    }

    [Fact]
    public void LinesOf_WithPalaces_DrawsEachWallCounterClockwiseAndEachLineToItsThroneOnceSet()
    {
        // Arrange - Black's palace about b2, its throne a1; Yellow's placed, with no throne yet. Black's walls go row by
        // row, cell by cell: a1's bottom and left, b1's bottom, then c1's bottom and right
        GameState placed = this._rules.Apply(this.Begun(), new PalaceAction(Black, B2)).State;
        placed = this._rules.Apply(placed, this._rules.LegalActions(placed).OfType<PalaceAction>().First()).State with
        {
            Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Black, new CellId(0)),
        };

        // Act
        ImmutableList<BoardLine> lines = this._rules.LinesOf(placed);

        // Assert
        ImmutableList<BoardLine> black = [.. lines.Take(20)];
        Assert.Equal(
            (
                32,
                24,
                new BoardLine("palace", -0.5, -0.5, 0.5, -0.5),
                new BoardLine("palace", 2.5, -0.5, 2.5, 0.5),
                new BoardLine("palace-line", 1, 0, 0, 0)
            ),
            (
                lines.Count,
                lines.Count(line => line.Kind == "palace"),
                black[0],
                black[4],
                black.First(line => line.Kind == "palace-line")
            )
        );
    }

    private GameState Begun() =>
        this._rules.Drafted(this._rules.Deal(this._rules.CreateInitialState(), 4, dealsPalaces: true), 7);

    private static string Text(IEnumerable<CellId> cells) => string.Join(",", cells.Select(cell => cell.Value));

    private static string Text(IEnumerable<GameAction> actions) => string.Join(";", actions);

    /// <summary>Rules whose kingdoms are <paramref name="width"/> wide and <paramref name="depth"/> deep.</summary>
    private static Chaturaji2027Rules RulesOf(int depth, int width) =>
        new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Chaturaji2027Variant.Standard with
            {
                Dealer = (_, _, _) => Chaturaji2027Setup.Start,
                Drafting = new Drafting(
                    6,
                    [
                        .. Enumerable.Range(0, depth).SelectMany(row =>
                            Enumerable.Range(0, width).Select(column => new ArmySquare(column, row))
                        ),
                    ],
                    _ => [Chaturaji2027Pieces.Pawn, Chaturaji2027Pieces.Boat, Chaturaji2027Pieces.Raja],
                    fill => new ArmyFilled(
                        [new ArmyPiece(Chaturaji2027Pieces.Pawn, 0, 0)],
                        fill.Throne ?? new ArmySquare(width - 1, 0)
                    )
                ),
            }
        );
}
