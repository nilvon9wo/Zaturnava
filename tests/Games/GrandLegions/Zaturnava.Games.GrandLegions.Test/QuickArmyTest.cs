using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.GrandLegionsPieces;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class QuickArmyTest
{
    private static readonly ImmutableList<int> Seeds = [.. Enumerable.Range(0, 40)];

    public static TheoryData<string> Games => [.. QuickGame.All.Select(game => game.Id)];

    /// <summary>The quick games whose kingdoms have room for a palace: all but the medium, 4 × 2.</summary>
    public static TheoryData<string> PalacedGames =>
        [.. QuickGame.All.Where(game => !PalaceSquares.Centres(game.Kingdom).IsEmpty).Select(game => game.Id)];

    [Theory]
    [MemberData(nameof(Games))]
    public void Pick_ForEachQuickGame_MeetsTheMinimumsWithinTheBudgetAndTheKingdom(string id)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);

        // Act
        ImmutableList<ImmutableList<GrandPiece>> armies = [.. Seeds.Select(seed => QuickArmy.Pick(game, new(seed)))];

        // Assert
        Assert.All(armies, army =>
        {
            Assert.All(
                game.Minimums,
                minimum => Assert.True(army.Count(piece => piece.Class == minimum.Key) >= minimum.Value)
            );
            Assert.True(army.Sum(piece => piece.Value) <= game.Budget);
            Assert.True(army.Count <= game.Cells);
            _ = Assert.Single(army, piece => piece.Class == Leader);
            Assert.All(army, piece => Assert.Contains(piece, QuickArmy.Eligible(game)));
        });
    }

    [Theory]
    [InlineData(QuickArmy.EvenSpreadEdition - 1, true)]
    [InlineData(QuickArmy.EvenSpreadEdition, false)]
    public void Pick_WhenTheSpareBudgetIsSpreadEvenlyOrNot_GivesTheAmazonOnlyWhenNot(int edition, bool hasAmazon)
    {
        // Arrange - the large game, whose first slots could once spend what the last needed
        QuickGame game = QuickGame.Large with { Edition = edition };

        // Act
        bool dealt = Enumerable.Range(0, 200)
            .Any(seed => QuickArmy.Pick(game, new(seed)).Any(piece => piece.Id == GrandLegionsPieces.Amazon));

        // Assert
        Assert.Equal(hasAmazon, dealt);
    }

    [Theory]
    [InlineData("grand-legions-small", 110, 127)]
    [InlineData("grand-legions-medium", 125, 151)]
    [InlineData("grand-legions-large", 200, 288)]
    public void BudgetOf_BeforeTheFormulaEditionOrFromIt_IsTheGamesOwnOrTheFormulas(string id, int before, int since)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);

        // Act
        (int Before, int Since, int Formula) budgets = (
            QuickArmy.BudgetOf(game with { Edition = QuickArmy.FormulaEdition - 1 }),
            QuickArmy.BudgetOf(game with { Edition = QuickArmy.FormulaEdition }),
            QuickArmy.FormulaBudget(game with { Edition = QuickArmy.FormulaEdition })
        );

        // Assert
        Assert.Equal((before, since, since), budgets);
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void Pick_FromTheFormulaEdition_MeetsTheMinimumsWithinTheFormulasBudget(string id)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id) with { Edition = QuickArmy.FormulaEdition };
        int budget = QuickArmy.BudgetOf(game);

        // Act
        ImmutableList<ImmutableList<GrandPiece>> armies =
            [.. Seeds.Select(seed => QuickArmy.Pick(game with { Budget = budget }, new(seed)))];

        // Assert
        Assert.All(armies, army =>
        {
            Assert.All(
                game.Minimums,
                minimum => Assert.True(army.Count(piece => piece.Class == minimum.Key) >= minimum.Value)
            );
            Assert.True(army.Sum(piece => piece.Value) <= budget);
            _ = Assert.Single(army, piece => piece.Class == Leader);
        });
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void Pick_ForEachQuickGame_PicksArmiesThatDiffer(string id)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);

        // Act
        ImmutableHashSet<string> armies =
        [
            .. Seeds.Select(seed => string.Join(",", QuickArmy.Pick(game, new(seed)).Select(piece => piece.Id.Value))),
        ];

        // Assert
        Assert.True(armies.Count > 1);
    }

    [Fact]
    public void Eligible_WithoutRivers_LeavesOutWhatAPalaceConfinesOrARiverHoldsButNotWhatPalaceLinesEmpower()
    {
        // Arrange - quick games have neither palaces nor rivers
        QuickGame game = QuickGame.Medium;

        // Act
        ImmutableList<PieceKindId> eligible = [.. QuickArmy.Eligible(game).Select(piece => piece.Id)];

        // Assert
        Assert.DoesNotContain(XiangqiGeneral, eligible);
        Assert.DoesNotContain(JanggiGuard, eligible);
        Assert.Contains(JanggiSoldier, eligible);
        Assert.DoesNotContain(XiangqiSoldier, eligible);
        Assert.DoesNotContain(XiangqiElephant, eligible);
        Assert.Contains(Lance, eligible);
    }

    [Fact]
    public void Eligible_WithRivers_LetsInWhatARiverHoldsButNotWhatAPalaceDoes()
    {
        // Arrange
        QuickGame game = QuickGame.Medium with { Rivers = Rivers.NorthSouth };

        // Act
        ImmutableList<PieceKindId> eligible = [.. QuickArmy.Eligible(game).Select(piece => piece.Id)];

        // Assert
        Assert.Contains(XiangqiSoldier, eligible);
        Assert.Contains(XiangqiElephant, eligible);
        Assert.DoesNotContain(XiangqiAdvisor, eligible);
    }

    [Fact]
    public void Eligible_WithPalacePieces_LetsInWhatAPalaceHoldsWhereTheKingdomHasRoomForAPalace()
    {
        // Arrange - the large game's kingdoms have room for a palace; one 3 wide and 1 deep has none
        QuickGame game = QuickGame.Large with { HasPalacePieces = true };
        QuickGame shallow = Roomy with { HasPalacePieces = true };

        // Act
        ImmutableList<PieceKindId> eligible = [.. QuickArmy.Eligible(game).Select(piece => piece.Id)];
        ImmutableList<PieceKindId> cramped = [.. QuickArmy.Eligible(shallow).Select(piece => piece.Id)];

        // Assert
        Assert.Equal(
            (true, true, true, false),
            (
                eligible.Contains(XiangqiGeneral),
                eligible.Contains(JanggiGuard),
                eligible.Contains(XiangqiAdvisor),
                cramped.Contains(JanggiGuard)
            )
        );
    }

    [Theory]
    [MemberData(nameof(PalacedGames))]
    public void Layout_WithPalacePieces_PutsThoseButTheLeaderInThePalaceAboutTheThrone(string id)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id) with { HasPalacePieces = true };

        // Act
        ImmutableList<ImmutableList<ArmyPiece>> armies =
            [.. Seeds.Select(seed => QuickArmy.Layout(game, QuickArmy.Pick(game, new(seed)), new(seed)))];

        // Assert - some army holds such a piece; each stands in its palace, and no two share a square
        Assert.Contains(armies, army => army.Any(IsHeld));
        Assert.All(armies, army =>
        {
            ArmyPiece leader = army.Single(piece => GrandLegionsRules.Leaders.Contains(piece.Kind));
            ImmutableList<ArmySquare> palace = PalaceSquares.Window(
                PalaceSquares.CentreAbout(PalaceSquares.Centres(game.Kingdom), new ArmySquare(leader.Column, 0))
            );
            Assert.Equal(0, leader.Row);
            Assert.All(army.Where(IsHeld), held => Assert.Contains(new ArmySquare(held.Column, held.Row), palace));
            Assert.Equal(army.Count, army.Select(piece => (piece.Column, piece.Row)).Distinct().Count());
        });
    }

    [Fact]
    public void Pick_WithPalacePiecesAndRoomForMany_GivesNoMoreThanThePalaceHasRoomFor()
    {
        // Arrange - a kingdom of a hundred squares, sixty of them infantry, the budget no bound
        QuickGame game = new(
            "crowded",
            "Crowded",
            26,
            10,
            10,
            1_000_000,
            ImmutableDictionary<PieceClass, int>.Empty.Add(Leader, 1).Add(Infantry, 60)
        )
        {
            HasPalacePieces = true,
        };

        // Act
        ImmutableList<int> held =
            [.. Seeds.Select(seed => QuickArmy.Pick(game, new(seed)).Count(piece => IsHeld(piece.Id)))];

        // Assert - a palace's nine squares, but its throne
        Assert.Equal(8, held.Max());
    }

    [Fact]
    public void Layout_WithPalacePiecesAndAPalacePartlyOutside_PutsThemInItsSquaresInTheKingdom()
    {
        // Arrange - the medium game's 4 × 2 kingdom, its palace about b2 or c2, its front row outside
        QuickGame game = QuickGame.Medium with { HasPalacePieces = true, HasEdgePalaces = true };
        QuickGame unpalaced = QuickGame.Medium with { HasPalacePieces = true };

        // Act
        ImmutableList<ImmutableList<ArmyPiece>> armies =
            [.. Seeds.Select(seed => QuickArmy.Layout(game, QuickArmy.Pick(game, new(seed)), new(seed)))];
        bool isEligibleWithout = QuickArmy.Eligible(unpalaced).Any(piece => piece.Id == JanggiGuard);

        // Assert - some army holds such a piece; each stands in its palace, in the kingdom
        Assert.False(isEligibleWithout);
        Assert.Contains(armies, army => army.Any(IsHeld));
        Assert.All(armies, army =>
        {
            ArmyPiece leader = army.Single(piece => GrandLegionsRules.Leaders.Contains(piece.Kind));
            ImmutableList<ArmySquare> centres = PalaceSquares.EdgeCentres(game.Kingdom, game.Width, game.Height);
            ImmutableList<ArmySquare> palace =
                PalaceSquares.Window(PalaceSquares.CentreAbout(centres, new ArmySquare(leader.Column, 0)));
            Assert.All(
                army.Where(IsHeld),
                held => Assert.True(palace.Contains(new ArmySquare(held.Column, held.Row)) && held.Row < 2)
            );
            Assert.Equal(army.Count, army.Select(piece => (piece.Column, piece.Row)).Distinct().Count());
        });
    }

    [Fact]
    public void Pick_WithPalacePiecesAndAPalacePartlyOutside_GivesNoMoreThanItsSquaresInTheKingdomHaveRoomFor()
    {
        // Arrange - a 4 × 2 kingdom, all infantry but the leader, the budget no bound
        QuickGame game = new(
            "edge",
            "Edge",
            8,
            4,
            2,
            1_000_000,
            ImmutableDictionary<PieceClass, int>.Empty.Add(Leader, 1).Add(Infantry, 7)
        )
        {
            HasPalacePieces = true,
            HasEdgePalaces = true,
        };

        // Act
        ImmutableList<int> held =
            [.. Seeds.Select(seed => QuickArmy.Pick(game, new(seed)).Count(piece => IsHeld(piece.Id)))];

        // Assert - six of the palace's squares in the kingdom, but its throne
        Assert.InRange(held.Max(), 1, 5);
    }

    [Fact]
    public void Pick_WhenTheBudgetIsTight_DrawsOnlyWhatLeavesEnoughForTheRestOfTheMinimum()
    {
        // Arrange - the cheapest leader and heavy cost 65 and 12: the budget allows nothing dearer
        QuickGame game = Tight(77);

        // Act
        ImmutableList<ImmutableList<GrandPiece>> armies = [.. Seeds.Select(seed => QuickArmy.Pick(game, new(seed)))];

        // Assert
        Assert.All(armies, army => Assert.Equal([Bishop, SilverGeneral], army.Select(piece => piece.Id).OrderBy(Name)));
    }

    [Fact]
    public void Pick_ForAQuickGame_NeverDrawsAnInfantryPieceMoreOftenThanACheaperOne()
    {
        // Arrange - the infantry every medium army can afford, dearest first: none favoured for being next cheaper
        ImmutableList<PieceKindId> cheap = [WesternPawn, Noble, Minister, Courtier, MakrukPawn];

        // Act
        ImmutableDictionary<PieceKindId, int> counts = Enumerable.Range(0, 400)
            .SelectMany(seed => QuickArmy.Pick(QuickGame.Medium, new(seed)))
            .Select(piece => piece.Id)
            .Where(cheap.Contains)
            .GroupBy(kind => kind)
            .ToImmutableDictionary(kind => kind.Key, kind => kind.Count());

        // Assert - drawn by chance, so a few draws either way are noise: favoured means a twentieth more
        ImmutableList<int> ordered = [.. cheap.Select(kind => counts.GetValueOrDefault(kind))];
        Assert.True(ordered.Min() > 0);
        Assert.All(ordered.Zip(ordered.Skip(1)), pair => Assert.True(pair.First <= pair.Second * 1.05));
    }

    [Fact]
    public void Pick_WhenEvenTheCheapestPiecesCostMoreThanTheBudget_KeepsTheCheapest()
    {
        // Arrange
        QuickGame game = Tight(10);

        // Act
        ImmutableList<GrandPiece> army = QuickArmy.Pick(game, new(3));

        // Assert
        Assert.Equal([Bishop, SilverGeneral], army.Select(piece => piece.Id).OrderBy(Name));
    }

    [Fact]
    public void Pick_WhenTheKingdomHasRoomAndBudgetIsLeft_AddsPiecesUntilItIsFullButNoLeader()
    {
        // Arrange - a leader and one infantry required, 400 to spend, room for three
        QuickGame game = Roomy;

        // Act
        ImmutableList<GrandPiece> army = QuickArmy.Pick(game, new(5));

        // Assert
        Assert.Equal(3, army.Count);
        _ = Assert.Single(army, piece => piece.Class == Leader);
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void Layout_ForEachQuickGame_PutsInfantryInFrontOfTheRestAndTheLeaderInTheBackRank(string id)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);
        ImmutableList<GrandPiece> army = QuickArmy.Pick(game, new(11));

        // Act
        ImmutableList<ArmyPiece> laid = QuickArmy.Layout(game, army, new(11));

        // Assert
        Assert.Equal(army.Count, laid.Select(piece => (piece.Column, piece.Row)).Distinct().Count());
        Assert.All(laid, piece => Assert.InRange(piece.Column, 0, game.KingdomWidth - 1));
        Assert.All(laid, piece => Assert.InRange(piece.Row, 0, game.KingdomDepth - 1));
        Assert.True(
            laid.Where(piece => ById[piece.Kind].Class == Infantry).Min(piece => piece.Row)
                >= laid.Where(piece => ById[piece.Kind].Class != Infantry).Max(piece => piece.Row)
        );
        Assert.Equal(0, laid.Single(piece => GrandLegionsRules.Leaders.Contains(piece.Kind)).Row);
    }

    [Fact]
    public void Layout_WhenManyPiecesAreKeptToOneColour_SpreadsThemOverBothColoursEvenly()
    {
        // Arrange - eight pieces kept to one colour of square, five behind and three among the infantry
        QuickGame game = QuickGame.Large;
        ImmutableList<GrandPiece> army =
        [
            .. new[]
            {
                GrandLegionsPieces.WesternKing, GrandLegionsPieces.Bishop, GrandLegionsPieces.Bishop,
                GrandLegionsPieces.Camel, GrandLegionsPieces.Alfil, GrandLegionsPieces.WarEngine,
                GrandLegionsPieces.Rook, GrandLegionsPieces.Knight, GrandLegionsPieces.Queen,
                GrandLegionsPieces.Zebra, GrandLegionsPieces.Giraffe, GrandLegionsPieces.Chancellor,
                GrandLegionsPieces.Courtier, GrandLegionsPieces.Courtier, GrandLegionsPieces.Courtier,
                GrandLegionsPieces.WesternPawn, GrandLegionsPieces.WesternPawn, GrandLegionsPieces.Minister,
            }.Select(kind => ById[kind]),
        ];

        // Act
        ImmutableList<int> darkLessLight =
        [
            .. Seeds.Select(seed => QuickArmy.Layout(game, army, new(seed)))
                .Select(laid => laid.Where(piece => ById[piece.Kind].Restrictions.Any(rule => rule.IsColourBinding)))
                .Select(bound => bound.Sum(piece => int.IsEvenInteger(piece.Column + piece.Row) ? 1 : -1)),
        ];

        // Assert
        Assert.All(darkLessLight, difference => Assert.InRange(difference, -1, 1));
    }

    [Fact]
    public void Layout_WhenNoPieceOfItsPartCanMakeWay_LeavesAPieceKeptToOneColourWhereItIs()
    {
        // Arrange - the small game's leader in its corner, a dark square; a Bishop and a Knight beside it, on a light
        // square and a dark one; the Courtier, alone among the infantry, on a dark square at the front
        QuickGame game = QuickGame.Small;
        ImmutableList<GrandPiece> army =
        [
            ById[GrandLegionsPieces.WesternKing],
            ById[GrandLegionsPieces.Bishop],
            ById[GrandLegionsPieces.Knight],
            ById[GrandLegionsPieces.Courtier],
        ];

        // Act
        ImmutableList<ArmyPiece> courtiers =
        [
            .. Seeds.Select(seed => QuickArmy.Layout(game, army, new(seed)))
                .Select(laid => laid.Single(piece => piece.Kind == GrandLegionsPieces.Courtier)),
        ];

        // Assert
        Assert.All(courtiers, courtier => Assert.Equal((2, 2), (courtier.Column, courtier.Row)));
    }

    [Fact]
    public void Layout_WhereTheKingdomsTouch_PutsTheLeaderInTheOuterCorner()
    {
        // Arrange - only the small game's kingdoms touch: 3 + 3 cells fill its side of 6
        QuickGame game = QuickGame.Small;

        // Act
        ImmutableList<ArmyPiece> leaders = [.. Seeds.Select(seed => LeaderOf(game, seed))];

        // Assert
        Assert.All(leaders, leader => Assert.Equal((0, 0), (leader.Column, leader.Row)));
    }

    [Fact]
    public void Layout_WhereNeutralGroundLiesBetweenTheKingdoms_PutsTheLeaderAnywhereInTheBackRank()
    {
        // Arrange
        QuickGame game = QuickGame.Large;

        // Act
        ImmutableHashSet<int> columns = [.. Seeds.Select(seed => LeaderOf(game, seed).Column)];

        // Assert
        Assert.True(columns.Count > 1);
    }

    [Fact]
    public void Dealt_BySeed_DealsEachSeatItsOwnArmyTheSameEachTime()
    {
        // Arrange
        QuickGame game = QuickGame.Medium;

        // Act
        ImmutableList<Placement> dealt = QuickArmy.Dealt(game, 21);

        // Assert
        Assert.Equal(dealt, QuickArmy.Dealt(game, 21));
        Assert.NotEqual(dealt, QuickArmy.Dealt(game, 22));
        Assert.Equal(Pinwheel.SeatCount, dealt.Select(placed => placed.Owner).Distinct().Count());
        Assert.All(
            dealt.GroupBy(placed => placed.Owner),
            army => _ = Assert.Single(army, placed => GrandLegionsRules.Leaders.Contains(placed.Kind))
        );
        Assert.Contains(
            Seeds,
            seed => ArmyOf(QuickArmy.Dealt(game, seed), 0) != ArmyOf(QuickArmy.Dealt(game, seed), 1)
        );
    }

    [Fact]
    public void Dealt_WhenTheArmiesAreAlike_DealsEverySeatTheFirstSeatsArmy()
    {
        // Arrange
        QuickGame game = QuickGame.Large;

        // Act
        ImmutableList<Placement> dealt = QuickArmy.Dealt(game, 21, areAlike: true);

        // Assert
        string first = ArmyOf(QuickArmy.Dealt(game, 21), 0);
        Assert.Equal(
            Enumerable.Repeat(first, Pinwheel.SeatCount),
            Enumerable.Range(0, Pinwheel.SeatCount).Select(seat => ArmyOf(dealt, seat))
        );
    }

    /// <summary>A kingdom of 3 × 1: a leader and one infantry required, 400 to spend.</summary>
    private static QuickGame Roomy =>
        new(
            "roomy",
            "Roomy",
            6,
            3,
            1,
            400,
            ImmutableDictionary<PieceClass, int>.Empty.Add(Leader, 1).Add(Infantry, 1)
        );

    private static bool IsHeld(PieceKindId kind) =>
        PalaceConfinement.Confined.Contains(kind) && !GrandLegionsRules.Leaders.Contains(kind);

    private static bool IsHeld(ArmyPiece piece) => IsHeld(piece.Kind);

    private static string Name(PieceKindId kind) => kind.Value;

    /// <summary>A game on 8 × 8, where a leader and a heavy cost what the sheet says, on a tight budget.</summary>
    private static QuickGame Tight(int budget) =>
        new(
            "tight",
            "Tight",
            8,
            3,
            3,
            budget,
            ImmutableDictionary<PieceClass, int>.Empty.Add(Leader, 1).Add(Heavy, 1)
        );

    private static ArmyPiece LeaderOf(QuickGame game, int seed) =>
        QuickArmy.Layout(game, QuickArmy.Pick(game, new(seed)), new(seed))
            .Single(piece => GrandLegionsRules.Leaders.Contains(piece.Kind));

    private static string ArmyOf(ImmutableList<Placement> dealt, int seat) =>
        string.Join(
            ",",
            dealt
                .Where(placed => placed.Owner == new SeatId(seat))
                .Select(placed => placed.Kind.Value)
                .Order(StringComparer.Ordinal)
        );
}
