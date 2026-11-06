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

        // Assert
        ImmutableList<int> ordered = [.. cheap.Select(kind => counts.GetValueOrDefault(kind))];
        Assert.True(ordered.Min() > 0);
        Assert.Equal(ordered.Order(), ordered);
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
        QuickGame game = new(
            "roomy",
            "Roomy",
            6,
            3,
            1,
            400,
            ImmutableDictionary<PieceClass, int>.Empty.Add(Leader, 1).Add(Infantry, 1)
        );

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

    private static string Name(PieceKindId kind) => kind.Value;

    private static QuickGame Tight(int budget) =>
        new(
            "tight",
            "Tight",
            6,
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
