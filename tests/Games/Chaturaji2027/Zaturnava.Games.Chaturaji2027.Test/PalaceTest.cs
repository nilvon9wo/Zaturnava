using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
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
            variant: Chaturaji2027Variant.Standard with { Dealer = _ => Chaturaji2027Setup.Start }
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
    public void Apply_WhenASeatPlacesItsPalace_TellsWhere()
    {
        // Arrange
        GameState drafted = this.Begun();

        // Act
        Transition placed = this._rules.Apply(drafted, new PalaceAction(Black, B2));

        // Assert
        ImmutableList<GameEvent> expected = [new PalacePlaced(Black, B2)];
        Assert.Equal(expected, placed.Events);
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
        // Arrange - Black's palace about b2, its throne a1; Yellow's placed, with no throne yet. Black's walls go row
        // by row, cell by cell: a1's bottom and left, b1's bottom, then c1's bottom and right
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

    [Fact]
    public void LegalActions_WhenAKindIsPalaceBound_OffersItOnlyOnTheFreeSquaresOfTheSeatsPalace()
    {
        // Arrange - a palace holds the Horse; Black's palace about b2, its throne a1, a Pawn bought on b1
        Chaturaji2027Rules rules = RulesOf(3, 4, [Chaturaji2027Pieces.Horse]);
        GameState begun = rules.Drafted(rules.Deal(rules.CreateInitialState(), 4, dealsPalaces: true), 7);
        GameState placed = rules.Apply(begun, new PalaceAction(Black, B2)).State with { DecidingSeat = Black };
        GameState throned = rules.Apply(placed, new ThroneAction(Black, new CellId(0))).State with
        {
            DecidingSeat = Black,
        };
        DraftAction pawn = new(Black, Chaturaji2027Pieces.Pawn, new CellId(1));
        GameState bought = rules.Apply(throned, pawn).State with { DecidingSeat = Black };

        // Act
        ImmutableList<GameAction> choices = rules.LegalActions(bought);

        // Assert - every square of the palace but b1, a Pawn anywhere free in the kingdom
        Assert.Equal(
            ("0,2,8,9,10,16,17,18", 11),
            (
                Text(
                    choices.OfType<DraftAction>()
                        .Where(draft => draft.Piece == Chaturaji2027Pieces.Horse)
                        .Select(draft => draft.Cell)
                ),
                choices.OfType<DraftAction>().Count(draft => draft.Piece == Chaturaji2027Pieces.Pawn)
            )
        );
    }

    [Fact]
    public void LegalActions_WhenTheGameHasNoPalaces_DoesNotOfferAPalaceBoundKind()
    {
        // Arrange
        Chaturaji2027Rules rules = RulesOf(3, 4, [Chaturaji2027Pieces.Horse]);
        GameState drafted = rules.Drafted(rules.Deal(rules.CreateInitialState(), 4), 7);
        GameState throned = rules.Apply(drafted, new ThroneAction(Black, new CellId(0))).State with
        {
            DecidingSeat = Black,
        };

        // Act
        ImmutableList<GameAction> choices = rules.LegalActions(throned);

        // Assert
        Assert.Equal(
            (false, true),
            (
                choices.OfType<DraftAction>().Any(draft => draft.Piece == Chaturaji2027Pieces.Horse),
                choices.OfType<DraftAction>().Any()
            )
        );
    }

    [Fact]
    public void Deal_WhenDealtWithPalacePieces_HasThemOnlyWhereThereArePalaces()
    {
        // Arrange - kingdoms with room for a palace, and too narrow for one
        List<ArmyDealing> dealt = [];
        Chaturaji2027Rules rules = RulesOf(3, 4, dealt: dealt);
        Chaturaji2027Rules narrow = RulesOf(2, 4, dealt: dealt);

        // Act
        (bool Palaced, bool Unpalaced, bool Narrow, bool Unasked) held = (
            rules.Deal(rules.CreateInitialState(), 4, dealsPalaces: true, dealsPalacePieces: true).HasPalacePieces,
            rules.Deal(rules.CreateInitialState(), 4, dealsPalacePieces: true).HasPalacePieces,
            narrow.Deal(narrow.CreateInitialState(), 4, dealsPalaces: true, dealsPalacePieces: true).HasPalacePieces,
            rules.Deal(rules.CreateInitialState(), 4, dealsPalaces: true).HasPalacePieces
        );

        // Assert - each deal asked for the armies as it has them
        Assert.Equal(
            ((true, false, false, false), "True,False,False,False"),
            (held, string.Join(",", dealt.Select(dealing => dealing.HasPalacePieces)))
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_WhenASeatWithAPalaceIsFilled_FillsItKnowingItsPalaceAndWhetherItMayHoldPalacePieces(
        bool dealsPalacePieces
    )
    {
        // Arrange
        List<ArmyFill> filled = [];
        Chaturaji2027Rules rules = RulesOf(3, 4, filled: filled);
        GameState begun = rules.Drafted(
            rules.Deal(rules.CreateInitialState(), 4, dealsPalaces: true, dealsPalacePieces: dealsPalacePieces),
            7
        );
        GameState placed = rules.Apply(begun, new PalaceAction(Black, B2)).State with { DecidingSeat = Black };

        // Act
        _ = rules.Apply(placed, new FillArmyAction(Black)).State;

        // Assert - Black's palace about b2: a1 to c3, as seat 0 sees it
        Assert.Equal(
            (dealsPalacePieces, "0 0,1 0,2 0,0 1,1 1,2 1,0 2,1 2,2 2"),
            (
                filled.Single().HasPalacePieces,
                string.Join(",", filled.Single().Palace!.Select(square => $"{square.Column} {square.Row}"))
            )
        );
    }

    [Fact]
    public void Deal_WhenPalacesMayStandAtTheEdge_SetsOnePartlyOutsideAKingdomTooSmallAndNoneElseDifferently()
    {
        // Arrange - kingdoms 4 × 2, too small for a palace, and 4 × 3
        Chaturaji2027Rules narrow = RulesOf(2, 4);

        // Act
        GameState atEdge = narrow.Deal(narrow.CreateInitialState(), 4, dealsPalaces: true, dealsEdgePalaces: true);
        GameState roomy =
            this._rules.Deal(this._rules.CreateInitialState(), 4, dealsPalaces: true, dealsEdgePalaces: true);

        // Assert - about c2, nearest the throne on d1: b1 to d3, d3's row outside the kingdom
        Assert.Equal(
            (AboutC2, 4, true, AboutC2, true),
            (
                Text(atEdge.Palaces![Black]),
                atEdge.Palaces.Count,
                atEdge.HasEdgePalaces,
                Text(roomy.Palaces![Black]),
                roomy.HasEdgePalaces
            )
        );
    }

    [Fact]
    public void LegalActions_WhenAPalaceStandsPartlyOutsideTheKingdom_OffersItsThroneOnlyInTheKingdom()
    {
        // Arrange
        Chaturaji2027Rules narrow = RulesOf(2, 4);
        GameState begun = narrow.Drafted(
            narrow.Deal(narrow.CreateInitialState(), 4, dealsPalaces: true, dealsEdgePalaces: true),
            7
        );
        ImmutableList<GameAction> palaces = narrow.LegalActions(begun);
        GameState placed = narrow.Apply(begun, new PalaceAction(Black, B2)).State with { DecidingSeat = Black };

        // Act
        ImmutableList<GameAction> thrones = narrow.LegalActions(placed);

        // Assert - about b2 or c2; about b2, a1 to c3, its throne on a1 to c2 alone
        ImmutableList<GameAction> offered =
            [new PalaceAction(Black, B2), new PalaceAction(Black, C2), new FillArmyAction(Black)];
        Assert.Equal(
            (Text(offered), AboutB2, "0,1,2,8,9,10"),
            (
                Text(palaces),
                Text(placed.Palaces![Black]),
                Text(thrones.OfType<ThroneAction>().Select(throne => throne.Cell))
            )
        );
    }

    [Fact]
    public void Apply_WhenASeatWithAPalacePartlyOutsideIsFilled_FillsItKnowingOnlyThePalacesSquaresInTheKingdom()
    {
        // Arrange
        List<ArmyFill> filled = [];
        Chaturaji2027Rules narrow = RulesOf(2, 4, filled: filled);
        GameState begun = narrow.Drafted(
            narrow.Deal(narrow.CreateInitialState(), 4, dealsPalaces: true, dealsEdgePalaces: true),
            7
        );
        GameState placed = narrow.Apply(begun, new PalaceAction(Black, B2)).State with { DecidingSeat = Black };

        // Act
        GameState done = narrow.Apply(placed, new FillArmyAction(Black)).State;

        // Assert - its throne in the palace's middle, b2
        Assert.Equal(
            ("0 0,1 0,2 0,0 1,1 1,2 1", B2),
            (
                string.Join(",", filled.Single().Palace!.Select(square => $"{square.Column} {square.Row}")),
                done.Thrones![Black]
            )
        );
    }

    [Fact]
    public void Deal_WhenAnEdgePalaceWouldStandInAnotherArmysKingdom_SetsItElsewhere()
    {
        // Arrange - kingdoms 6 × 2: a palace about b2 or c2 would stand partly in Yellow's kingdom, up the left edge
        Chaturaji2027Rules wide = RulesOf(2, 6);
        GameState begun = wide.Drafted(
            wide.Deal(wide.CreateInitialState(), 4, dealsPalaces: true, dealsEdgePalaces: true),
            7
        );

        // Act
        ImmutableList<GameAction> palaces = wide.LegalActions(begun);

        // Assert - about d2 or e2 alone
        ImmutableList<GameAction> expected =
        [
            new PalaceAction(Black, new CellId(11)),
            new PalaceAction(Black, new CellId(12)),
            new FillArmyAction(Black),
        ];
        Assert.Equal(Text(expected), Text(palaces));
    }

    private GameState Begun() =>
        this._rules.Drafted(this._rules.Deal(this._rules.CreateInitialState(), 4, dealsPalaces: true), 7);

    private static string Text(IEnumerable<CellId> cells) => string.Join(",", cells.Select(cell => cell.Value));

    private static string Text(IEnumerable<GameAction> actions) => string.Join(";", actions);

    /// <summary>
    /// Rules whose kingdoms are <paramref name="width"/> wide and <paramref name="depth"/> deep; a palace holding
    /// <paramref name="palaceBound"/>, if any; each deal asked kept in <paramref name="dealt"/>, and each army filled
    /// at random in <paramref name="filled"/>, if given.
    /// </summary>
    private static Chaturaji2027Rules RulesOf(
        int depth,
        int width,
        ImmutableHashSet<PieceKindId>? palaceBound = null,
        List<ArmyDealing>? dealt = null,
        List<ArmyFill>? filled = null
    ) =>
        new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Chaturaji2027Variant.Standard with
            {
                Dealer = dealing =>
                {
                    dealt?.Add(dealing);
                    return Chaturaji2027Setup.Start;
                },
                Drafting = new Drafting(
                    6,
                    [
                        .. Enumerable.Range(0, depth).SelectMany(row =>
                            Enumerable.Range(0, width).Select(column => new ArmySquare(column, row))
                        ),
                    ],
                    (_, _) => [Chaturaji2027Pieces.Pawn, Chaturaji2027Pieces.Boat, Chaturaji2027Pieces.Raja],
                    fill =>
                    {
                        filled?.Add(fill);
                        return new ArmyFilled(
                            [new ArmyPiece(Chaturaji2027Pieces.Pawn, 0, 0)],
                            fill.Throne ?? new ArmySquare(width - 1, 0)
                        );
                    }
                )
                {
                    PalaceBound = palaceBound ?? [],
                },
            }
        );
}
