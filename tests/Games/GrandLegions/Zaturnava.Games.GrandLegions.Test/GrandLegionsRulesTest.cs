using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class GrandLegionsRulesTest
{
    /// <summary>The roll-off: the first seat throws a six, the others ones.</summary>
    private static readonly ImmutableList<int> OpeningSix = [6, 1, 1, 1];

    [Fact]
    public void Quick_WhenRead_IsASmallAMediumALargeAndACustomGame()
    {
        // Arrange - nothing beyond the rules

        // Act
        ImmutableList<(string Id, string Name, string Game, string GameName, string? Size, int Cells)> games =
        [
            .. GrandLegionsRules.Quick.Select(rules =>
                (rules.Id, rules.Name, rules.GameId, rules.GameName, rules.Size, rules.Topology.Cells.Count)
            ),
        ];

        // Assert
        Assert.Equal(
            [
                ("grand-legions-small", "Grand Legions (small)", "grand-legions", "Grand Legions", "small", 36),
                ("grand-legions-medium", "Grand Legions (medium)", "grand-legions", "Grand Legions", "medium", 64),
                ("grand-legions-large", "Grand Legions (large)", "grand-legions", "Grand Legions", "large", 144),
                ("grand-legions-custom", "Grand Legions (custom)", "grand-legions", "Grand Legions", "custom", 100),
            ],
            games
        );
    }

    [Fact]
    public void Sized_WhenACustomGamesCreatorSetsItsBoard_PlaysItThereUnderTheSameIdAndElseNowhere()
    {
        // Arrange
        Chaturaji2027Rules custom = GrandLegionsRules.RulesOf(QuickGame.Custom);
        BoardSize board = new(12, 4, 4);

        // Act
        IRulesApplying sized = custom.Sized(board)!;

        // Assert
        Assert.Equal(
            (
                new BoardSize(10, 4, 3),
                "grand-legions-custom",
                "Grand Legions (12 × 12, kingdoms 4 × 4)",
                144,
                (int?)192,
                (BoardSize?)board,
                true
            ),
            (
                custom.SizableBoard!,
                sized.Id,
                sized.Name,
                sized.Topology.Cells.Count,
                sized.DraftBudget,
                sized.SizableBoard,
                ReferenceEquals(sized, custom.Sized(board))
            )
        );
        Assert.Equal(
            (true, true, true),
            (
                custom.Sized(new BoardSize(5, 3, 3)) is null,
                GrandLegionsRules.RulesOf(QuickGame.Medium).Sized(board) is null,
                GrandLegionsRules.RulesOf(QuickGame.Medium).SizableBoard is null
            )
        );
    }

    [Fact]
    public void CustomOf_WhenTheKingdomsAreSet_AsksForAnArmyAndABudgetToFillThem()
    {
        // Arrange - kingdoms of 12 cells, 4 wide
        BoardSize board = new(10, 4, 3);

        // Act
        QuickGame game = QuickGame.CustomOf(board)!;

        // Assert
        Assert.Equal(
            (144, "Artillery 1, Cavalry 1, Heavy 2, Infantry 4, Leader 1"),
            (
                game.Budget,
                string.Join(
                    ", ",
                    game.Minimums.OrderBy(minimum => minimum.Key.ToString(), StringComparer.Ordinal)
                        .Select(minimum => $"{minimum.Key} {minimum.Value}")
                )
            )
        );
    }

    [Fact]
    public void Scoring_WhenRead_IsChaturaji2027sTenTimesOver()
    {
        // Arrange
        Scoring standard = Scoring.Standard;

        // Act
        Scoring grand = GrandLegionsRules.Scoring;

        // Assert
        Assert.Equal(
            new Scoring(
                standard.ThroneEntryBonus * 10,
                standard.RajaOnThroneBonus * 10,
                standard.EmpireBonus * 10,
                standard.EmpireByCaptureBonus * 10,
                standard.EmpireByThroneCaptureBonus * 10,
                standard.EveryPieceBonus * 10,
                standard.RansomAcceptedBonus * 10,
                standard.RansomRajaBonus * 10,
                standard.ClearLead * 10
            ),
            grand
        );
    }

    [Fact]
    public void Quick_WhenStarted_HasEachLeaderOnItsOwnThroneAtEverySize()
    {
        // Arrange - nothing beyond the rules

        // Act
        ImmutableList<string> thrones =
        [
            .. GrandLegionsRules.Quick.SelectMany(rules =>
                rules.Thrones.Select(throne =>
                    rules.CreateInitialState().Position.PieceAt(throne.Value) is { } piece
                    && piece.Owner == throne.Key
                    && rules.IsLeader(piece.Kind)
                        ? "leader"
                        : $"{rules.Id}: seat {throne.Key.Value}'s throne has no leader of its own"
                )
            ),
        ];

        // Assert
        Assert.Equal(Enumerable.Repeat("leader", 16), thrones);
    }

    [Fact]
    public void Quick_WhenRead_IsAPreviewAtEverySize()
    {
        // Arrange - nothing beyond the rules

        // Act
        ImmutableList<bool> previews = [.. GrandLegionsRules.Quick.Select(rules => rules.IsPreview)];

        // Assert
        Assert.Equal([true, true, true, true], previews);
    }

    [Fact]
    public void QuickWith_WhenTheSafetyNetsAreSetTighter_EndsEachGameAtTheirLimits()
    {
        // Arrange - one idle round: every seat but the last has passed it
        ImmutableList<Chaturaji2027Rules> quick = GrandLegionsRules.QuickWith(new SafetyNets(1, 50, 3));

        // Act
        ImmutableList<string?> endings =
        [
            .. quick.Select(rules =>
            {
                GameState state = rules.CreateInitialState() with
                {
                    RollOff = [],
                    AwaitedDice = null,
                    DecidingSeat = new SeatId(0),
                    TurnNumber = 2,
                    IdlePasses = 3,
                };
                return rules.Apply(state, new PassAction(new SeatId(0))).State.Outcome?.Ending;
            }),
        ];

        // Assert
        Assert.Equal(Enumerable.Repeat<string?>(Chaturaji2027Endings.Idle, 4), endings);
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
        Assert.InRange(state.Position.Occupants.Count, 32, 4 * QuickGame.Medium.Cells);
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
    [InlineData("grand-legions-small")]
    [InlineData("grand-legions-medium")]
    [InlineData("grand-legions-large")]
    public void Deal_ForEachQuickGame_DealsArmiesOfTheGamesOwnEachLeaderOnItsThrone(string id)
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.Quick.Single(quick => quick.Id == id);
        GameState initial = rules.CreateInitialState();

        // Act
        GameState dealt = rules.Deal(initial, 9);

        // Assert
        Assert.True(rules.IsDealt);
        Assert.Equal(dealt.Position, dealt.Start);
        Assert.NotEqual(initial.Position.Occupants, dealt.Position.Occupants);
        Assert.All(
            rules.ThronesOf(dealt),
            throne => Assert.True(
                dealt.Position.PieceAt(throne.Value) is { } leader
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
    public void WorthOf_WhenAsked_IsThePiecesPriceOnTheGamesBoard()
    {
        // Arrange - the Dragon King, 78 on 8 × 8, reaching 8 cells more from the middle of 12 × 12
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Large);

        // Act
        (int Worth, string Name) worth = (
            rules.WorthOf(GrandLegionsPieces.DragonKing),
            rules.NameOf(GrandLegionsPieces.DragonKing)
        );

        // Assert
        Assert.Equal((86, "Dragon King (Shogi)"), worth);
    }

    [Fact]
    public void ChancesOf_WhenAJanggiGeneralFacesAnEnemyLeader_IsAFaceOffWhoeverDecides()
    {
        // Arrange - Black's Janggi General on c1 faces Yellow's Western King on the c-file's far end, while Yellow
        // decides
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        int side = (int)Math.Sqrt(rules.Topology.Cells.Count);
        CellId general = new(2);
        CellId king = new(((side - 1) * side) + 2);
        GameState state = rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = new SeatId(1),
            Position = Position.Empty
                .Place(general, new Piece(new PieceId(1), GrandLegionsPieces.JanggiGeneral, new SeatId(0)))
                .Place(king, new Piece(new PieceId(2), GrandLegionsPieces.WesternKing, new SeatId(1))),
        };

        // Act
        ImmutableList<GameAction> chances = rules.ChancesOf(state, new SeatId(0));

        // Assert
        Assert.Equal([new FaceOffAction(new SeatId(0), general, king)], chances);
    }

    [Fact]
    public void LegalActions_WhenAnInfantryPieceNotAPawnStandsOnTheFarEdge_OffersItsPromotion()
    {
        // Arrange - Black's Minister on the c-file's far end, a Yellow Rook held prisoner, Black to decide
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        int side = (int)Math.Sqrt(rules.Topology.Cells.Count);
        CellId edge = new(((side - 1) * side) + 2);
        Piece rook = new(new PieceId(2), GrandLegionsPieces.Rook, new SeatId(1));
        GameState state = rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = new SeatId(0),
            Position = Position.Empty
                .Place(edge, new Piece(new PieceId(1), GrandLegionsPieces.Minister, new SeatId(0))),
            Prisoners = [new Prisoner(rook, new SeatId(0))],
        };

        // Act
        ImmutableList<GameAction> actions = rules.LegalActions(state);

        // Assert
        Assert.Contains(new PromoteAction(new SeatId(0), edge, rook.Id), actions);
    }

    [Fact]
    public void GuideOf_WhenAnInfantryPieceNotAPawn_TellsItsFarEdgePromotionAsCommon()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(GrandLegionsPieces.Minister);

        // Assert
        Assert.Equal("Promoted, on reaching the far edge, into a captured piece.", guide.Powers.Single());
    }

    [Theory]
    [InlineData("knight", "Other", "Horse", "Western")]
    [InlineData("xiangqi-elephant", "Other", "Elephant", "Xiangqi")]
    [InlineData("rook", "Orthogonal", null, "Western")]
    [InlineData("lance", "Forward", null, "Shogi")]
    [InlineData("dragon-king", "Every way", null, "Shogi")]
    public void GuideOf_WhenAsked_TellsWhichWayItGoesWhatSortOfBeastAndWhichGameItComesFrom(
        string kind,
        string bearing,
        string? kin,
        string origin
    )
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(new PieceKindId(kind));

        // Assert
        Assert.Equal((bearing, kin, origin), (guide.Bearing, guide.Kin, guide.Origin));
    }

    [Fact]
    public void GuideOf_WhenAWesternPawn_TellsItsClassStartRowPowersAndRestriction()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(GrandLegionsPieces.WesternPawn);

        // Assert
        Assert.Equal(
            ("Infantry", 2, 2, "From its start rows, its first move may go two cells straight ahead.",
                "Blocked straight ahead"),
            (guide.Class, guide.Powers.Count, guide.SpecialPowers.Count, guide.SpecialPowers[0],
                guide.Restrictions.Single())
        );
    }

    [Theory]
    [InlineData("western-pawn", "queen rook bishop knight", "on the last row")]
    [InlineData("shogi-pawn", "gold-general", "in the far 3 rows")]
    [InlineData("makruk-pawn", "courtier", "in an enemy kingdom")]
    public void GuideOf_WhenAPieceHasAPromotionOfItsOwn_TellsItAsPlayedAndWhere(
        string kind,
        string into,
        string where
    )
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(new PieceKindId(kind));

        // Assert
        Assert.Equal(
            (into, true, where),
            (string.Join(" ", guide.PromotesTo.Select(promoted => promoted.Value)), guide.IsPromotionPlayed,
                guide.PromotesWhere)
        );
    }

    [Fact]
    public void LegalActions_WhenAWesternPawnStandsOnTheLastRow_OffersItsOwnPromotions()
    {
        // Arrange - Black's Western Pawn on the c-file's far end, no prisoners
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        CellId edge = Ahead(rules, 0);
        GameState state = BlackDeciding(rules, (edge, BlackPiece(GrandLegionsPieces.WesternPawn)));

        // Act
        ImmutableList<GameAction> actions = rules.LegalActions(state);

        // Assert
        Assert.Equal(
            ["queen", "rook", "bishop", "knight"],
            actions.OfType<PromoteOwnAction>().Select(promotion => promotion.Becomes.Value)
        );
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    public void LegalActions_WhenAShogiPawnStandsRowsFromTheFarEdge_OffersAGoldGeneralOnlyInTheFarThree(
        int rowsAhead,
        int expected
    )
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        GameState state = BlackDeciding(rules, (Ahead(rules, rowsAhead), BlackPiece(GrandLegionsPieces.ShogiPawn)));

        // Act
        int offered = rules.LegalActions(state).OfType<PromoteOwnAction>().Count();

        // Assert
        Assert.Equal(expected, offered);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    public void LegalActions_WhenAMakrukPawnStandsInAKingdom_OffersACourtierOnlyInAnEnemys(int seat, int expected)
    {
        // Arrange - Black's Makruk Pawn amid a seat's kingdom: its own, Yellow's, Green's (its ally's) or Red's
        QuickGame game = QuickGame.Small;
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(game);
        Kingdom kingdom = new GrandBoard(game.Side, game.Side, game.KingdomWidth, game.KingdomDepth).Kingdoms[seat];
        CellId amid = new(
            ((kingdom.Row + (kingdom.Height / 2)) * game.Side) + kingdom.Column + (kingdom.Width / 2)
        );
        GameState state = BlackDeciding(rules, (amid, BlackPiece(GrandLegionsPieces.MakrukPawn)));

        // Act
        int offered = rules.LegalActions(state).OfType<PromoteOwnAction>().Count();

        // Assert
        Assert.Equal(expected, offered);
    }

    [Fact]
    public void LegalActions_WhenTheSeatHasRetired_OffersNoOwnPromotionOfItsLeader()
    {
        // Arrange - Black's Silver General in the far three rows, Black retired
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        GameState state = BlackDeciding(rules, (Ahead(rules, 1), BlackPiece(GrandLegionsPieces.SilverGeneral))) with
        {
            Retired = [new SeatId(0)],
        };

        // Act
        ImmutableList<GameAction> actions = rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(actions, action => action is PromoteOwnAction);
    }

    [Fact]
    public void Apply_WhenAPieceIsPromotedByItsOwnPromotion_BecomesThatKindStillItself()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        CellId cell = Ahead(rules, 1);
        Piece pawn = BlackPiece(GrandLegionsPieces.ShogiPawn);
        GameState state = BlackDeciding(rules, (cell, pawn));
        PromoteOwnAction promotion = new(new SeatId(0), cell, GrandLegionsPieces.GoldGeneral);

        // Act
        Transition promoted = rules.Apply(state, promotion);

        // Assert
        Piece general = pawn with { Kind = GrandLegionsPieces.GoldGeneral };
        Assert.Equal(general, promoted.State.Position.PieceAt(cell));
        Assert.Contains(new PiecePromoted(pawn, general, cell, new SeatId(0)), promoted.Events);
    }

    [Fact]
    public void Apply_WhenAPieceMovesWhereItMayBePromoted_WaitsOnThatPromotionOrPromotingLater()
    {
        // Arrange - Black's Shogi Pawn steps into the far three rows
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        CellId from = Ahead(rules, 3);
        CellId to = Ahead(rules, 2);
        GameState state = BlackDeciding(rules, (from, BlackPiece(GrandLegionsPieces.ShogiPawn)));

        // Act
        GameState moved = rules.Apply(state, new MoveAction(new SeatId(0), from, to)).State;

        // Assert
        Assert.Equal(
            [new PassAction(new SeatId(0)), new PromoteOwnAction(new SeatId(0), to, GrandLegionsPieces.GoldGeneral)],
            rules.LegalActions(moved)
        );
    }

    [Fact]
    public void MeasuresOf_WhenAnInfantryPieceNotAPawnIsARowFromTheFarEdge_CountsItNearPromoting()
    {
        // Arrange - Black's Minister, promoted at the far edge as every Infantry piece is
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        GameState state = BlackDeciding(rules, (Ahead(rules, 1), BlackPiece(GrandLegionsPieces.Minister)));

        // Act
        double near = rules.MeasuresOf(state, new SeatId(0))[1];

        // Assert
        Assert.Equal(1d, near, 6);
    }

    /// <summary>The cell of the c-file <paramref name="rows"/> rows short of Black's far edge.</summary>
    private static CellId Ahead(Chaturaji2027Rules rules, int rows)
    {
        int side = (int)Math.Sqrt(rules.Topology.Cells.Count);
        return new CellId(((side - 1 - rows) * side) + 2);
    }

    private static Piece BlackPiece(PieceKindId kind) => new(new PieceId(1), kind, new SeatId(0));

    /// <summary>
    /// Black to decide, at the start of its turn, with only <paramref name="pieces"/> on the board, and Yellow's
    /// King on d2, on no throne and out of the way, so that the game goes on.
    /// </summary>
    private static GameState BlackDeciding(Chaturaji2027Rules rules, params (CellId Cell, Piece Piece)[] pieces)
    {
        Position yellow = Position.Empty.Place(
            new CellId(9),
            new Piece(new PieceId(9), GrandLegionsPieces.WesternKing, new SeatId(1))
        );
        return rules.CreateInitialState() with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = new SeatId(0),
            Position = pieces.Aggregate(yellow, (board, placed) => board.Place(placed.Cell, placed.Piece)),
        };
    }

    [Theory]
    [InlineData("western-king", "Castles with its own Rook (Western): two cells towards it")]
    [InlineData("xiangqi-general", "Flying general: ")]
    [InlineData("janggi-general", "Face-off: ")]
    public void GuideOf_WhenALeaderWithAPowerOfItsOwn_TellsItAsSpecialApartFromLeading(string leader, string power)
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(new PieceKindId(leader));

        // Assert
        Assert.Equal(
            ("Leader", "Leads its army", true),
            (
                guide.Class,
                guide.Powers.Single()[..14],
                guide.SpecialPowers.Single().StartsWith(power, StringComparison.Ordinal)
            )
        );
    }

    [Theory]
    [InlineData("xiangqi-elephant", "20,24", 5)]
    [InlineData("alfil", "20,24,56,60", null)]
    public void GuideOf_WhenARiverMayHoldThePieceBack_DrawsTheRiverJustAheadAndNothingAcrossIt(
        string elephant,
        string cells,
        int? riverRow
    )
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(elephant)).Diagram;

        // Assert
        Assert.Equal(
            (cells, cells, riverRow),
            (string.Join(",", diagram.Moves), string.Join(",", diagram.Captures), diagram.RiverRow)
        );
    }

    [Theory]
    [InlineData("xiangqi-horse", "31,39,41,49")]
    [InlineData("xiangqi-elephant", "30,32")]
    [InlineData("alfil", "")]
    public void GuideOf_WhenAPieceInTheWayBlocksALeap_MarksWhereSuchAPieceWouldStand(string leaper, string blockers)
    {
        // Arrange - the Xiangqi Horse's leg, and the Xiangqi Elephant's eye short of the river; the Alfil leaps over
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(leaper)).Diagram;

        // Assert
        Assert.Equal(blockers, string.Join(",", diagram.Blockers));
    }

    [Theory]
    [InlineData("xiangqi-cannon", "31,39,41,49", "4,13,36,37,43,44,67,76", "22,38,42,58")]
    [InlineData(
        "grasshopper",
        "10,13,16,37,43,64,67,70",
        "10,13,16,37,43,64,67,70",
        "20,22,24,38,42,56,58,60"
    )]
    [InlineData("janggi-cannon", "4,13,36,37,43,44,67,76", "4,13,36,37,43,44,67,76", "22,38,42,58")]
    public void GuideOf_WhenAPieceHops_DrawsItWithAPieceToHopTwoSquaresOutOnEachLine(
        string hopper,
        string moves,
        string captures,
        string screens
    )
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(hopper)).Diagram;

        // Assert
        Assert.Equal(
            (moves, captures, screens),
            (string.Join(",", diagram.Moves), string.Join(",", diagram.Captures), string.Join(",", diagram.Screens))
        );
    }

    [Fact]
    public void ClearLead_WhenAsked_IsTenTimesChaturajis()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Large);

        // Act
        int clear = rules.ClearLead;

        // Assert
        Assert.Equal(100, clear);
    }

    [Fact]
    public void Guides_WhenRead_TellOfEveryPieceOfTheCatalogue()
    {
        // Arrange - nothing beyond the guides

        // Act
        ImmutableList<GrandPiece> untold =
            [.. GrandLegionsPieces.Catalogue.Where(piece => !GrandLegionsRules.Guides.ContainsKey(piece.Id))];

        // Assert
        Assert.Empty(untold);
    }

    [Fact]
    public void MarkOf_WhenAsked_IsThePiecesSymbolAndLetters()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceMark? mark = rules.MarkOf(GrandLegionsPieces.MakrukPawn);

        // Assert
        Assert.Equal(new PieceMark("pawn", "M") { Class = "infantry" }, mark);
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
        // Arrange - dealt, then dice and choices alike at random, the same each run
        Chaturaji2027Rules rules = GrandLegionsRules.Quick.Single(quick => quick.Id == id);
        GameState state = rules.Deal(rules.CreateInitialState(), 2027);
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
