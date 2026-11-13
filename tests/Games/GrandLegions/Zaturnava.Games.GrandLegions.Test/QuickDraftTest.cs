using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.GrandLegionsPieces;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class QuickDraftTest
{
    private static readonly ImmutableList<int> Seeds = [.. Enumerable.Range(0, 20)];

    [Fact]
    public void Of_ForTheSmallGame_GivesItsBudgetAndEverySquareOfItsKingdom()
    {
        // Arrange
        QuickGame game = QuickGame.Small;

        // Act
        Drafting drafting = QuickDraft.Of(game);

        // Assert - a palace's pieces offered apart, for the game's palaces
        Assert.Equal(
            (110, 9, true),
            (drafting.Budget, drafting.Kingdom.Count, drafting.PalaceBound.SetEquals(PalaceConfinement.Confined))
        );
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Of_OnEachTerrain_OffersAPieceARiverHoldsOnlyIfThereIsARiver(int terrain, bool isOffered)
    {
        // Arrange
        Drafting drafting = QuickDraft.Of(QuickGame.Medium);

        // Act
        ImmutableHashSet<PieceKindId> offered = drafting.Offered(terrain);

        // Assert
        Assert.Equal((isOffered, true), (offered.Contains(XiangqiElephant), offered.Contains(Rook)));
    }

    [Fact]
    public void Filled_FromNothing_DealsAWholeArmyItsLeadersSquareItsThrone()
    {
        // Arrange
        QuickGame game = QuickGame.Small;

        // Act
        ImmutableList<ArmyFilled> filled =
        [
            .. Seeds.Select(seed => QuickDraft.Filled(game, new ArmyFill(0, game.Budget, [], null, new(seed)))),
        ];

        // Assert
        Assert.All(
            filled,
            army => Assert.Equal(
                Leader,
                ById[
                    army.Added.Single(piece => piece.Column == army.Throne.Column && piece.Row == army.Throne.Row).Kind
                ].Class
            )
        );
    }

    [Fact]
    public void Filled_WhenTheArmyHasAThroneButNoLeader_PutsALeaderOnItAndSpendsNoMoreThanIsLeft()
    {
        // Arrange - a throne on b1, nothing bought yet, the whole budget left
        QuickGame game = QuickGame.Small;
        ArmySquare throne = new(1, 0);
        ImmutableDictionary<PieceKindId, int> prices = BoardPrices.On(QuickTerrain.BoardOf(game, 0));

        // Act
        ImmutableList<ArmyFilled> filled =
        [
            .. Seeds.Select(seed => QuickDraft.Filled(game, new ArmyFill(0, game.Budget, [], throne, new(seed)))),
        ];

        // Assert
        Assert.All(
            filled,
            army =>
            {
                Assert.Equal(throne, army.Throne);
                Assert.Contains(
                    army.Added,
                    piece => ById[piece.Kind].Class == Leader && piece is { Row: 0, Column: 1 }
                );
                Assert.InRange(army.Added.Sum(piece => prices[piece.Kind]), 0, game.Budget);
                Assert.Equal(
                    army.Added.Count,
                    army.Added.Select(piece => (piece.Column, piece.Row)).Distinct().Count()
                );
            }
        );
    }

    [Fact]
    public void Filled_WhenTheArmyHasALeader_AddsNoOtherAndPutsInfantryInFrontOfTheRest()
    {
        // Arrange - a King on its throne, a1
        QuickGame game = QuickGame.Small;
        ArmyFill fill = new(0, 60, [new ArmyPiece(WesternKing, 0, 0)], new ArmySquare(0, 0), new(3));

        // Act
        ArmyFilled filled = QuickDraft.Filled(game, fill);

        // Assert
        Assert.DoesNotContain(filled.Added, piece => ById[piece.Kind].Class == Leader);
        Assert.All(
            filled.Added.Where(piece => ById[piece.Kind].Class == Infantry),
            infantry => Assert.True(
                filled.Added.Where(piece => ById[piece.Kind].Class != Infantry).All(other => other.Row <= infantry.Row)
            )
        );
    }

    [Fact]
    public void Filled_WithPalacePieces_PutsThoseItAddsOnFreeSquaresOfThePalaceButTheThrone()
    {
        // Arrange - the large game; a palace about b2, its throne b1, a Rook bought on a1
        QuickGame game = QuickGame.Large;
        ImmutableList<ArmySquare> palace = PalaceSquares.Window(new ArmySquare(1, 1));
        ArmySquare throne = new(1, 0);

        // Act
        ImmutableList<ArmyFilled> filled =
        [
            .. Seeds.Select(seed => QuickDraft.Filled(
                game,
                new ArmyFill(0, game.Budget, [new ArmyPiece(Rook, 0, 0)], throne, new(seed))
                {
                    Palace = palace,
                    HasPalacePieces = true,
                }
            )),
        ];

        // Assert
        ImmutableList<ArmyPiece> held = [.. filled.SelectMany(army => army.Added).Where(IsHeld)];
        Assert.NotEmpty(held);
        Assert.All(
            held,
            piece => Assert.Contains(
                new ArmySquare(piece.Column, piece.Row),
                palace.Remove(throne).Remove(new ArmySquare(0, 0))
            )
        );
    }

    [Fact]
    public void Filled_WithPalacePiecesWhenThePalaceIsFull_AddsNoneButOthers()
    {
        // Arrange - the large game; a palace about b2, every square of it bought, a King on its throne b1
        QuickGame game = QuickGame.Large;
        ImmutableList<ArmySquare> palace = PalaceSquares.Window(new ArmySquare(1, 1));
        ImmutableList<ArmyPiece> army =
        [
            .. palace.Select(square => new ArmyPiece(
                square == new ArmySquare(1, 0) ? WesternKing : Courtier,
                square.Column,
                square.Row
            )),
        ];

        // Act
        ImmutableList<ArmyFilled> filled =
        [
            .. Seeds.Select(seed => QuickDraft.Filled(
                game,
                new ArmyFill(0, game.Budget, army, new ArmySquare(1, 0), new(seed))
                {
                    Palace = palace,
                    HasPalacePieces = true,
                }
            )),
        ];

        // Assert
        Assert.All(filled, each => Assert.NotEmpty(each.Added));
        Assert.DoesNotContain(filled.SelectMany(each => each.Added), IsHeld);
    }

    [Fact]
    public void Filled_WhenNothingIsLeftToSpend_AddsNothing()
    {
        // Arrange
        ArmyFill fill = new(0, 0, [new ArmyPiece(WesternKing, 0, 0)], new ArmySquare(0, 0), new(3));

        // Act
        ArmyFilled filled = QuickDraft.Filled(QuickGame.Small, fill);

        // Assert
        Assert.Empty(filled.Added);
    }

    [Fact]
    public void Drafted_ForAQuickGame_LetsItsPlayersDraftAndStartsOnceEachArmyIsFilled()
    {
        // Arrange
        Chaturaji2027Rules rules = GrandLegionsRules.RulesOf(QuickGame.Small);
        GameState drafted = rules.Drafted(rules.Deal(rules.CreateInitialState(), 5), 5);

        // Act
        GameState started = Enumerable.Range(0, 4).Aggregate(
            drafted,
            (state, _) => rules.Apply(state, rules.LegalActions(state).OfType<Engine.Actions.FillArmyAction>().Single())
                .State
        );

        // Assert
        Assert.Equal(
            (true, null as ArmyDraft, 4, 4),
            (
                rules.IsDraftable,
                started.Draft,
                started.Thrones!.Count,
                started.Position.Occupants.Values.Count(piece => rules.IsLeader(piece.Kind))
            )
        );
    }

    private static bool IsHeld(ArmyPiece piece) =>
        PalaceConfinement.Confined.Contains(piece.Kind) && ById[piece.Kind].Class != Leader;
}
