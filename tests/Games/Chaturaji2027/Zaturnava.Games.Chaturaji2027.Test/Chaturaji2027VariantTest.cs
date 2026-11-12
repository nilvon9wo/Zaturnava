using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class Chaturaji2027VariantTest
{
    private static readonly SeatId Black = new(0);

    /// <summary>A board with no row past the third: nothing may go there.</summary>
    private static readonly Chaturaji2027Variant Fenced = Chaturaji2027Variant.Standard with
    {
        Id = "fenced",
        Name = "Fenced",
        DieNames = ImmutableDictionary<int, string>.Empty.Add(1, "Ships"),
        MayMove = (_, grid, move) => grid.RowOf(move.To) < 3,
    };

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Fenced
    );

    [Fact]
    public void Identity_WhenAVariantIsGiven_IsTheVariants()
    {
        // Arrange - nothing beyond the rules

        // Act
        (string Id, string Name) identity = (this._rules.Id, this._rules.Name);

        // Assert
        Assert.Equal(("fenced", "Fenced"), identity);
    }

    [Fact]
    public void Promotables_WhenSetOrNot_AreThoseSetElseThePawns()
    {
        // Arrange
        Chaturaji2027Variant standard = Chaturaji2027Variant.Standard;
        Chaturaji2027Variant set = standard with { Promotables = [Chaturaji2027Pieces.Horse] };

        // Act
        (string Unset, string Set) promotables = (
            string.Join(" ", standard.Promotables.Select(kind => kind.Value)),
            string.Join(" ", set.Promotables.Select(kind => kind.Value))
        );

        // Assert
        Assert.Equal(("pawn", "horse"), promotables);
    }

    [Fact]
    public void Game_WhenTheVariantIsOneSizeOfAGame_IsThatGameAtThatSize()
    {
        // Arrange
        Chaturaji2027Rules rules = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Fenced with { Game = new GameSize("fences", "Fences", "small", IsPreview: true) }
        );

        // Act
        (string Id, string Name, string? Size, bool IsPreview) game =
            (rules.GameId, rules.GameName, rules.Size, rules.IsPreview);

        // Assert
        Assert.Equal(("fences", "Fences", "small", true), game);
    }

    [Fact]
    public void Game_WhenTheVariantIsAGameOfOneSize_IsItsOwnAtNoSizeAndNoPreview()
    {
        // Arrange - nothing beyond the rules

        // Act
        (string Id, string Name, string? Size, bool IsPreview) game =
            (this._rules.GameId, this._rules.GameName, this._rules.Size, this._rules.IsPreview);

        // Assert
        Assert.Equal(("fenced", "Fenced", null, false), game);
    }

    [Fact]
    public void MarkOf_WhenTheVariantMarksNoKind_IsNoneForAny()
    {
        // Arrange - nothing beyond the rules

        // Act
        PieceMark? mark = this._rules.MarkOf(Chaturaji2027Pieces.Raja);

        // Assert
        Assert.Null(mark);
    }

    [Fact]
    public void GuideOf_WhenTheVariantTellsNothingOfALeader_IsItsMovesAndLeadingAlone()
    {
        // Arrange - nothing beyond the rules

        // Act
        PieceGuide guide = this._rules.GuideOf(Chaturaji2027Pieces.Raja);

        // Assert
        Assert.Equal(
            (Chaturaji2027Pieces.Kinds[Chaturaji2027Pieces.Raja], (string?)null, 0,
                "Leads its army; standing on another army's throne, it takes command of that army too."),
            (guide.Kind, guide.Class, guide.Restrictions.Count, guide.Powers.Single())
        );
    }

    [Fact]
    public void GuideOf_WhenAPawn_TellsOfPromotionAndTheSwap()
    {
        // Arrange - nothing beyond the rules

        // Act
        PieceGuide guide = this._rules.GuideOf(Chaturaji2027Pieces.Pawn);

        // Assert
        Assert.Equal<string>(
            [
                "Promoted, on reaching the far edge, into a captured piece.",
                "Swaps places with an allied Pawn straight ahead of it, the two back to back.",
            ],
            guide.Powers
        );
    }

    [Fact]
    public void GuideOf_WhenALeader_DrawsItMovingAndCapturingOneSquareAnyWay()
    {
        // Arrange - the Raja on the centre of a 9 x 9 board, cell 40
        ImmutableList<int> around = [30, 31, 32, 39, 41, 48, 49, 50];

        // Act
        MoveDiagram diagram = this._rules.GuideOf(Chaturaji2027Pieces.Raja).Diagram;

        // Assert
        Assert.Equal(
            (9, string.Join(",", around), string.Join(",", around), 0),
            (diagram.Side, string.Join(",", diagram.Moves), string.Join(",", diagram.Captures), diagram.Screens.Count)
        );
    }

    [Fact]
    public void GuideOf_WhenAPawn_DrawsItMovingUpAndCapturingDiagonallyUp()
    {
        // Arrange - nothing beyond the rules

        // Act
        MoveDiagram diagram = this._rules.GuideOf(Chaturaji2027Pieces.Pawn).Diagram;

        // Assert
        Assert.Equal(("49", "48,50"), (string.Join(",", diagram.Moves), string.Join(",", diagram.Captures)));
    }

    [Fact]
    public void GuideOf_WhenARiverHoldsTheKindBack_DrawsTheRiverJustAheadAndNothingAcrossIt()
    {
        // Arrange - a Raja no river lets across
        Chaturaji2027Rules rules = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Fenced with { RiverBound = [Chaturaji2027Pieces.Raja] }
        );

        // Act
        MoveDiagram diagram = rules.GuideOf(Chaturaji2027Pieces.Raja).Diagram;

        // Assert
        Assert.Equal(
            ("30,31,32,39,41", "30,31,32,39,41", 5),
            (string.Join(",", diagram.Moves), string.Join(",", diagram.Captures), diagram.RiverRow)
        );
    }

    [Fact]
    public void GuideOf_WhenAPieceInTheWayBlocksALeap_MarksWhereSuchAPieceWouldStand()
    {
        // Arrange - a Horse whose leg a piece blocks, as a Xiangqi Horse's is
        Chaturaji2027Rules rules = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Fenced with
            {
                Kinds = Fenced.Kinds.SetItem(
                    Chaturaji2027Pieces.Horse,
                    new PieceKind(Chaturaji2027Pieces.Horse, [MovementRule.Leap(DirectionSets.Knight, 1).Lame()])
                ),
            }
        );

        // Act
        MoveDiagram diagram = rules.GuideOf(Chaturaji2027Pieces.Horse).Diagram;

        // Assert
        Assert.Equal("31,39,41,49", string.Join(",", diagram.Blockers));
    }

    [Fact]
    public void GuideOf_WhenAKindHops_DrawsAPieceTwoSquaresOutOnEachLineToHopOver()
    {
        // Arrange - a Boat that hops along rows and columns, landing just beyond what it hops
        Chaturaji2027Rules rules = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Fenced with
            {
                Kinds = Fenced.Kinds.SetItem(
                    Chaturaji2027Pieces.Boat,
                    new PieceKind(Chaturaji2027Pieces.Boat, [MovementRule.Hop(DirectionSets.Orthogonal, 1)])
                ),
            }
        );

        // Act
        MoveDiagram diagram = rules.GuideOf(Chaturaji2027Pieces.Boat).Diagram;

        // Assert
        Assert.Equal(
            ("13,37,43,67", "13,37,43,67", "22,38,42,58"),
            (string.Join(",", diagram.Moves), string.Join(",", diagram.Captures), string.Join(",", diagram.Screens))
        );
    }

    [Fact]
    public void ClearLead_WhenAsked_IsTheVariantsScoring()
    {
        // Arrange - nothing beyond the rules

        // Act
        int clear = this._rules.ClearLead;

        // Assert
        Assert.Equal(Scoring.Standard.ClearLead, clear);
    }

    [Fact]
    public void GuideOf_WhenAskedAgain_DrawsTheSameDiagram()
    {
        // Arrange
        MoveDiagram first = this._rules.GuideOf(Chaturaji2027Pieces.Horse).Diagram;

        // Act
        MoveDiagram again = this._rules.GuideOf(Chaturaji2027Pieces.Horse).Diagram;

        // Assert
        Assert.Same(first, again);
    }

    [Fact]
    public void GuideOf_WhenTheVariantTellsOfTheKind_PutsTheRulesPowersBeforeItsOwn()
    {
        // Arrange
        Chaturaji2027Rules rules = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies,
            variant: Fenced with
            {
                Guides = ImmutableDictionary<PieceKindId, PieceGuide>.Empty.Add(
                    Chaturaji2027Pieces.Raja,
                    new PieceGuide(Chaturaji2027Pieces.Kinds[Chaturaji2027Pieces.Raja])
                    {
                        Class = "Leader",
                        Powers = ["Sings."],
                        SpecialPowers = ["Dances."],
                        Restrictions = ["Never on Sundays."],
                    }
                ),
            }
        );

        // Act
        PieceGuide guide = rules.GuideOf(Chaturaji2027Pieces.Raja);

        // Assert
        Assert.Equal(
            ("Leader", 2, "Sings.", "Dances.", "Never on Sundays."),
            (guide.Class, guide.Powers.Count, guide.Powers[^1], guide.SpecialPowers.Single(),
                guide.Restrictions.Single())
        );
    }

    [Theory]
    [InlineData(1, "Ships")]
    [InlineData(2, "Horse")]
    public void NameOfDie_WhenTheVariantNamesSomeValues_NamesThoseItsWay(int value, string expected)
    {
        // Arrange - nothing beyond the rules

        // Act
        string name = this._rules.NameOfDie(value);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void LegalActions_WhenTheBoardForbidsSomeMoves_OffersOnlyTheOthers()
    {
        // Arrange - Black's Pawns may not step to the third row; its Horse may still leap within the first two
        GameState state = this._rules.CreateInitialState() with { TurnNumber = 2, RollOff = [], AwaitedDice = null };

        // Act
        ImmutableList<MoveAction> moves = [.. this._rules.LegalActions(state).OfType<MoveAction>()];

        // Assert
        SquareGridTopology grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);
        Assert.All(moves, move => Assert.True(grid.RowOf(move.To) < 3));
        Assert.DoesNotContain(this._rules.AttacksOf(state, Black), attack => grid.RowOf(attack.Target) >= 3);
    }

    [Fact]
    public void Thrones_WhenRead_AreWhereEachSeatsFirstLeaderStarts()
    {
        // Arrange
        SquareGridTopology grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

        // Act
        ImmutableDictionary<SeatId, CellId> thrones = Fenced.Thrones(grid);

        // Assert
        Assert.Equal(Chaturaji2027Setup.Thrones(grid), thrones);
    }
}
