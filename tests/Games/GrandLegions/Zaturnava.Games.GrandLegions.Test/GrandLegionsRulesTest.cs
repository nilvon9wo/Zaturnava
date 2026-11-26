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
        BoardSize board = new(12, 10, 4, 4);

        // Act
        IRulesApplying sized = custom.Sized(board)!;

        // Assert
        Assert.Equal(
            (
                new BoardSize(10, 10, 4, 3),
                "grand-legions-custom",
                "Grand Legions (12 × 10, kingdoms 4 × 4)",
                120,
                (int?)226,
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
            (true, true, true, (BoardSize?)new BoardSize(8, 8, 4, 2)),
            (
                custom.Sized(new BoardSize(5, 5, 3, 3)) is null,
                GrandLegionsRules.RulesOf(QuickGame.Medium).Sized(board) is null,
                GrandLegionsRules.RulesOf(QuickGame.Medium).SizableBoard is null,
                GrandLegionsRules.RulesOf(QuickGame.Medium).Board
            )
        );
    }

    [Fact]
    public void CustomOf_WhenTheKingdomsAreSet_AsksForAnArmyAndABudgetToFillThem()
    {
        // Arrange - kingdoms of 12 cells, 4 wide
        BoardSize board = new(10, 10, 4, 3);

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
    public void CustomOf_WhenTheBoardIsNotSquare_DealsEachArmyIntoItsOwnKingdom()
    {
        // Arrange - a board 12 across and 9 up, kingdoms 4 x 3, with neutral ground between them
        QuickGame game = QuickGame.CustomOf(new BoardSize(12, 9, 4, 3))!;

        // Act
        ImmutableList<Placement> dealt = QuickArmy.Dealt(game, 7);

        // Assert
        Assert.Equal(
            (true, true, 4),
            (
                game.HasNeutralZone,
                dealt.All(placed => game.Board.Kingdoms[placed.Owner.Value].Contains(placed.Column, placed.Row)),
                dealt.Select(placed => placed.Owner).Distinct().Count()
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
    public void Quick_WhenRead_IsNoLongerAPreviewAtAnySize()
    {
        // Arrange - out of preview (the user's word of 2026-11-20)

        // Act
        ImmutableList<bool> previews = [.. GrandLegionsRules.Quick.Select(rules => rules.IsPreview)];

        // Assert
        Assert.Equal([false, false, false, false], previews);
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
    [InlineData("janggi-guard")]
    [InlineData("xiangqi-advisor")]
    public void GuideOf_WhenAnInfantryPieceAPalaceHolds_TellsItsFarEdgePromotionOnlyShouldItLeaveThePalace(string kind)
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(new PieceKindId(kind));

        // Assert
        Assert.Equal(
            "Promoted, should it ever leave its palace and reach the far edge, into a captured piece.",
            guide.Powers.Single()
        );
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

    [Theory]
    [InlineData("bishop", "", "One colour of square")]
    [InlineData("lance", "Blocked by pieces in its path", "")]
    [InlineData("rook", "", "")]
    public void GuideOf_WhenAPieceIsHeldBack_TellsTheRulesRestrictionsApartFromItsMovesWeaknesses(
        string kind,
        string restrictions,
        string weaknesses
    )
    {
        // Arrange - a restriction a lift may remove; a weakness, nothing (the user's distinction of 2026-11-19)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(new PieceKindId(kind));

        // Assert
        Assert.Equal(
            (restrictions, weaknesses),
            (string.Join(",", guide.Restrictions), string.Join(",", guide.Weaknesses))
        );
    }

    [Fact]
    public void StartRows_AtEverySize_AreTheKingdomsDepthUnlessTheGamesCreatorSetsOthers()
    {
        // Arrange (the user, 2026-11-26)
        ImmutableList<QuickGame> games = QuickGame.All;

        // Act
        ImmutableList<int?> rows = [.. games.Select(game => GrandLegionsRules.RulesOf(game).StartRows)];

        // Assert
        Assert.Equal([.. games.Select(game => (int?)game.KingdomDepth)], rows);
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
        Kingdom kingdom = game.Board.Kingdoms[seat];
        CellId amid = new(
            ((kingdom.Row + (kingdom.Height / 2)) * game.Width) + kingdom.Column + (kingdom.Width / 2)
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

    [Theory]
    [InlineData(GrandLegionsRules.FieldPromotionTurns - 1, false)]
    [InlineData(GrandLegionsRules.FieldPromotionTurns, true)]
    public void LegalActions_WhenAPieceHasStoodOnAnEnemyThrone_OffersItASilverGeneralOnlyOnceItHasStoodLongEnough(
        int turns,
        bool isOffered
    )
    {
        // Arrange - Black's Rook on Yellow's throne
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        CellId throne = rules.Thrones[new SeatId(1)];
        Piece rook = BlackPiece(GrandLegionsPieces.Rook);
        GameState state = BlackDeciding(rules, (throne, rook)) with { ThroneTenure = [new(rook.Id, turns)] };

        // Act
        ImmutableList<GameAction> actions = rules.LegalActions(state);

        // Assert
        Assert.Equal(
            isOffered,
            actions.Contains(new PromoteOwnAction(new SeatId(0), throne, GrandLegionsPieces.SilverGeneral))
        );
    }

    [Theory]
    [InlineData("enemy's", 1)]
    [InlineData("own", 0)]
    public void Apply_WhenATurnPasses_CountsATurnMoreForEachPieceOnAnEnemysThroneOnly(string whose, int counted)
    {
        // Arrange - Black's Rook on Yellow's throne, an enemy's, or on Black's own, three turns there already
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        CellId throne = rules.Thrones[new SeatId(whose == "own" ? 0 : 1)];
        Piece rook = BlackPiece(GrandLegionsPieces.Rook);
        GameState state = BlackDeciding(rules, (throne, rook)) with { ThroneTenure = [new(rook.Id, 3)] };

        // Act
        GameState passed = rules.Apply(state, new PassAction(new SeatId(0))).State;

        // Assert
        Assert.Equal(counted == 0 ? 0 : 4, passed.ThroneTenure.GetValueOrDefault(rook.Id));
    }

    [Fact]
    public void Apply_WhenAPieceIsPromotedInTheField_BecomesASilverGeneralAndScoresAsALeaderEnteringTheThrone()
    {
        // Arrange - Black's Rook, ten turns on Yellow's throne
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        CellId throne = rules.Thrones[new SeatId(1)];
        Piece rook = BlackPiece(GrandLegionsPieces.Rook);
        GameState state = BlackDeciding(rules, (throne, rook)) with
        {
            ThroneTenure = [new(rook.Id, GrandLegionsRules.FieldPromotionTurns)],
        };

        // Act
        Transition promoted = rules.Apply(
            state,
            new PromoteOwnAction(new SeatId(0), throne, GrandLegionsPieces.SilverGeneral)
        );

        // Assert
        Piece general = rook with { Kind = GrandLegionsPieces.SilverGeneral };
        int bonus = promoted.Events.OfType<ThroneEntered>().Single().Points;
        Assert.Equal(
            (general, state.Scores[new SeatId(0)] + bonus),
            (promoted.State.Position.PieceAt(throne), promoted.State.Scores[new SeatId(0)])
        );
        Assert.True(bonus > 0);
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

    [Fact]
    public void Apply_WhenAThreeleaperTakesAnEnemyJanggiGeneral_ScoresItAndHoldsItPrisoner()
    {
        // Arrange - Black's Threeleaper on c1 leaps three up onto Yellow's Janggi General on c4 (playtest 2026-11-15)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Medium);
        int side = (int)Math.Sqrt(rules.Topology.Cells.Count);
        CellId from = new(2);
        CellId to = new((3 * side) + 2);
        Piece general = new(new PieceId(7), GrandLegionsPieces.JanggiGeneral, new SeatId(1));
        GameState state = BlackDeciding(rules, (from, BlackPiece(GrandLegionsPieces.Threeleaper)), (to, general));

        // Act
        GameState taken = rules.Apply(state, new MoveAction(new SeatId(0), from, to)).State;

        // Assert
        Assert.Equal(
            (68, true),
            (taken.Scores[new SeatId(0)], taken.Prisoners.Contains(new Prisoner(general, new SeatId(0))))
        );
    }

    [Theory]
    [InlineData("western-pawn", "western-pawn")]
    [InlineData("lance", "shogi-pawn")]
    [InlineData("makruk-pawn", "western-pawn")]
    [InlineData("western-pawn", "xiangqi-soldier")]
    public void Apply_WhenBlacksPieceMovesOntoGreensBackToBack_SwapsThemIfBothMoveOnlyStraightForward(
        string black,
        string green
    )
    {
        // Arrange - Black's piece on d2, facing up, and Green's on d3, facing down (playtest 2026-11-15)
        (Chaturaji2027Rules rules, GameState state, CellId d2, CellId d3) = BackToBack(black, green);

        // Act
        GameState moved = rules.Apply(state, new MoveAction(new SeatId(0), d2, d3)).State;

        // Assert
        Assert.Equal(
            (new PieceKindId(green), new PieceKindId(black)),
            (moved.Position.PieceAt(d2)?.Kind, moved.Position.PieceAt(d3)?.Kind)
        );
    }

    [Fact]
    public void Apply_WhenAJanggiElephantLeapsOverARiverToItsRight_LetsItAsNoRiverHoldsItBack()
    {
        // Arrange - on the large board, a river north to south between f and g; Black's Janggi Elephant on f2 leaps
        // one right and two diagonally on, to i4 (playtest 2026-11-15: it "cannot move anywhere to its right")
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Large);
        int side = (int)Math.Sqrt(rules.Topology.Cells.Count);
        CellId f2 = new(side + 5);
        CellId i4 = new((3 * side) + 8);
        GameState state = BlackDeciding(rules, (f2, BlackPiece(GrandLegionsPieces.JanggiElephant))) with
        {
            Terrain = 1,
        };

        // Act
        GameState moved = rules.Apply(state, new MoveAction(new SeatId(0), f2, i4)).State;

        // Assert
        Assert.Equal(GrandLegionsPieces.JanggiElephant, moved.Position.PieceAt(i4)?.Kind);
    }

    [Theory]
    [InlineData(8, 18, true)]
    [InlineData(7, 18, false)]
    public void Apply_WhenAPalaceBoundPieceGoesHomeWithItsSquareTaken_StaysInThePalaceWhileItHasRoom(
        int edition,
        int expectedCell,
        bool isPlaced
    )
    {
        // Arrange - Black's Advisor started on b1, now taken by a Rook; its palace a1-c3 full but for c3, which is
        // as near b1 as d1, d2 and d3 outside it (playtest 2026-11-15: Advisors escaping a palace not full)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Medium);
        Piece advisor = new(new PieceId(5), GrandLegionsPieces.XiangqiAdvisor, new SeatId(0));
        int[] palace = [0, 1, 2, 8, 9, 10, 16, 17, 18];
        (CellId, Piece)[] occupied =
        [
            .. palace
                .Where(cell => cell is not (9 or 18))
                .Select(cell =>
                    (new CellId(cell), new Piece(new PieceId(20 + cell), GrandLegionsPieces.Rook, new SeatId(0)))
                ),
        ];
        GameState state = BlackDeciding(rules, occupied) with
        {
            Edition = edition,
            Start = Position.Empty.Place(new CellId(1), advisor),
            Prisoners = [new Prisoner(advisor, new SeatId(0))],
            Palaces = ImmutableDictionary<SeatId, ImmutableList<CellId>>.Empty.Add(
                new SeatId(0),
                [.. palace.Select(cell => new CellId(cell))]
            ),
        };

        // Act
        GameState given = rules.Apply(state, new OfferAction(new SeatId(0), new SeatId(0), [advisor.Id], [])).State;

        // Assert
        Assert.Equal(isPlaced, given.Position.PieceAt(new CellId(expectedCell)) == advisor);
    }

    [Theory]
    [InlineData("lance", true)]
    [InlineData("man", false)]
    [InlineData("shogi-knight", false)]
    [InlineData("rook", false)]
    public void GuideOf_WhenAPieceMovesOnlyStraightForward_TellsOfTheSwap(string kind, bool isTold)
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Medium);

        // Act
        PieceGuide guide = rules.GuideOf(new PieceKindId(kind));

        // Assert
        Assert.Equal(isTold, guide.Powers.Any(power => power.StartsWith("Swaps places", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(false, "shogi-pawn lance")]
    [InlineData(true, " lance")]
    public void Apply_WhenAnAllyBackToBackMayAlsoBeTaken_OffersBothAndSwapsOrTakesAsChosen(
        bool captures,
        string expected
    )
    {
        // Arrange - Black's Lance on d2 and Green's Shogi Pawn on d3, back to back, with Yellow's King on Green's
        // throne, so that Black may take Green's pieces too (the user's request of 2026-11-19: "Swap or capture?")
        (Chaturaji2027Rules rules, GameState backToBack, CellId d2, CellId d3) = BackToBack("lance", "shogi-pawn");
        CellId yellowsKing = new(9);
        GameState state = backToBack with
        {
            Position = backToBack.Position.Relocate(yellowsKing, rules.Thrones[new SeatId(2)]),
        };
        MoveAction swap = new(new SeatId(0), d2, d3);
        MoveAction capture = swap with { Captures = true };

        // Act
        GameState moved = rules.Apply(state, captures ? capture : swap).State;

        // Assert
        Assert.Equal(
            (true, true, expected),
            (
                rules.LegalActions(state).Contains(swap),
                rules.LegalActions(state).Contains(capture),
                $"{moved.Position.PieceAt(d2)?.Kind.Value} {moved.Position.PieceAt(d3)?.Kind.Value}"
            )
        );
    }

    [Theory]
    [InlineData("rook", "western-pawn")]
    [InlineData("western-pawn", "janggi-soldier")]
    [InlineData("shogi-knight", "western-pawn")]
    [InlineData("berolina-pawn", "man")]
    public void Apply_WhenEitherPieceBackToBackMayMoveOtherThanStraightOn_RefusesTheSwap(string black, string green)
    {
        // Arrange
        (Chaturaji2027Rules rules, GameState state, CellId d2, CellId d3) = BackToBack(black, green);

        // Act
        Exception? refused = Record.Exception(() => rules.Apply(state, new MoveAction(new SeatId(0), d2, d3)));

        // Assert
        _ = Assert.IsType<IllegalActionException>(refused);
    }

    /// <summary>
    /// Black's <paramref name="black"/> on d2 of the medium board, Green's <paramref name="green"/> on d3.
    /// </summary>
    private static (Chaturaji2027Rules Rules, GameState State, CellId D2, CellId D3) BackToBack(
        string black,
        string green
    )
    {
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Medium);
        int side = (int)Math.Sqrt(rules.Topology.Cells.Count);
        CellId d2 = new(side + 3);
        CellId d3 = new((2 * side) + 3);
        GameState state = BlackDeciding(
            rules,
            (d2, new Piece(new PieceId(1), new PieceKindId(black), new SeatId(0))),
            (d3, new Piece(new PieceId(2), new PieceKindId(green), new SeatId(2)))
        );
        return (rules, state, d2, d3);
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
    [InlineData("western-king", "Castles with its own Rook (Western) anywhere in its row or column, nothing between")]
    [InlineData("xiangqi-general", "Flying general: ")]
    [InlineData("janggi-general", "Face-off: ")]
    [InlineData("tafl-king", "Takes by custody, ")]
    [InlineData("evangelist", "Converts, ")]
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
                guide.SpecialPowers[0].StartsWith(power, StringComparison.Ordinal)
            )
        );
    }

    [Fact]
    public void GuideOf_WhenTheTaflKing_TellsItMayEscapeTheBoardFromItsFarCorner()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(new PieceKindId("tafl-king"));

        // Assert
        Assert.Equal(
            [false, true],
            guide.SpecialPowers.Select(power => power.StartsWith("Escape: ", StringComparison.Ordinal))
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
    [InlineData("janggi-guard", "30,31,32,39,41,48,49,50", "30,31,32,39,40,41,48,49,50")]
    [InlineData("janggi-soldier", "39,41,48,49,50", "30,31,32,39,40,41,48,49,50")]
    [InlineData("minister", "31,39,41,49", "")]
    public void GuideOf_WhenThePieceStepsAlongPalaceLines_DrawsItOnTheThroneOfAPalaceSteppingAndTakingAlongThem(
        string kind,
        string reached,
        string palace
    )
    {
        // Arrange - the diagram is 9 cells square, its centre 40; the palace about it, 30 to 50
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(kind)).Diagram;

        // Assert
        Assert.Equal(
            (reached, reached, palace),
            (string.Join(",", diagram.Moves), string.Join(",", diagram.Captures), string.Join(",", diagram.Palace))
        );
    }

    [Fact]
    public void GuideOf_TheShogiPawn_TakesStraightAheadAsItMovesAndIsNotSaidToBeBlockedThere()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(GrandLegionsPieces.ShogiPawn);

        // Assert - one square ahead, 49, to move and to take
        Assert.Equal(
            ("49", "49", 0),
            (string.Join(",", guide.Diagram.Moves), string.Join(",", guide.Diagram.Captures), guide.Restrictions.Count)
        );
    }

    [Theory]
    [InlineData("man", "48,50", "48,50,64,70", "56,60,72,80")]
    [InlineData("checkers-king", "30,32,48,50", "10,16,30,32,48,50,64,70", "0,8,20,24,56,60,72,80")]
    [InlineData("western-king", "30,31,32,39,41,48,49,50", "30,31,32,39,41,48,49,50", "")]
    public void GuideOf_WhenAKindJumps_DrawsItLandingBeyondEachPieceItTakesAndJumpingOnAgain(
        string kind,
        string moves,
        string captures,
        string jumps
    )
    {
        // Arrange - from the centre, 40: the Man steps and jumps diagonally forward only, the Checkers King every way
        // diagonally, an enemy one and three squares out along each line it jumps; the Western King never jumps
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(kind)).Diagram;

        // Assert
        Assert.Equal(
            (moves, captures, jumps),
            (
                string.Join(",", diagram.Moves),
                string.Join(",", diagram.Captures),
                string.Join(",", diagram.Jumps)
            )
        );
    }

    [Theory]
    [InlineData("chancellor", "21,23,29,33,47,51,57,59")]
    [InlineData("dragon-horse", "")]
    [InlineData("xiangqi-horse", "")]
    [InlineData("alfil", "20,24,56,60")]
    public void GuideOf_WhenAKindLeapsFartherThanTheNextSquare_SaysWhichSquaresItLeapsToRatherThanSlides(
        string kind,
        string leaps
    )
    {
        // Arrange - the Chancellor slides as a Rook, and leaps as a Knight; the Dragon Horse slides as a Bishop and
        // steps as a King; the Xiangqi Horse's leaps a piece blocks; the Alfil leaps two squares diagonally
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(kind)).Diagram;

        // Assert
        Assert.Equal(leaps, string.Join(",", diagram.Leaps));
    }

    [Theory]
    [InlineData("xiangqi-horse", "39,49", "29,47,57,59")]
    [InlineData("xiangqi-elephant", "30,32", "20,24")]
    [InlineData("rook", "39,49", "36,37,38,58,67,76")]
    [InlineData("alfil", "", "")]
    [InlineData("western-king", "", "")]
    public void GuideOf_WhenPiecesInItsWayStopAKind_PutsThemWhereTheyStopItMostAndSaysWhatTheyStop(
        string kind,
        string obstacles,
        string obstructed
    )
    {
        // Arrange - from the centre, 40: the Xiangqi Horse's legs, the foremost first, then the leftmost; the Xiangqi
        // Elephant's eyes short of the river; the Rook's slides; the Alfil leaps over anything, the King only steps.
        // Each captures where it moves, and is stopped capturing as moving (the user's report of 2026-11-19)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(kind)).Diagram;

        // Assert
        Assert.Equal(
            (obstacles, obstructed, obstacles, obstructed),
            (
                string.Join(",", diagram.Obstacles),
                string.Join(",", diagram.Obstructed),
                string.Join(",", diagram.CaptureObstacles),
                string.Join(",", diagram.CaptureObstructed)
            )
        );
    }

    [Fact]
    public void GuideOf_WhenAKindStepsSidewaysOnlyAcrossARiver_DrawsItOnItsOwnSideAndAcross()
    {
        // Arrange - the Xiangqi Soldier, from the centre, 40: forward to 49; once across a river, to 39 and 41 too
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(GrandLegionsPieces.XiangqiSoldier).Diagram;

        // Assert
        NamedDiagram across = Assert.Single(diagram.Alternates, alternate => alternate.Name == "Across a river");
        Assert.Equal(
            ("49", "49", "Across a river", "39,41,49", "39,41,49"),
            (
                string.Join(",", diagram.Moves),
                string.Join(",", diagram.Captures),
                across.Name,
                string.Join(",", across.Diagram.Moves),
                string.Join(",", across.Diagram.Captures)
            )
        );
    }

    [Fact]
    public void GuideOf_WhenAKindStepsAlongAPalacesLines_DrawsItOnItsPalacesCornerToo()
    {
        // Arrange - the Janggi Soldier on its throne, 40, in its palace's back left corner (the user's word of
        // 2026-11-20: a throne off the palace's centre, its lines running otherwise)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(GrandLegionsPieces.JanggiSoldier).Diagram;

        // Assert - forward, sideways, and along the lines ahead from the throne
        NamedDiagram corner = Assert.Single(diagram.Alternates);
        Assert.Equal(
            ("Throne in a corner", "39,41,49,50", 40, true),
            (
                corner.Name,
                string.Join(",", corner.Diagram.Moves),
                corner.Diagram.Throne,
                corner.Diagram.IsPalaceLined
            )
        );
    }

    [Fact]
    public void GuideOf_WhenAPalaceHoldsAKind_DrawsItInItsPalaceUnlinedAndItsFlyingGeneralOutOfIt()
    {
        // Arrange - the Xiangqi General steps orthogonally, in its palace; its flying general takes a leader on 76
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(GrandLegionsPieces.XiangqiGeneral).Diagram;

        // Assert - and no open board to switch to: out of its palace only by its power (the user's report of
        // 2026-11-19: outside the palace is the exception, not a toggle)
        NamedDiagram flying = Assert.Single(diagram.Alternates);
        Assert.Equal(
            (true, "31,39,41,49", false, 9, "Flying general", 76, "76", 9),
            (
                diagram.Elsewhere is null,
                string.Join(",", diagram.Moves),
                diagram.IsPalaceLined,
                diagram.Palace.Count,
                flying.Name,
                flying.Diagram.Example?.To,
                string.Join(",", flying.Diagram.Example?.Taken ?? []),
                flying.Diagram.Palace.Count
            )
        );
    }

    [Theory]
    [InlineData("western-pawn", "First move,En passant,Swap")]
    [InlineData("western-king", "Castling")]
    [InlineData("janggi-general", "Throne in a corner,Face-off")]
    [InlineData("evangelist", "Converting")]
    [InlineData("tafl-king", "Escape,Berserk")]
    [InlineData("swede", "Berserk")]
    [InlineData("knight", "")]
    [InlineData("man", "Chain of jumps")]
    [InlineData("checkers-king", "Chain of jumps")]
    public void GuideOf_WhenAKindHasSpecialPowers_DrawsEachAtWork(string kind, string names)
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(kind)).Diagram;

        // Assert - each told, its cells marked
        Assert.Equal(
            (names, true),
            (
                string.Join(",", diagram.Alternates.Select(other => other.Name)),
                diagram.Alternates
                    .Where(other => other.Diagram.Told is not null)
                    .All(other => !other.Diagram.Marks.IsEmpty)
            )
        );
    }

    [Fact]
    public void GuideOf_WhenAKindIsAMercenary_SaysSoApartFromItsPowers()
    {
        // Arrange - Shogi's pieces are mercenaries
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(GrandLegionsPieces.ShogiPawn);

        // Assert
        Assert.Equal(
            (true, false),
            (guide.IsMercenary, guide.SpecialPowers.Any(power => power.Contains("ercenary", StringComparison.Ordinal)))
        );
    }

    [Theory]
    [InlineData("tafl-king")]
    [InlineData("swede")]
    public void GuideOf_WhenAKindMayGoBerserk_SaysSoAmongItsDrawbacksNotItsPowers(string kind)
    {
        // Arrange - going berserk helps no one use it (the user's word of 2026-11-19)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        PieceGuide guide = rules.GuideOf(new PieceKindId(kind));

        // Assert
        Assert.Equal(
            (true, false),
            (
                guide.Drawbacks.Any(drawback => drawback.StartsWith("Berserk", StringComparison.Ordinal)),
                guide.SpecialPowers.Any(power => power.Contains("Berserk", StringComparison.Ordinal))
            )
        );
    }

    [Theory]
    [InlineData("xiangqi-cannon")]
    [InlineData("grasshopper")]
    [InlineData("xiangqi-horse")]
    public void GuideOf_WhenPiecesInItsWayStopAKindsCaptures_PutsThemThereForItsCaptures(string kind)
    {
        // Arrange - a hopper's capture stopped by a piece where it would land, or between it and its screen; a Xiangqi
        // Horse's, by a piece on its leg (the user's reports of 2026-11-19)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(kind)).Diagram;

        // Assert
        Assert.Equal((false, false), (diagram.CaptureObstacles.IsEmpty, diagram.CaptureObstructed.IsEmpty));
    }

    [Theory]
    [InlineData("tafl-king", 58, 67, 76)]
    [InlineData("swede", 58, 67, 76)]
    public void GuideOf_WhenAKindTakesByCustody_DrawsACaptureWithThePieceThatHoldsTheEnemyBeyond(
        string kind,
        int landing,
        int taken,
        int partner
    )
    {
        // Arrange - from the centre, 40, it slides two squares forward, next to the enemy, its partner beyond
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(kind)).Diagram;

        // Assert - and gone berserk, drawn apart: an enemy on its throne, a piece of its own side taken with an enemy
        // (the user's report of 2026-11-19: berserk only seemed to repeat its capture)
        MoveDiagram berserk = diagram.Alternates.Single(other => other.Name == "Berserk").Diagram;
        Assert.Equal(
            (new CustodyExample(landing, taken, partner), "13", "67", "67,59"),
            (
                diagram.Custody,
                string.Join(",", berserk.Marks.Single(mark => mark.Mark == CellMark.OnThrone).Cells),
                string.Join(",", berserk.Marks.Single(mark => mark.Mark == CellMark.TakenFriend).Cells),
                string.Join(",", berserk.Example?.Taken ?? [])
            )
        );
    }

    [Fact]
    public void GuideOf_WhenARiverHoldsAKindBack_DrawsItOnAnOpenBoardToo()
    {
        // Arrange - the Xiangqi Elephant leaps two squares diagonally, never across its river
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(GrandLegionsPieces.XiangqiElephant).Diagram;

        // Assert - by the river, only back; on an open board, every way
        MoveDiagram open = Assert.IsType<MoveDiagram>(diagram.Elsewhere);
        Assert.Equal(
            ("20,24", true, "20,24,56,60", false, "48,50"),
            (
                string.Join(",", diagram.Moves),
                diagram.RiverRow is not null,
                string.Join(",", open.Moves),
                open.RiverRow is not null,
                string.Join(",", open.Obstacles)
            )
        );
    }

    [Theory]
    [InlineData("janggi-soldier", 0)]
    [InlineData("janggi-guard", null)]
    public void GuideOf_WhenAKindStepsAlongAPalacesLines_DrawsItOnAnOpenBoardTooUnlessItsPalaceHoldsIt(
        string kind,
        int? openPalace
    )
    {
        // Arrange - the Janggi Soldier goes anywhere; the Guard, its palace holds (the user's report of 2026-11-19)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(new PieceKindId(kind)).Diagram;

        // Assert - in its palace, and out of any, if it may leave
        Assert.Equal((9, openPalace), (diagram.Palace.Count, diagram.Elsewhere?.Palace.Count));
    }

    [Theory]
    [InlineData(QuickArmy.PalacesAnywhereEdition, true)]
    [InlineData(QuickArmy.PalacesAnywhereEdition - 1, false)]
    public void Deal_WhenAWideKingdomIsDealtPalaces_PlacesThemAnywhereTheThroneAndHeldPiecesIn(int edition, bool isAway)
    {
        // Arrange - kingdoms 18 x 6, Black's along the bottom of a 26 x 26 board (the user's words of 2026-11-19: a
        // palace need not stand against any edge of the board; the palace placed, then the throne in it)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.CustomOf(new BoardSize(26, 26, 18, 6))!);
        SeatId black = new(0);

        // Act
        ImmutableList<GameState> dealt =
        [
            .. Enumerable.Range(0, 30).Select(seed =>
                rules.Deal(
                    rules.CreateInitialState(),
                    seed,
                    dealsPalaces: true,
                    dealsPalacePieces: true,
                    edition: edition
                )
            ),
        ];

        // Assert - some of Black's palaces off the board's edge, then; every throne and piece a palace holds in its
        // palace, always
        Assert.Equal(
            (isAway, true),
            (
                dealt.Any(state => state.Palaces![black].All(cell => cell.Value / 26 > 0 && cell.Value % 26 > 0)),
                dealt.All(state => state.Palaces!.All(palace => IsHeldIn(state, palace.Key, palace.Value)))
            )
        );
    }

    [Fact]
    public void Deal_WhenTheCreatorChoseTheRivers_DealsEveryGameOnThem()
    {
        // Arrange - rivers quartering the board, around the kingdoms (asked 2026-11-19)
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Medium);
        int mask = rules.TerrainAxes.Aggregate(0, (all, axis) => all | axis.Mask);
        int quartered = rules.TerrainAxes[0].Options.Single(option => option.Name.Contains("Quarter")).Value;
        int around = rules.TerrainAxes[1].Options.Single(option => option.Name.Contains("Around")).Value;

        // Act
        ImmutableList<int> terrains =
        [
            .. Enumerable.Range(0, 10).Select(seed =>
                rules.Deal(
                    rules.CreateInitialState(),
                    seed,
                    dealsTerrain: true,
                    terrain: new TerrainChoice(mask, quartered | around)
                ).Terrain
            ),
        ];

        // Assert
        Assert.All(
            terrains,
            terrain => Assert.Equal(
                (Rivers.Both, RiverCourse.AroundKingdoms),
                (QuickTerrain.RiversOf(terrain), QuickTerrain.CourseOf(terrain))
            )
        );
    }

    /// <summary>Whether <paramref name="seat"/>'s throne and every piece of its a palace confines are in it.</summary>
    private static bool IsHeldIn(GameState state, SeatId seat, ImmutableList<CellId> palace) =>
        palace.Contains(state.Thrones![seat])
        && state.Position.Occupants
            .Where(placed => placed.Value.Owner == seat && PalaceConfinement.Confined.Contains(placed.Value.Kind))
            .All(placed => palace.Contains(placed.Key));

    [Fact]
    public void GuideOf_WhenNeitherRiverNorPalaceHoldsAKind_DrawsItOnAnOpenBoardAlone()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);

        // Act
        MoveDiagram diagram = rules.GuideOf(GrandLegionsPieces.Rook).Diagram;

        // Assert
        Assert.Null(diagram.Elsewhere);
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
            (6, 5),
            (dice.Sides, dice.KindsOf(1).Count)
        );
        Assert.Contains(GrandLegionsPieces.Grasshopper, dice.KindsOf(1));
        Assert.Contains(GrandLegionsPieces.WesternKing, dice.KindsOf(6));
    }
}
