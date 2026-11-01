using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.GrandLegionsPieces;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class QuickArmyTest
{
    public static TheoryData<string> Games => [.. QuickGame.All.Select(game => game.Id)];

    [Theory]
    [MemberData(nameof(Games))]
    public void Pick_ForEachQuickGame_MeetsTheMinimumsWithinTheBudgetAndTheKingdom(string id)
    {
        // Arrange
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);

        // Act
        ImmutableList<GrandPiece> army = QuickArmy.Pick(game);

        // Assert
        Assert.All(
            game.Minimums,
            minimum => Assert.True(army.Count(piece => piece.Class == minimum.Key) >= minimum.Value)
        );
        Assert.True(army.Sum(piece => piece.Value) <= game.Budget);
        Assert.True(army.Count <= game.Cells);
        _ = Assert.Single(army, piece => piece.Class == Leader);
        Assert.All(army, piece => Assert.Contains(piece, QuickArmy.Eligible));
    }

    [Fact]
    public void Pick_ForTheMediumGame_SpendsTheBudgetClassByClass()
    {
        // Arrange - the cheapest army costs 102 of 125

        // Act
        ImmutableList<GrandPiece> army = QuickArmy.Pick(QuickGame.Medium);

        // Assert
        Assert.Equal(
            [Queen, Zebra, JanggiCannon, Courtier, MakrukPawn, MakrukPawn, MakrukPawn, SilverGeneral],
            army.Select(piece => piece.Id)
        );
    }

    [Fact]
    public void Pick_WhenTheKingdomHasRoomAndBudgetIsLeft_AddsTheBestAffordablePiecesButNoLeader()
    {
        // Arrange - a leader and one infantry required, 200 to spend, room for three
        QuickGame game = new(
            "roomy",
            "Roomy",
            6,
            3,
            1,
            200,
            ImmutableDictionary<PieceClass, int>.Empty.Add(Leader, 1).Add(Infantry, 1)
        );

        // Act
        ImmutableList<GrandPiece> army = QuickArmy.Pick(game);

        // Assert
        Assert.Equal([ShogiPawn, DragonKing, Lance], army.Select(piece => piece.Id));
    }

    [Fact]
    public void Layout_ForTheMediumGame_PutsTheLeaderInTheInnerCornerAndInfantryInFront()
    {
        // Arrange
        ImmutableList<GrandPiece> army = QuickArmy.Pick(QuickGame.Medium);

        // Act
        ImmutableList<ArmyPiece> laid = QuickArmy.Layout(QuickGame.Medium, army);

        // Assert
        Assert.Equal(
            [
                new ArmyPiece(SilverGeneral, 3, 0),
                new ArmyPiece(Queen, 2, 0),
                new ArmyPiece(JanggiCannon, 1, 0),
                new ArmyPiece(Zebra, 0, 0),
                new ArmyPiece(Courtier, 0, 1),
                new ArmyPiece(MakrukPawn, 1, 1),
                new ArmyPiece(MakrukPawn, 2, 1),
                new ArmyPiece(MakrukPawn, 3, 1),
            ],
            laid
        );
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void Layout_ForEachQuickGame_PutsTheLeaderInTheFarCornerOnlyWhereTheKingdomsTouch(string id)
    {
        // Arrange - only the small game's kingdoms touch: 3 + 3 cells fill its side of 6
        QuickGame game = QuickGame.All.Single(quick => quick.Id == id);

        // Act
        ArmyPiece leader = QuickArmy.Layout(game, QuickArmy.Pick(game))
            .Single(piece => GrandLegionsRules.Leaders.Contains(piece.Kind));

        // Assert
        Assert.Equal(
            (game == QuickGame.Small, game.HasNeutralZone ? game.KingdomWidth - 1 : 0, 0),
            (!game.HasNeutralZone, leader.Column, leader.Row)
        );
    }
}
