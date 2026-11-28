using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
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
    [InlineData("grand-legions-small", 110, 125)]
    [InlineData("grand-legions-medium", 125, 147)]
    [InlineData("grand-legions-large", 200, 279)]
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
    [InlineData("grand-legions-small", 6)]
    [InlineData("grand-legions-medium", 8)]
    [InlineData("grand-legions-large", 16)]
    [InlineData("grand-legions-custom", 10)]
    public void AimOf_ForEachQuickGame_IsFourFifthsOfItsKingdomRoundedUpOrItsMinimumsIfMore(string id, int expected)
    {
        // Arrange - kingdoms of 6, 8, 18 and 12 squares, with 6, 8, 16 and 9 pieces at least (playtest 2026-11-15, E2;
        // the small game's kingdoms 3 × 2 since 2026-11-26, fewer than the medium's)
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);

        // Act
        int aim = QuickArmy.AimOf(game);

        // Assert
        Assert.Equal(expected, aim);
    }

    [Theory]
    [InlineData(8, "1,1,1,1,4")]
    [InlineData(21, "2,3,3,2,11")]
    [InlineData(30, "2,5,4,4,15")]
    public void TargetOf_ACount_IsALeaderPerTwentyHalfInfantryAndTheRestEvenlyButNeverBelowTheMinimums(
        int count,
        string expected
    )
    {
        // Arrange - the small game, at least a leader, a heavy, a cavalry and three infantry (E1)
        QuickGame game = QuickGame.Small;

        // Act
        ImmutableDictionary<PieceClass, int> target = QuickArmy.TargetOf(game, count);

        // Assert - leaders, heavies, cavalry, artillery, infantry
        Assert.Equal(
            expected,
            string.Join(",", new[] { Leader, Heavy, Cavalry, Artillery, Infantry }.Select(each => target[each]))
        );
    }

    [Fact]
    public void TargetOf_WhenTheMinimumsAskForMore_KeepsTheMinimums()
    {
        // Arrange - the large game, at least 3 heavies, 2 cavalry, 2 artillery and 8 infantry
        QuickGame game = QuickGame.Large;

        // Act
        ImmutableDictionary<PieceClass, int> target = QuickArmy.TargetOf(game, 4);

        // Assert
        Assert.Equal(
            "1,3,2,2,8",
            string.Join(",", new[] { Leader, Heavy, Cavalry, Artillery, Infantry }.Select(each => target[each]))
        );
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void BudgetOf_FromTheCompositionEdition_IsWhatItsCompositionCostsAtAveragePrices(string id)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id) with { Edition = QuickArmy.CompositionEdition };

        // Act
        int budget = QuickArmy.BudgetOf(game);

        // Assert - at least the formula's, as the composition has at least the minimums
        Assert.Equal(QuickArmy.CompositionBudget(game), budget);
        Assert.True(budget >= QuickArmy.FormulaBudget(game));
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void Pick_FromTheCompositionEdition_FillsFourFifthsOfTheKingdomInTheCompositionAimedAtWithinTheBudget(
        string id
    )
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id) with { Edition = QuickArmy.CompositionEdition };
        game = game with { Budget = QuickArmy.BudgetOf(game) };
        ImmutableDictionary<PieceClass, int> target = QuickArmy.TargetOf(game, QuickArmy.AimOf(game));

        // Act
        ImmutableList<ImmutableList<GrandPiece>> armies = [.. Seeds.Select(seed => QuickArmy.Pick(game, new(seed)))];

        // Assert
        Assert.All(armies, army =>
        {
            Assert.InRange(army.Count, QuickArmy.AimOf(game), game.Cells);
            Assert.All(target, aim => Assert.True(army.Count(piece => piece.Class == aim.Key) >= aim.Value));
            Assert.True(army.Sum(piece => piece.Value) <= game.Budget);
            Assert.All(army, piece => Assert.Contains(piece, QuickArmy.Eligible(game)));
        });
    }

    [Theory]
    [InlineData(Infantry)]
    [InlineData(Heavy)]
    [InlineData(Cavalry)]
    public void Pick_FromTheCompositionEdition_DrawsTheDearestAndTheCheapestOfAClassSeldom(PieceClass pieceClass)
    {
        // Arrange - the large game: its class's pieces, by price (E3)
        QuickGame game = QuickGame.Large with { Edition = QuickArmy.CompositionEdition };
        game = game with { Budget = QuickArmy.BudgetOf(game) };
        ImmutableList<int> prices =
        [
            .. QuickArmy.Eligible(game)
                .Where(piece => piece.Class == pieceClass)
                .Select(piece => piece.Value)
                .Order(),
        ];

        // Act
        ImmutableDictionary<int, int> counts = Enumerable.Range(0, 300)
            .SelectMany(seed => QuickArmy.Pick(game, new(seed)))
            .Where(piece => piece.Class == pieceClass)
            .GroupBy(piece => piece.Value)
            .ToImmutableDictionary(price => price.Key, price => price.Count());

        // Assert - the price drawn most often lies strictly between the cheapest and the dearest
        int commonest = counts.MaxBy(price => price.Value).Key;
        Assert.InRange(commonest, prices[0] + 1, prices[^1] - 1);
        Assert.True(counts.GetValueOrDefault(prices[^1]) < counts[commonest] / 4);
    }

    [Fact]
    public void Pick_FromTheCompositionEditionForAKingdomOfMoreThanTwentyPieces_GivesALeaderForEachTwenty()
    {
        // Arrange - a custom game of kingdoms 8 × 4: 26 pieces aimed at, so 2 leaders
        QuickGame game = QuickGame.CustomOf(new BoardSize(20, 20, 8, 4))! with
        {
            Edition = QuickArmy.CompositionEdition,
        };
        game = game with { Budget = QuickArmy.BudgetOf(game) };
        ImmutableList<GrandPiece> army = QuickArmy.Pick(game, new(4));

        // Act
        ImmutableList<ArmyPiece> laid = QuickArmy.Layout(game, army, new(4));

        // Assert - the throne the first leader's square, in the back rank
        Assert.True(army.Count(piece => piece.Class == Leader) >= 2);
        Assert.Equal(Leader, GrandLegionsPieces.ById[laid[0].Kind].Class);
        Assert.Equal(0, laid[0].Row);
    }

    [Fact]
    public void Pick_FromTheCompositionEditionWhenTheBudgetIsTight_KeepsTheMinimumsCheapestAndStops()
    {
        // Arrange - the cheapest leader and heavy cost 65 and 12, and only 10 to spend
        QuickGame game = Tight(10) with { Edition = QuickArmy.CompositionEdition };

        // Act
        ImmutableList<GrandPiece> army = QuickArmy.Pick(game, new(3));

        // Assert
        Assert.Equal([Bishop, SilverGeneral], army.Select(piece => piece.Id).OrderBy(Name));
    }

    [Fact]
    public void Pick_FromTheCompositionEditionWhenTheBudgetIsLarge_FillsTheKingdomPastTheAim()
    {
        // Arrange - the small game, ten times its budget
        QuickGame game = QuickGame.Small with { Edition = QuickArmy.CompositionEdition };
        game = game with { Budget = QuickArmy.BudgetOf(game) * 10 };

        // Act
        ImmutableList<GrandPiece> army = QuickArmy.Pick(game, new(2));

        // Assert
        Assert.Equal(game.Cells, army.Count);
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

    [Fact]
    public void Layout_WhereverItsPalaceStands_NeverPutsTheThroneOnTheKingdomsFrontRankOrInnerSide()
    {
        // Arrange - a palace about every square of the kingdom, its front corner too, and an army dealt no palace
        // (the user's word of 2026-11-20: "Putting the king on the front ranks is ridiculous")
        QuickGame game = QuickGame.Large with { HasPalacePieces = true, HasEdgePalaces = true };

        // Act
        ImmutableList<ArmyPiece> thrones =
        [
            .. game.Kingdom.SelectMany(centre => Seeds.Select(seed => QuickArmy.Layout(
                    game,
                    QuickArmy.Pick(game, new(seed)),
                    new(seed),
                    PalaceSquares.Window(centre)
                )[0]
            )),
            .. Seeds.Select(seed => QuickArmy.Layout(game, QuickArmy.Pick(game, new(seed)), new(seed))[0]),
        ];

        // Assert
        Assert.All(
            thrones,
            throne => Assert.True(throne.Row < game.KingdomDepth - 1 && throne.Column < game.KingdomWidth - 1)
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
    public void Layout_WithPalacePiecesWhenTheLeaderIsKeptToOneColour_LeavesItOnTheThroneTheirPalaceIsAbout()
    {
        // Arrange - the large game, as dealt now (the user's report of 2026-11-19, of a Guard on d2 outside the
        // palace a1:c3: the Checkers King, spread over both colours, left the palace's pieces outside the palace that
        // the game placed about its new square)
        QuickGame game = QuickGame.Large with { HasPalacePieces = true, Edition = QuickArmy.ThroneKeptEdition };

        // Act
        ImmutableList<ImmutableList<ArmyPiece>> armies =
        [
            .. Enumerable.Range(0, 300).Select(seed =>
                QuickArmy.Layout(game, QuickArmy.Pick(game, new(seed)), new(seed))
            ),
        ];

        // Assert - among them, armies led by a leader kept to one colour, with pieces a palace holds; each such
        // piece in the palace about its leader's square
        Assert.Contains(armies, army => army.Any(IsHeld) && army.Any(IsColourBoundLeader));
        Assert.All(armies, army =>
        {
            ArmyPiece leader = army.Single(piece => GrandLegionsRules.Leaders.Contains(piece.Kind));
            ImmutableList<ArmySquare> palace = PalaceSquares.Window(
                PalaceSquares.CentreAbout(
                    PalaceSquares.Centres(game.Kingdom),
                    new ArmySquare(leader.Column, leader.Row)
                )
            );
            Assert.All(army.Where(IsHeld), held => Assert.Contains(new ArmySquare(held.Column, held.Row), palace));
        });
    }

    [Fact]
    public void Layout_WithSeveralLeaders_PutsOnlyThoseAPalaceHoldsInThePalaceAndDispersesTheRest()
    {
        // Arrange - a game of large kingdoms, as dealt now, with several leaders each (the user's report of 2026-11-19,
        // of flying generals starting outside the palace)
        QuickGame game = QuickGame.CustomOf(new BoardSize(20, 20, 8, 4))! with
        {
            HasPalacePieces = true,
            Edition = QuickArmy.HeldLeadersEdition,
        };

        // Act
        ImmutableList<ImmutableList<ArmyPiece>> armies =
        [
            .. Enumerable.Range(0, 300).Select(seed =>
                QuickArmy.Layout(game, QuickArmy.Pick(game, new(seed)), new(seed))
            ),
        ];

        // Assert - among them, armies with a leader a palace holds beside the one on the throne, each such leader in
        // the palace about the throne; and armies with other leaders beside the throne's, some of them outside it
        // (the user's word of 2026-11-19: most leaders start anywhere; only those restricted to the palace may not)
        static bool IsConfined(ArmyPiece piece) => PalaceConfinement.Confined.Contains(piece.Kind);
        ImmutableList<ArmySquare> PalaceOf(ImmutableList<ArmyPiece> army) =>
            PalaceSquares.Window(
                PalaceSquares.CentreAbout(
                    PalaceSquares.Centres(game.Kingdom),
                    new ArmySquare(army[0].Column, army[0].Row)
                )
            );
        bool IsInPalace(ArmyPiece piece, ImmutableList<ArmyPiece> army) =>
            PalaceOf(army).Contains(new ArmySquare(piece.Column, piece.Row));
        Assert.Contains(armies, army => army.Skip(1).Any(piece => IsConfined(piece) && IsLeader(piece)));
        Assert.Contains(
            armies,
            army => army.Skip(1).Any(piece => IsLeader(piece) && !IsConfined(piece) && !IsInPalace(piece, army))
        );
        Assert.All(
            armies,
            army => Assert.All(army.Skip(1).Where(IsConfined), held => Assert.True(IsInPalace(held, army)))
        );
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 4)]
    [InlineData(true, true, 1)]
    public void PalacesDealt_FromItsEdition_PlacesEachSeatsAtRandomOrNoneWithoutPalaces(
        bool hasPalaces,
        bool areAlike,
        int distinct
    )
    {
        // Arrange - kingdoms 18 x 6, with room for a palace about 64 squares
        QuickGame game = QuickGame.CustomOf(new BoardSize(26, 26, 18, 6))! with
        {
            HasPalaces = hasPalaces,
            Edition = QuickArmy.PalacesAnywhereEdition,
        };

        // Act
        ImmutableList<ArmySquare> dealt = QuickArmy.PalacesDealt(game, 5, areAlike);

        // Assert
        Assert.Equal(
            (hasPalaces ? 4 : 0, distinct, true),
            (dealt.Count, dealt.Distinct().Count(), dealt.All(PalaceSquares.Centres(game.Kingdom).Contains))
        );
    }

    private static bool IsLeader(ArmyPiece piece) => GrandLegionsRules.Leaders.Contains(piece.Kind);

    [Fact]
    public void Dealt_WhenItsCreatorSetsABudget_SpendsItNotTheFormulas()
    {
        // Arrange - the small board with kingdoms of 9 squares, more than the least of each class fill, 4 seats
        QuickGame game = QuickGame.Small with { KingdomDepth = 3, Edition = QuickArmy.ThroneKeptEdition };

        // Act - a budget no army can spend before its kingdom is full, and one too small for more than the least
        ImmutableList<Placement> rich = QuickArmy.Dealt(game, 3, budget: 100_000);
        ImmutableList<Placement> poor = QuickArmy.Dealt(game, 3, budget: 1);

        // Assert - every square of every kingdom filled; the least of each class, and no more
        Assert.Equal((36, 24), (rich.Count, poor.Count));
    }

    [Theory]
    [InlineData("Makruk", "King (Western)|Noble (Makruk)|Courtier|Pawn (Makruk)|Rook (Western)|Knight (Western)", 18)]
    // a host of Men, not the two the minimum asks (the user's report of 2026-11-28: Palamedes dealt a King and two Men)
    [InlineData("Checkers", "King (Checkers)|Man", 18)]
    [InlineData("Othello", "Evangelist", 3)]
    public void Dealt_WhenASeatIsNamedAFamily_DealsItOnlyThatGamesPiecesButNotWhereArmiesAreAlike(
        string family,
        string expected,
        int count
    )
    {
        // Arrange - the large game; Black dealt the family's game's pieces only, those it plays under other names
        // too, none of a class it has none of (the user's word of 2026-11-20: "Western Chess doesn't have
        // artilary"); the others any; of the newest edition, as a game created now is
        QuickGame game = QuickGame.Large with { Edition = QuickArmy.PalacesAnywhereEdition };
        ImmutableList<string?> families = [family, null, null, null];

        // Act
        ImmutableList<Placement> dealt = QuickArmy.Dealt(game, 3, families: families);
        ImmutableList<Placement> alike = QuickArmy.Dealt(game, 3, areAlike: true, families: families);

        // Assert - Black's of the family only, of every kind it may have, as many as its budget buys; the others' of
        // other games too; armies alike as if no family were named
        ImmutableList<string> kinds =
            [.. QuickArmy.OfFamily(QuickArmy.Eligible(game), family).Select(piece => piece.Name)];
        ImmutableList<GrandPiece> black =
            [.. dealt.Where(placed => placed.Owner == new SeatId(0)).Select(placed => ById[placed.Kind])];
        Assert.Equal(
            (expected, true, count, true, true),
            (
                string.Join('|', kinds),
                black.All(piece => piece.IsOf(family)),
                black.Count,
                dealt.Where(placed => placed.Owner != new SeatId(0)).Any(placed => !ById[placed.Kind].IsOf(family)),
                alike.SequenceEqual(QuickArmy.Dealt(game, 3, areAlike: true))
            )
        );
    }

    [Theory]
    [InlineData("Leader", "Leader:20")]
    [InlineData("Leader,Artillery", "Leader:1|Artillery:19")]
    [InlineData("Leader,Infantry,Cavalry", "Leader:1|Infantry:13|Cavalry:6")]
    public void TargetOf_WhenAnArmyHasPiecesOfSomeClassesOnly_SharesTheOthersAmongThemButTheLeaders(
        string present,
        string expected
    )
    {
        // Arrange - an army of 20 pieces: a leader, 10 infantry, 3 each of the rest
        ImmutableHashSet<PieceClass> classes = [.. present.Split(',').Select(Enum.Parse<PieceClass>)];

        // Act
        ImmutableDictionary<PieceClass, int> target = QuickArmy.TargetOf(QuickGame.Large, 20, classes);

        // Assert - leaders taking the rest only where there is nothing else
        Assert.Equal(
            expected,
            string.Join(
                '|',
                target.Where(each => each.Value > 0)
                    .OrderBy(each => each.Key)
                    .Select(each => $"{each.Key}:{each.Value}")
            )
        );
    }

    private static bool IsColourBoundLeader(ArmyPiece piece) =>
        GrandLegionsRules.Leaders.Contains(piece.Kind)
        && GrandLegionsPieces.ById[piece.Kind].Restrictions.Any(restriction => restriction.IsColourBinding);

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
        // Arrange - the small board with kingdoms 3 × 3: the leader in its corner, a dark square; a Bishop and a Knight
        // beside it, on a light square and a dark one; the Courtier, alone among the infantry, on a dark square at the
        // front
        QuickGame game = QuickGame.Small with { KingdomDepth = 3 };
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
        // Arrange - kingdoms 3 × 3 on the small board touch: 3 + 3 cells fill its side of 6 (the small game's own,
        // 3 × 2 since 2026-11-26, touch no longer)
        QuickGame game = QuickGame.Small with { KingdomDepth = 3 };

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
